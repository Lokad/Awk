using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkIoCloseDirectionTests
{
    public static IEnumerable<object[]> PendingCalls()
    {
        foreach (var operation in new[] { "close", "fflush" })
            foreach (var statement in new[] { false, true })
                foreach (var governed in new[] { false, true })
                    foreach (var cancel in new[] { false, true }) yield return [operation, statement, governed, cancel];
    }

    [Theory]
    [MemberData(nameof(PendingCalls))]
    public async Task PendingCallsKeepTheirCallerAndStorageUntilActualCompletion(
        string operation, bool statement, bool governed, bool cancel)
    {
        var invocation = operation + "(\"out\")";
        var source = "function perform(){" + (statement ? invocation + ";r=0" : "r=" + invocation) +
            ";return r+7}BEGIN{print \"payload\">\"out\";print perform();print \"tail\"}";
        var host = new MockFileSystem();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.CloseDescriptorAsyncOverride = async (descriptor, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.CloseDescriptorAsyncOverride = null;
            return await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
        };
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource();
        var run = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal(1, host.OpenFileCount);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? "" : "7\ntail\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal("payload\n", host.ReadFile("/out"));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedCloseKeepsRecordOutputNativeAndStableNamesCached(bool governed)
    {
        const int records = 128;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("native row\n", records)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault())
        { MemoryBudget = governed ? budget : null };
        var command = AwkTestHarness.BuildInvocation("awk", "{print $0>\"out\";status+=close(\"out\")}END{print status}");
        Assert.Equal(AwkExitCode.Success, await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None, options));
        Assert.Equal("0\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("native row\n", host.ReadFile("/out"));
        Assert.Equal(records, counters[AwkCounter.RecordsRead]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.InRange(counters[AwkCounter.StringDecodes], 0, 4);
        // Each redirected record owns its payload until the host append completes.
        Assert.InRange(counters[AwkCounter.TextAllocations], records, records + 8);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> CloseCases()
    {
        var fileCases = AwkGoldenCorpus.CloseModes.Select(test => (test, CommandStatus: 0));
        foreach (var (test, commandStatus) in fileCases.Concat(AwkGoldenCorpus.CloseModePipeCases()))
            foreach (var governed in new[] { false, true })
                yield return [test, governed, commandStatus];
    }

    [Theory]
    [MemberData(nameof(CloseCases))]
    public async Task PublicClosePreservesStreamStatusAndLifetime(AwkGoldenCase test, bool governed, int commandStatus)
    {
        var files = AwkTestHarness.BuildFileSystem(test);
        var host = new AwkEmptyPipeTestHost(files) { InputRecord = "row\n", CommandStatus = commandStatus };
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", test.Arguments, test.Environment));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(test.ExpectedExitCode, (await awk.ExecuteAsync(host, policy, watchdog.Token)).Value);
        Assert.Equal(test.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));
        foreach (var file in test.ExpectedFiles) Assert.Equal(file.Content, files.ReadFile(file.Path));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
