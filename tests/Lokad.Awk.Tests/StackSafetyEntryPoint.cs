using System.Text.Json;

namespace Lokad.Awk.Tests;

internal static class StackSafetyEntryPoint
{
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--token-endpoint" && args[1] is "first" or "last")
            return RunOnSmallStack(() => TokenEndpoint(args[1]));
        if (args.Length != 3 || args[0] != "--stack-safety" || !bool.TryParse(args[2], out var governed))
            return 64;
        var scenario = AwkStackSafetyScenario.Create(args[1]);
        return RunOnSmallStack(() => Execute(args[1], governed, scenario));
    }

    private static int RunOnSmallStack<T>(Func<T> execute)
    {
        try
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try { completion.SetResult(execute()); }
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

    private static int TokenEndpoint(string endpoint)
    {
        AwkToken[] trivia = [new(AwkTokenKind.Comment, "# padding", new(0, 9))];
        IReadOnlyList<AwkToken> tokens = [new AwkToken(AwkTokenKind.Identifier, "value", new(7, 5))];
        for (var index = 0; index < 30_000; index++)
            tokens = AwkTokenNavigation.Flatten([trivia, tokens, trivia]);
        var token = endpoint == "first" ? AwkTokenNavigation.FindFirstSignificant(tokens)
            : AwkTokenNavigation.FindLastSignificant(tokens);
        return token?.Span.Start ?? -1;
    }

    private static AwkStackSafetyObservation Execute(string shape, bool governed, AwkStackSafetyScenario scenario)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(scenario.Input);
        foreach (var file in scenario.Files)
            host.AddFile(file.Path, file.Content, file.Permission);
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
