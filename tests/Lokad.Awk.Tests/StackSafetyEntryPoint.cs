using System.Text.Json;

namespace Lokad.Awk.Tests;

internal static class StackSafetyEntryPoint
{
    private static int Main(string[] args)
    {
        if (args.Length != 3 || args[0] != "--stack-safety" || !bool.TryParse(args[2], out var governed))
            return 64;
        try
        {
            var scenario = AwkStackSafetyScenario.Create(args[1]);
            var completion = new TaskCompletionSource<AwkStackSafetyObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try { completion.SetResult(Execute(args[1], governed, scenario)); }
                catch (Exception error) { completion.SetException(error); }
            }, 256 << 10) { IsBackground = true };
            thread.Start();
            var result = completion.Task.GetAwaiter().GetResult();
            thread.Join();
            Console.WriteLine(JsonSerializer.Serialize(result));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static AwkStackSafetyObservation Execute(string shape, bool governed, AwkStackSafetyScenario scenario)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(512L << 20);
        var policy = AwkTestHarness.CreatePolicy()
            .WithMaxSyntaxNestingDepth(1_000_000).WithMaxFunctionCallDepth(scenario.FunctionDepth);
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var reads = 0;
        var suspendedRead = false;
        if (scenario.PendingReads)
            host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            suspendedRead = true;
            if (++reads == 500 && scenario.CancelOnRead)
                cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return reads > 1_000 ? AwkReadBuffer.EndOfFile
                : AwkReadBuffer.FromContent("x\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
        }
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [scenario.Source], []);
        if (!Awk.TryParse(invocation, out var awk))
            throw new InvalidOperationException("The stack-safety command was not recognized.");
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        var completedSynchronously = execution.IsCompleted;
        var canceled = false;
        var exit = 0;
        try { exit = execution.GetAwaiter().GetResult().Value; }
        catch (OperationCanceledException) when (scenario.CancelOnRead && cancellation.IsCancellationRequested)
        {
            if (reads != 500) throw;
            canceled = true;
        }
        return new(shape, governed, 256 << 10, exit,
            host.GetOutput(AwkFileDescriptor.StdOut), host.GetOutput(AwkFileDescriptor.StdErr),
            canceled, suspendedRead, completedSynchronously, host.OpenFileCount, budget.Reserved,
            host.FileExists("/out") ? host.ReadFile("/out") : "");
    }
}
