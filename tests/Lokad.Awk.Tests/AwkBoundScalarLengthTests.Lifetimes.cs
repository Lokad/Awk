namespace Lokad.Awk.Tests;

public sealed partial class AwkBoundScalarLengthTests
{
    [Fact]
    public void HiddenLocalLengthUsesTheVisibleNameThenRestoresTheLocal()
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("p");
        var layout = new AwkFunctionLocalLayout([name]);
        var parsed = AwkParser.Parse("BEGIN{result=length(p)}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules),
            AwkLoweringScope.Function(layout), -1, out var region));
        var global = new AwkArrayName(name.Identifier);
        for (var index = 1; index <= 3; index++)
            state.SetArrayString(global, AwkArrayKey.FromOrdinal(index), "kept");
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("é😀"))]);
        using var storage = new AwkRegionStorage(state);
        try
        {
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
            Assert.Equal(2, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            using (state.EnterGlobalBindings())
            {
                Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
                Assert.Equal(3, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            }
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
            Assert.Equal(2, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            Assert.True(state.Counters.BoundScalarLengths >= 2);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { state.PopFunctionFrame(); }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task LocalLengthsKeepTheirBindingsThroughPendingHostWork(bool governed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(governed, cancellation.Token);
        var state = memory.State;
        var region = Bind(state, "function size(x,a,line){print length(x),length(a);" +
            "getline line<\"/side\";return length(x)+length(a)}" +
            "BEGIN{a[\"k\"]=1;answer=\"keep\";answer=size(\"é😀\",a)}");
        var host = new MockFileSystem();
        host.AddFile("/side", "ready\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("2 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
            var held = memory.Budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(memory.Budget.Reserved >= held);
            release.TrySetResult();
            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var answer = state.ResolveVariableValue(AwkVariableName.FromText("answer"));
            Assert.Equal(cancel ? "keep" : "3", answer.AsString());
            Assert.False(state.IsInsideFunction);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }
}
