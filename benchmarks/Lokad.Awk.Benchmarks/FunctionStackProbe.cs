using System.Text;
using System.Text.Json;

namespace Lokad.Awk.Benchmarking;

// Run in a child process: a failed stack-safety hypothesis must not kill the test runner.
internal static class FunctionStackProbe
{
    public static async Task RunAsync()
    {
        foreach (var shape in new[] { "return", "arguments", "mutual", "async", "limit", "error", "cancel", "raised-syntax", "prepared-chain", "prepared-async", "prepared-field-chain", "prepared-field-async", "prepared-four-chain", "prepared-four-async", "prepared-control-chain", "prepared-control-async" })
            await RunCaseAsync(shape).ConfigureAwait(false);
    }

    private static async Task RunCaseAsync(string shape)
    {
        var preparedChain = shape.StartsWith("prepared-", StringComparison.Ordinal);
        var nestedReturn = "f(n-1)";
        var additions = shape == "raised-syntax" ? 120 : 60;
        for (var index = 0; index < additions; index++)
            nestedReturn = "(1+" + nestedReturn + ")";
        const string preparedFunctions = "function c1(x){return x+1} function c2(x){return c1(x)+1} function c3(x){return c2(x)+1} function c4(x){return c3(x)+1} ";
        const string preparedFieldFunctions = "function c1(x){return x+length($1)+NF+($2!=\"\")} function c2(x){return c1(x)+1} function c3(x){return c2(x)+1} function c4(x){return c3(x)+1} ";
        const string preparedFourFunctions = "function c1(a,b,c,d){return a+b+c+d} function c2(a,b,c,d){return c1(a,b,c,d)+1} function c3(a,b,c,d){return c2(a,b,c,d)+1} function c4(a,b,c,d){return c3(a,b,c,d)+1} ";
        var preparedControlFunctions = "function c1(x){" +
            string.Concat(Enumerable.Repeat("if(x){", 10)) + "return x+1" + new string('}', 10) +
            ";return 0} function c2(x){return c1(x)+1} function c3(x){return c2(x)+1} function c4(x){return c3(x)+1} ";
        var source = shape switch
        {
            "return" or "raised-syntax" => "function f(n){if(n>1)return " + nestedReturn + ";return 1} BEGIN{print f(1000)}",
            "arguments" => "function id(x){return x} function f(n){if(n>1)return id(f(n-1));return 7} BEGIN{print f(1000)}",
            "mutual" => "function f(n){if(n>1)return g(n-1);return 4} function g(n){if(n>1)return f(n-1);return 4} BEGIN{print f(1000)}",
            "async" or "cancel" => "function f(n,v){getline v;if(n>1)return 1+f(n-1);return 1} BEGIN{print f(1000)}",
            "prepared-chain" => preparedFunctions + "function f(n){if(n>1)return c4(f(n-1));return 1} BEGIN{print f(1000)}",
            "prepared-async" => preparedFunctions + "function f(n,v){getline v;if(n>1)return c4(f(n-1));return 1} BEGIN{print f(1000)}",
            "prepared-field-chain" => preparedFieldFunctions + "function f(n){if(n>1)return c4(f(n-1));return 1} BEGIN{$0=\"a bb\";print f(1000)}",
            "prepared-field-async" => preparedFieldFunctions + "function f(n,v){getline v;if(n>1)return c4(f(n-1));return 1} BEGIN{$0=\"a bb\";print f(1000)}",
            "prepared-control-chain" => preparedControlFunctions + "function f(n){if(n>1)return c4(f(n-1));return 1} BEGIN{print f(1000)}",
            "prepared-control-async" => preparedControlFunctions + "function f(n,v){getline v;if(n>1)return c4(f(n-1));return 1} BEGIN{print f(1000)}",
            "prepared-four-chain" => preparedFourFunctions + "function f(n){if(n>1)return c4(f(n-1),1,1,1);return 1} BEGIN{print f(1000)}",
            "prepared-four-async" => preparedFourFunctions + "function f(n,v){getline v;if(n>1)return c4(f(n-1),1,1,1);return 1} BEGIN{print f(1000)}",
            "limit" => "function f(n){if(n>1)return f(n-1);return 1} BEGIN{print f(1001)}",
            "error" => "function f(n){if(n>1)return f(n-1);return 1/0} BEGIN{print f(1000)}",
            _ => throw new ArgumentException("Unknown stack probe.")
        };
        var expectedOutput = shape switch
        {
            "return" => "59941\n",
            "raised-syntax" => "119881\n",
            "arguments" => "7\n",
            "mutual" => "4\n",
            "async" => "1000\n",
            "prepared-chain" or "prepared-async" or "prepared-control-chain" or "prepared-control-async" => "3997\n",
            "prepared-field-chain" or "prepared-field-async" => "6994\n",
            "prepared-four-chain" or "prepared-four-async" => "5995\n",
            _ => ""
        };
        var expectedError = shape switch
        {
            "limit" => "awk: function recursion limit exceeded\n",
            "error" => "awk: division by zero attempted\n",
            _ => ""
        };
        var workload = new ReferenceWorkload("stack-" + shape, "control", 0, 0, source, [], [], Encoding.UTF8.GetBytes(expectedOutput));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var reads = 0;
        async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            if (++reads == 1000 && shape == "cancel")
                cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            if (reads > 1000)
                return AwkReadBuffer.EndOfFile;
            return AwkReadBuffer.FromContent("x\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
        }
        using var host = new ReferenceBenchmarkHost(workload, true,
            shape is "async" or "cancel" or "prepared-async" or "prepared-field-async" or "prepared-four-async" or "prepared-control-async" ? ReadAsync : null);
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []);
        AwkCompiledProgram compiled;
        if (shape == "raised-syntax")
        {
            var binding = AwkBinder.Bind(AwkParser.Parse(source, 256));
            if (!binding.Success) throw new InvalidOperationException("Raised-syntax probe did not compile.");
            var program = binding.Program.Program;
            compiled = new(AwkCommandParser.Parse(invocation), program,
                [], AwkExecutionMemoryOwner.Disabled);
        }
        else
            compiled = await AwkExecutor.CompileAsync(host, invocation, cancellation.Token, regexEngine: AwkUtf8RegexEngine.Instance).ConfigureAwait(false);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        // The recursive caller can retain 1,000 frames while the prepared chain adds four.
        if (preparedChain) limits = limits.WithMaxFunctionCallDepth(1004);
        if (shape == "raised-syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits) with { MemoryBudget = new Lokad.Awk.Tests.CooperativeTestBudget(64L << 20) };
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
            throw new InvalidOperationException("Stack probe failed: " + shape +
                "; exit " + exit + "; output " + Encoding.UTF8.GetString(result.Output) +
                "; error " + Encoding.UTF8.GetString(result.Error) + "; reads " + reads);
        if (shape == "raised-syntax" && counters.FunctionStackSuspensions == 0)
            throw new InvalidOperationException("Raised syntax budget did not retain every call boundary.");
        if (preparedChain && counters.SynchronousFunctionRuns == 0)
            throw new InvalidOperationException("Prepared function chain did not use synchronous execution.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Shape = shape,
            Exit = exit,
            RequestedStackBytes = 256 << 10,
            FunctionDepthLimit = limits.MaxFunctionCallDepth,
            Reads = reads,
            counters.FunctionStackSuspensions,
            counters.HostCalls,
            counters.StringDecodes,
            counters.Utf8Encodes,
            counters.SynchronousFunctionRuns
        }));
    }
}
