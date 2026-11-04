namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFieldTargetAllocationTests
{
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
