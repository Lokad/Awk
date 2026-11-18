namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Fact]
    public async Task PublishedRecordRefundCancellationStopsAnEmptyMainProgram()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(4 << 20);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(),
            cancellation.Token, memory.Owner));
        var state = memory.State;
        var program = Bind("{}");
        state.AssignVariableValue(AwkVariableName.FromText("NR"), AwkValue.FromNumber(0));
        state.AssignVariableValue(AwkVariableName.FromText("FNR"), AwkValue.FromNumber(0));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(new string('a', 65536)));
        var host = new MockFileSystem();
        host.SetStandardInput("next\n");
        var io = AwkTestHarness.CreateRegionIo(state, host, cancellation.Token);
        using var executor = new AwkRuntimeActionExecutor(program, state);
        var input = AwkTestHarness.BindRegionInput("{}", executor, state, io, host, cancellation.Token);
        var refunded = false;
        memory.Budget.OnRefund = () =>
        {
            if (state.RecordNumber == 0) return;
            refunded = true;
            cancellation.Cancel();
        };
        try
        {
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => input.ProcessRemainingInputAsync());
            Assert.True(refunded);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(1, state.RecordNumber);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        }
        finally
        {
            memory.Budget.OnRefund = null;
            await input.CloseRemainingDescriptorsForCleanupAsync();
            await io.CloseForCleanupAsync();
        }
        executor.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData("{x=7}")] [InlineData("{}")]
    [InlineData("1{x=7}")] [InlineData("$1>0{x=7}")]
    public async Task MainEntryObservesCancellationBeforeEffects(string source)
    {
        using var cancellation = new CancellationTokenSource();
        using var state = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        var program = Bind(source);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var x = AwkVariableName.FromText("x");
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("1"));
        var host = new MockFileSystem();
        host.SetStandardInput("1\n");
        var io = AwkTestHarness.CreateRegionIo(state, host, cancellation.Token);
        using var executor = new AwkRuntimeActionExecutor(program, state);
        var input = AwkTestHarness.BindRegionInput(source, executor, state, io, host, cancellation.Token);
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => input.ProcessRemainingInputAsync());
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(5, state.ResolveVariableValue(x).AsNumber());
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        await input.CloseRemainingDescriptorsForCleanupAsync();
        await io.CloseForCleanupAsync();
    }
}
