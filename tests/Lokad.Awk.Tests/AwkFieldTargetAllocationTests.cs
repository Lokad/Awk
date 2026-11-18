namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFieldTargetAllocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedFieldCopiesUseAlreadyFundedStorage(bool governed)
    {
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBaseline = fieldMemory.Accounting.LiveBytes;
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using (var context = new AwkFieldTestContext(fieldMemory))
            {
                fieldMemoryBaseline = context.BaselineBytes;
                fieldMemoryBudgetBaseline = context.BaselineBudget;
                var frame = context.Frame;
                frame.SetRecordText(AwkText.FromSourceLiteralString("left right"));
                for (var warm = 0; warm < 64; warm++) Copy();
                var peak = budget.Peak;
                var before = GC.GetAllocatedBytesForCurrentThread();

                for (var iteration = 0; iteration < 4096; iteration++) Copy();

                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.InRange(allocated, 0, 1024);
                Assert.Equal(peak, budget.Peak);
                Assert.Equal(2, frame.GetFieldCount());
                Assert.Equal("left left", frame.RecordAwkText.AsString());

                void Copy()
                {
                    if (!frame.TryCopyInputField(new(1), new(2)))
                        throw new InvalidOperationException("The existing field slices should remain available.");
                }
            }
            Assert.Equal(fieldMemoryBaseline, accounting.LiveBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Fact]
    public async Task ExistingFieldTargetUpdatesAvoidPerRecordAllocation()
    {
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.Arrays.SetAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{values[$1]+=1}"));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        for (var warm = 0; warm < 32; warm++) await UpdateAsync();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 1024; row++) await UpdateAsync();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(allocated, 0, 1024);
        Assert.Equal(1063, state.Arrays.GetAwkValue(name, key).AsNumber());

        async ValueTask UpdateAsync()
        {
            if (await region.ExecuteAsync(state, storage, regionOutput, regionIo) != AwkStatementFlow.Continue)
                throw new InvalidOperationException("Unexpected native array-update control flow.");
        }
    }
}
