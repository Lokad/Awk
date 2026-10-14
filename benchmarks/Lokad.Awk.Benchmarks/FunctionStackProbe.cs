using System.Text;
using System.Text.Json;

namespace Lokad.Awk.Benchmarking;

// Run in a child process: a failed stack-safety hypothesis must not kill the test runner.
internal static class FunctionStackProbe
{
    public static async Task RunAsync()
    {
        foreach (var engine in new[] { AwkExecutionEngineKind.Interpreter, AwkExecutionEngineKind.Executable })
        foreach (var shape in new[] { "return", "arguments", "mutual", "async", "limit", "error", "fuel", "cancel", "raised-syntax" })
            await RunCaseAsync(shape, engine).ConfigureAwait(false);
    }

    private static async Task RunCaseAsync(string shape, AwkExecutionEngineKind engine)
    {
        var nestedReturn = "f(n-1)";
        var additions = shape == "raised-syntax" ? 120 : 60;
        for (var index = 0; index < additions; index++)
            nestedReturn = "(1+" + nestedReturn + ")";
        var source = shape switch
        {
            "return" or "raised-syntax" => "function f(n){if(n>1)return " + nestedReturn + ";return 1} BEGIN{print f(1000)}",
            "arguments" => "function id(x){return x} function f(n){if(n>1)return id(f(n-1));return 7} BEGIN{print f(1000)}",
            "mutual" => "function f(n){if(n>1)return g(n-1);return 4} function g(n){if(n>1)return f(n-1);return 4} BEGIN{print f(1000)}",
            "async" or "cancel" => "function f(n,v){getline v;if(n>1)return 1+f(n-1);return 1} BEGIN{print f(1000)}",
            "limit" => "function f(n){if(n>1)return f(n-1);return 1} BEGIN{print f(1001)}",
            "error" => "function f(n){if(n>1)return f(n-1);return 1/0} BEGIN{print f(1000)}",
            "fuel" => "function f(n){if(n>1)return 1+f(n-1);return 1} BEGIN{print f(1000)}",
            _ => throw new ArgumentException("Unknown stack probe.")
        };
        var expectedOutput = shape switch
        {
            "return" => "59941\n", "raised-syntax" => "119881\n",
            "arguments" => "7\n", "mutual" => "4\n", "async" => "1000\n", _ => ""
        };
        var expectedError = shape switch
        {
            "limit" => "awk: function recursion limit exceeded\n",
            "error" => "awk: division by zero attempted\n",
            "fuel" => "awk: execution step limit exceeded\n",
            _ => ""
        };
        var workload = new ReferenceWorkload("stack-" + shape, "control", 0, 0, source, [], [], Encoding.UTF8.GetBytes(expectedOutput));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var reads = 0;
        async Task<AwkReadResult> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            if (++reads == 1000 && shape == "cancel")
                cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            if (reads > 1000)
                return AwkReadResult.EndOfFile;
            return AwkReadResult.FromContent("x\n"u8.ToArray(), [], AwkReadCompletion.MoreData);
        }
        using var host = new ReferenceBenchmarkHost(workload, true,
            shape is "async" or "cancel" ? ReadAsync : null);
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []);
        AwkCompiledProgram compiled;
        if (shape == "raised-syntax")
        {
            var binding = AwkBinder.Bind(AwkParser.Parse(source, 256));
            if (!binding.Success) throw new InvalidOperationException("Raised-syntax probe did not compile.");
            var program = binding.Program.Program;
            compiled = new(AwkCommandParser.Parse(invocation), program,
                AwkExecutableProgram.Compile(program), [], AwkExecutionMemoryOwner.Disabled);
        }
        else
            compiled = await AwkExecutor.CompileAsync(host, invocation, cancellation.Token).ConfigureAwait(false);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(64L << 20);
        if (shape == "fuel") limits = limits.WithMaxExecutionSteps(200);
        if (shape == "raised-syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility, engine);
        var completion = new TaskCompletionSource<AwkExitCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { completion.SetResult(compiled.ExecuteAsync(host, cancellation.Token, options).GetAwaiter().GetResult()); }
            catch (Exception error) { completion.SetException(error); }
        }, 256 << 10);
        thread.Start();
        int exit;
        try { exit = (await completion.Task.ConfigureAwait(false)).Value; }
        catch (OperationCanceledException) when (shape == "cancel" && reads == 1000) { exit = -1; }
        if (!thread.Join(TimeSpan.FromSeconds(5))) throw new InvalidOperationException("Probe thread did not finish.");
        var result = host.Finish(exit);
        var expectedExit = shape == "cancel" ? -1 : expectedError.Length == 0 ? 0 : 2;
        if (exit != expectedExit || result.OutputDigest != workload.ExpectedDigest ||
            Encoding.UTF8.GetString(result.Error) != expectedError)
            throw new InvalidOperationException("Stack probe failed: " + shape + "/" + engine +
                "; exit " + exit + "; output " + Encoding.UTF8.GetString(result.Output) +
                "; error " + Encoding.UTF8.GetString(result.Error) + "; reads " + reads);
        if (counters.MemoryReservedBytes != counters.MemoryReleasedReservedBytes + counters.MemoryCommittedBytes)
            throw new InvalidOperationException("Stack probe left transient reservations: " + shape);
        if (shape == "raised-syntax" && counters.FunctionStackSuspensions != 2000)
            throw new InvalidOperationException("Raised syntax budget did not retain every call boundary.");
        Console.WriteLine(JsonSerializer.Serialize(new { Shape = shape, Engine = engine.ToString(), Exit = exit,
            RequestedStackBytes = 256 << 10, Reads = reads, counters.FunctionStackSuspensions,
            counters.ExecutionSteps, counters.HostCalls, counters.StringDecodes, counters.Utf8Encodes }));
    }
}
