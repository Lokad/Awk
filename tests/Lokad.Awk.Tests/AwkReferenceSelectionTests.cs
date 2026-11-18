using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkReferenceSelectionTests
{
    [Theory]
    [InlineData("projection", "small", false, 1)]
    [InlineData("projection", "all", false, 3)]
    [InlineData("projection", "steady", false, 1)]
    [InlineData("long-fields", "steady", false, 0)]
    [InlineData("aggregate-composite", "all", false, 3)]
    [InlineData("general-call-loop", "all", false, 3)]
    [InlineData("function-control-flow", "all", false, 3)]
    [InlineData("nested-array-calls-16", "all", false, 3)]
    [InlineData("nested-array-calls-1024", "all", false, 3)]
    [InlineData("scalar-sum", "all", false, 3)]
    [InlineData("scalar-sum-decimal", "all", false, 3)]
    [InlineData("aggregate-decimal", "all", false, 3)]
    [InlineData("field-call-loop", "all", false, 3)]
    [InlineData("field-number-loop", "all", false, 3)]
    [InlineData("numeric-wide", "all", false, 3)]
    [InlineData("numeric-filter", "all", false, 3)]
    [InlineData("numeric-output", "all", false, 3)]
    [InlineData("unicode-prefix", "all", false, 3)]
    [InlineData("fixed-field-loop", "all", false, 3)]
    [InlineData("prefix-nf-loop", "all", false, 3)]
    [InlineData("wide-field-rewrite", "all", false, 3)]
    [InlineData("projection", "small", true, 1)]
    [InlineData("*", "control", false, 2)]
    [InlineData("startup-empty", "small", false, 0)]
    [InlineData("startup-empty", "small", true, 1)]
    [InlineData("unknown", "all", true, 0)]
    [InlineData("projection", "unknown", true, 0)]
    public void FiltersPreserveSelectedCasesAndControlOrdering(string name, string scale, bool smoke, int count)
    {
        var workloads = ReferenceWorkloads.Select(name, scale, smoke).ToArray();
        Assert.Equal(count, workloads.Length);
        Assert.All(workloads, workload =>
        {
            Assert.True(name == "*" || workload.Name == name);
            Assert.True(scale == "all" || workload.Scale == scale || smoke && workload.Scale == "control");
        });
        if (name == "*" && smoke)
            Assert.Equal(new[] { "startup-empty", "startup-tiny" }, workloads.TakeLast(2).Select(w => w.Name));
    }

    [Fact]
    public void SmallSmokeSelectsEveryRegisteredFamilyBeforeStartupControls()
    {
        var selected = ReferenceWorkloads.Select("*", "small", includeControls: true).ToArray();
        var processing = selected[..^2];
        Assert.Equal(ReferenceWorkloads.Families, processing.Select(workload => workload.Name));
        Assert.All(processing, workload => Assert.Equal("small", workload.Scale));
        Assert.Equal(new[] { "startup-empty", "startup-tiny" }, selected.TakeLast(2).Select(workload => workload.Name));
        Assert.All(selected.TakeLast(2), workload => Assert.Equal("control", workload.Scale));
    }

    [Fact]
    public void SingleSmallSelectionDoesNotConstructUnselectedLargeInputs()
    {
        _ = ReferenceWorkloads.Select("projection", "small").ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var selected = ReferenceWorkloads.Select("projection", "small").ToArray();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("projection", Assert.Single(selected).Name);
        Assert.InRange(allocated, 1, 64 * 1024);
    }

    [Fact]
    public void SteadyScaleSelectsOnlyTheExplicitProcessingCohort()
    {
        var steady = ReferenceWorkloads.Select(scale: "steady").ToArray();
        Assert.Equal(new[] { "projection", "filter", "scalar-sum", "aggregate", "two-file", "csv" },
            steady.Select(selection => selection.Name));
        Assert.All(steady, selection => Assert.Equal("steady", selection.Scale));
        Assert.DoesNotContain(ReferenceWorkloads.Select(), selection => selection.Scale == "steady");
        Assert.Throws<ArgumentException>(() => ReferenceWorkloads.Create("long-fields", "steady"));
    }
}
