namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Fact]
    public void PublishedRecordRefundCancellationStopsAnEmptyMainProgram()
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
        using var storage = new AwkRegionStorage(state);
        var machine = new AwkRegionMachine(state, program.MainProgram, storage, null, null);
        machine.Initialize();
        var refunded = false;
        memory.Budget.OnRefund = () => { refunded = true; cancellation.Cancel(); };
        try
        {
            var next = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("next"),
                AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
            var error = Assert.ThrowsAny<OperationCanceledException>(() =>
            {
                machine.BeginMainRecord(in next);
                machine.Run();
            });
            Assert.True(refunded);
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        finally
        {
            memory.Budget.OnRefund = null;
            machine.Cleanup();
        }
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData("{x=7}")] [InlineData("{}")]
    [InlineData("1{x=7}")] [InlineData("$1>0{x=7}")]
    public void StandaloneMainEntryObservesCancellationBeforeEffects(string source)
    {
        using var cancellation = new CancellationTokenSource();
        using var state = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        var program = Bind(source);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var region = program.MainProgram;
        var x = AwkVariableName.FromText("x");
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("1"));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        cancellation.Cancel();
        var error = Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(5, state.ResolveVariableValue(x).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
