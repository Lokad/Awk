namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFieldTargetAllocationTests
{
    [Fact]
    public void ExistingFieldTargetUpdatesAvoidPerRecordAllocation()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        var target = AwkReference.ArrayElement(name, AwkArrayIndex.Simple(
            AwkExpression.FieldIndex(AwkExpression.Number(1))));
        var right = AwkExpression.Number(1);
        var context = new AwkEvaluationContext(state);
        for (var warm = 0; warm < 32; warm++) Update();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var row = 0; row < 1024; row++) Update();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(allocated, 0, 1024);
        Assert.Equal(1063, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);

        void Update()
        {
            var result = target.ApplyCompoundAssignmentAsync(context, right, AwkCompoundAssignmentOperation.Add);
            if (!result.IsCompletedSuccessfully) throw new InvalidOperationException("A ready numeric update must complete synchronously.");
            _ = result.Result;
        }
    }
}
