namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkGensubAllocationTests
{
    [Fact]
    public void SparseReplacementDoesNotRentOneInstructionPerLiteralByte()
    {
        var text = AwkText.FromString(new string('x', 1_000_000) + "\\1");
        var before = GC.GetAllocatedBytesForCurrentThread();
        using var plan = AwkGensubReplacementPlan.Rent(text);
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 64 * 1024);
        Assert.Equal(2, plan.CapturePlan.SelectGroupCount(10));
    }
}
