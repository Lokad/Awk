namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ManagedStringPreparationRetainsItsFundedCacheUntilScratchRetirement(bool governed)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Values: 1));
        var capacity = fixture.Budget.Reserved;
        var source = AwkText.FromHostBytes("/side"u8.ToArray());
        storage.StoreValue(0, state.CreateStringValue(source));
        var retained = fixture.Budget.Reserved;
        if (governed) Assert.True(retained > capacity);
        var decodes = state.Counters.StringDecodes;
        storage.PrepareString(0);
        var prepared = storage.PreparedString(0);
        Assert.Equal("/side", prepared);
        Assert.Same(prepared, storage.PreparedString(0));
        Assert.Equal(decodes + 1, state.Counters.StringDecodes);
        Assert.Equal(retained, fixture.Budget.Reserved);
        Assert.True(source.AsMemory().Equals(storage.Values[0].ByteText.AsMemory()));
        storage.ClearValues();
        Assert.Equal(capacity, fixture.Budget.Reserved);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task EscapingShortFieldPathSurvivesReplacementAndActualPendingRead(bool governed, bool cancel)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("/side\n");
        host.AddFile("/side", "7\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                entered.TrySetResult();
                await resume.Task.ConfigureAwait(false);
            }
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = Read; }
        }
        var budget = new CooperativeTestBudget(4 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function side(path,v){$0=\"gone\";saved=\"\";getline v<path;return v}{saved=$1;print side(saved)}"));
        var execution = tool.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.Equal(held, budget.Reserved);
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(cancel ? "" : "7\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution; } catch (OperationCanceledException) { }
        }
    }
}
