using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkReferenceSelectionTests
{
    [Theory]
    [InlineData("projection", "small", false, 1)]
    [InlineData("projection", "all", false, 3)]
    [InlineData("aggregate-composite", "all", false, 3)]
    [InlineData("general-call-loop", "all", false, 3)]
    [InlineData("function-control-flow", "all", false, 3)]
    [InlineData("projection", "small", true, 1)]
    [InlineData("*", "control", false, 2)]
    [InlineData("startup-empty", "small", false, 0)]
    [InlineData("startup-empty", "small", true, 1)]
    [InlineData("unknown", "all", true, 0)]
    [InlineData("projection", "unknown", true, 0)]
    [InlineData("*", "small", true, 44)]
    public void FiltersPreserveSelectedCasesAndControlOrdering(string name, string scale, bool smoke, int count)
    {
        var workloads = ReferenceWorkloads.CreateAll(name, scale, smoke).ToArray();
        Assert.Equal(count, workloads.Length);
        Assert.All(workloads, workload =>
        {
            Assert.True(name == "*" || workload.Name == name);
            Assert.True(scale == "all" || workload.Scale == scale || smoke && workload.Scale == "control");
            if (workload.Scale != "control")
            {
                var direct = ReferenceWorkloads.Create(workload.Name, workload.Scale);
                Assert.Equal(direct.Program, workload.Program);
                Assert.Equal(direct.InputBytes, workload.InputBytes);
                Assert.Equal(direct.ExpectedDigest, workload.ExpectedDigest);
            }
        });
        if (name == "*" && smoke)
            Assert.Equal(new[] { "startup-empty", "startup-tiny" }, workloads.TakeLast(2).Select(w => w.Name));
    }

    [Fact]
    public void SingleSmallSelectionDoesNotConstructUnselectedLargeInputs()
    {
        _ = ReferenceWorkloads.CreateAll("projection", "small").ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var selected = ReferenceWorkloads.CreateAll("projection", "small").ToArray();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("projection", Assert.Single(selected).Name);
        Assert.InRange(allocated, 1, 64 * 1024);
    }
}
