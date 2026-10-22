namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRuntimeStartupTests
{
    [Theory]
    [InlineData(128)]
    [InlineData(1 << 20)]
    public void PlanningDoesNotCopyUnconsumedAssignmentValues(int length)
    {
        var command = AwkTestHarness.BuildInvocation("awk", "BEGIN{exit}", "a=" + new string('x', length));
        _ = AwkCommandParser.Parse(command);
        var before = GC.GetAllocatedBytesForCurrentThread();

        var invocation = AwkCommandParser.Parse(command);

        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 16 << 10);
        Assert.False(invocation.TerminalResult.IsTerminal);
        Assert.Equal(2, invocation.RuntimeArgumentsPlan.RuntimeArguments.Count);
    }

    [Fact]
    public void FunctionInventoryHasBoundedInitializationAllocation()
    {
        var options = AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault().WithTimeZone(TimeZoneInfo.Utc));
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>();
        using (var warm = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options))
            warm.InitializeFunctions(functions);
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var before = GC.GetAllocatedBytesForCurrentThread();

        state.InitializeFunctions(functions);

        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 8 << 10);
    }

    [Fact]
    public void DefaultOptionsDoNotCopyRandomState()
    {
        var policy = AwkExecutionPolicy.CreateDefault().WithTimeZone(TimeZoneInfo.Utc);
        _ = AwkRuntimeOptions.Create(policy).CreateRandomGenerator();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var reference = new AwkRandomGenerator();
        var generatorBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        var options = AwkRuntimeOptions.Create(policy);
        var generator = options.CreateRandomGenerator();
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, generatorBytes, generatorBytes + 1024);
        for (var index = 0; index < 32; index++)
            Assert.Equal(reference.NextDouble(), generator.NextDouble());
        Assert.Equal(new AwkRandomSeed(1), generator.Seed(new AwkRandomSeed(7)));
        Assert.Equal(new AwkRandomSeed(1), options.CreateRandomGenerator().Seed(new AwkRandomSeed(7)));
    }

    [Theory]
    [InlineData("unused", 0)]
    [InlineData("1970 1 1 0 0 0", 0)]
    [InlineData("1970 1 1 0 0 0 1", 0)]
    [InlineData("10000 1 1 0 0 0", 253402300800)]
    [InlineData("invalid", -1)]
    public void UtcAndUnusedTimeServicesDoNotCopyHostRules(string specification, long expected)
    {
        var zone = ManyRulesZone();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        var text = AwkText.FromString(specification);
        _ = new AwkTimeRuntime(static () => new(0), zone).MakeTime(text, true, resources, AwkMemoryAccounting.Disabled);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var runtime = new AwkTimeRuntime(static () => new(0), zone);
        if (specification != "unused")
            Assert.Equal(expected, runtime.MakeTime(text, true, resources, AwkMemoryAccounting.Disabled));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
        GC.KeepAlive(runtime);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.StringInteropFallbacks);
    }

    [Fact]
    public void ImplicitLocalTimeBorrowsHostRulesAndExplicitFlagsReuseTheirCopy()
    {
        var runtime = new AwkTimeRuntime(static () => new(0), ManyRulesZone());
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled);
        var implicitDate = AwkText.FromString("1970 7 1 0 0 0");
        var standardDate = AwkText.FromString("1970 7 1 0 0 0 0");
        var daylightDate = AwkText.FromString("1970 1 1 0 0 0 1");
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(15634800, runtime.MakeTime(implicitDate, false, resources, AwkMemoryAccounting.Disabled));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);

        Assert.Equal(15638400, runtime.MakeTime(standardDate, false, resources, AwkMemoryAccounting.Disabled));
        before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(-3600, runtime.MakeTime(daylightDate, false, resources, AwkMemoryAccounting.Disabled));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
    }

    private static TimeZoneInfo ManyRulesZone()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 1);
        var rules = new TimeZoneInfo.AdjustmentRule[5000];
        for (var year = 1; year <= rules.Length; year++)
            rules[year - 1] = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                new DateTime(year, 1, 1), new DateTime(year, 12, 31), TimeSpan.FromHours(1), start, end);
        return TimeZoneInfo.CreateCustomTimeZone("many-rules", TimeSpan.Zero, "many-rules", "standard", "daylight", rules);
    }
}
