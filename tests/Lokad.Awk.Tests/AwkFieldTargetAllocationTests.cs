namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFieldTargetAllocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedFieldCopiesUseAlreadyFundedStorage(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        var separator = AwkText.FromSourceLiteralString(" ");
        using (var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, accounting,
            resolveOutputFieldSeparator: () => separator))
        {
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
            Assert.Equal(0, accounting.CurrentReservedBytes);

            void Copy()
            {
                if (!frame.TryCopyInputField(new(1), new(2)))
                    throw new InvalidOperationException("The existing field slices should remain available.");
            }
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void ExistingFieldTargetUpdatesAvoidPerRecordAllocation()
    {
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{values[$1]+=1}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(state);
        for (var warm = 0; warm < 32; warm++) Update();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 1024; row++) Update();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(allocated, 0, 1024);
        Assert.Equal(1063, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);

        void Update()
        {
            if (region.Execute(state, storage) != AwkStatementFlow.Continue)
                throw new InvalidOperationException("Unexpected native array-update control flow.");
        }
    }
}
