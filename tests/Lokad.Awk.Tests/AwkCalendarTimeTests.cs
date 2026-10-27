namespace Lokad.Awk.Tests;

public sealed class AwkCalendarTimeTests
{
    [Theory]
    [InlineData(false, "2020 1 1 2 0 0", 1577840400)]
    [InlineData(false, "2020 1 1 2 0 0 0", 1577840400)]
    [InlineData(false, "2020 1 1 2 0 0 1", 1577836800)]
    [InlineData(false, "2020 7 1 2 0 0", 1593561600)]
    [InlineData(false, "2020 7 1 2 0 0 0", 1593565200)]
    [InlineData(false, "2020 7 1 2 0 0 1", 1593561600)]
    [InlineData(false, "2020 3 1 2 30 0", 1583026200)]
    [InlineData(false, "2020 3 1 2 30 0 1", 1583022600)]
    [InlineData(false, "2020 10 1 1 30 0", 1601512200)]
    [InlineData(false, "2020 10 1 1 30 0 1", 1601508600)]
    [InlineData(true, "2020 3 1 1 30 0", 1583026200)]
    [InlineData(true, "2020 3 1 1 30 0 0", 1583022600)]
    [InlineData(true, "2020 3 1 1 30 0 1", 1583026200)]
    [InlineData(true, "2020 10 1 2 30 0", 1601515800)]
    [InlineData(true, "2020 10 1 2 30 0 1", 1601519400)]
    public void InjectedZoneControlsSeasonalExplicitAndAmbiguousOffsets(bool negative, string specification, long expected)
    {
        var state = State(Zone(negative));
        Assert.Equal(expected, state.MakeCalendarTime(AwkText.FromString(specification), false));
    }

    [Fact]
    public void AmbiguousSelectionDoesNotDependOnPriorCalls()
    {
        var state = State(Zone(false));
        var ambiguous = AwkText.FromString("2020 10 1 1 30 0");
        var before = state.MakeCalendarTime(ambiguous, false);
        _ = state.MakeCalendarTime(AwkText.FromString("2020 7 1 2 0 0"), false);
        Assert.Equal(before, state.MakeCalendarTime(ambiguous, false));
        _ = state.MakeCalendarTime(AwkText.FromString("2020 1 1 2 0 0"), false);
        Assert.Equal(before, state.MakeCalendarTime(ambiguous, false));
    }

    [Theory]
    [InlineData("0 1 1 0 0 0", -62167219200)]
    [InlineData("10000 1 1 0 0 0", 253402300800)]
    public void UtcAndFixedZonesExtendBeyondDynamicZoneRuleRange(string specification, long expected)
    {
        var text = AwkText.FromString(specification);
        Assert.Equal(expected, State(Zone(false)).MakeCalendarTime(text, true));
        Assert.Equal(-1, State(Zone(false)).MakeCalendarTime(text, false));
        var fixedZone = TimeZoneInfo.CreateCustomTimeZone("fixed", TimeSpan.FromMinutes(90), "fixed", "fixed");
        Assert.Equal(expected - 5400, State(fixedZone).MakeCalendarTime(text, false));
    }

    [Fact]
    public void StandardBaseOffsetAdjustmentAndHalfHourDstAreHonored()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 1);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31),
            TimeSpan.FromMinutes(30), start, end, TimeSpan.FromMinutes(15));
        var zone = TimeZoneInfo.CreateCustomTimeZone("adjusted", TimeSpan.FromHours(1), "adjusted", "standard", "daylight", [rule]);
        var state = State(zone);
        Assert.Equal(1577846700, state.MakeCalendarTime(AwkText.FromString("2020 1 1 4 0 0 0"), false));
        Assert.Equal(1577844900, state.MakeCalendarTime(AwkText.FromString("2020 1 1 4 0 0 1"), false));
    }

    [Fact]
    public void BaseOffsetOnlyRulesRetainDynamicRangeAndExplicitFlagBehavior()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 1);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31),
            TimeSpan.Zero, start, end, TimeSpan.FromMinutes(15));
        var zone = TimeZoneInfo.CreateCustomTimeZone("base-only", TimeSpan.FromHours(1), "base-only", "standard", "daylight", [rule]);
        using var state = State(zone);
        foreach (var suffix in new[] { "", " 0" })
            Assert.Equal(1577846700, state.MakeCalendarTime(AwkText.FromString("2020 1 1 4 0 0" + suffix), false));
        Assert.Equal(1577843100, state.MakeCalendarTime(AwkText.FromString("2020 1 1 4 0 0 1"), false));
        Assert.Equal(-1, state.MakeCalendarTime(AwkText.FromString("10000 1 1 0 0 0"), false));
    }

    [Fact]
    public void DateConversionRemainsByteNativeWithBoundedWork()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(TimeZoneInfo.Utc, counters);
        var text = AwkText.FromString(new string(' ', 4096) + "1970 13 1 0 0 0");
        Assert.Equal(31536000, state.MakeCalendarTime(text, true));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.AwkTextAllocations);
    }

    [Fact]
    public void AmbiguousOffsetBufferIsGovernedAndReleasedOnFailure()
    {
        const int limit = 1 << 20;
        using var memory = new CooperativeTestMemory(limit);
        var options = new AwkRuntimeOptions(static () => new(0), Zone(false), new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options))).State;
        var text = AwkText.FromString("2020 10 1 1 30 0");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Equal(1601512200, state.MakeCalendarTime(text, false));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        using var pressure = state.MemoryAccounting.ReserveTransient(limit - before - AwkRuntimeMemory.EstimateTimeZoneOffsetsBytes() + 1);
        var pressured = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Throws<AwkRuntimeException>(() => state.MakeCalendarTime(text, false));
        Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void DateParsingChecksCallerCancellationBeforeScanning()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance));
        Assert.Throws<OperationCanceledException>(() => state.MakeCalendarTime(AwkText.FromString("1970 1 1 0 0 0"), true));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CanceledDatePreparationDoesNotMaterializeLazyText(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        var text = AwkText.FromString("1970 1 1 0 0 0 ")
            .Concat(AwkText.FromString(new string(' ', 1_000_000)), counters, memory.Accounting);
        var runtime = new AwkTimeRuntime(static () => new(0), TimeZoneInfo.Utc);
        using var cancellation = new CancellationTokenSource();
        var resources = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters, cancellation.Token);
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => runtime.MakeTime(text, true, resources, memory.Accounting));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, memory.Budget.Reserved);
        var active = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        Assert.Equal(0, runtime.MakeTime(text, true, active, memory.Accounting));
        Assert.Equal(text.ByteLength, counters.ConcatenationBytesCopied);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static TimeZoneInfo Zone(bool negative)
    {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1900, 1, 1), new DateTime(2099, 12, 31),
            TimeSpan.FromHours(negative ? -1 : 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 1));
        return TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromHours(1), "test", "standard", "daylight", [rule]);
    }

    private static AwkRuntimeState State(TimeZoneInfo zone, AwkRuntimeCounters? counters = null, AwkRuntimeLimits? limits = null) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), zone,
                new AwkRandomGenerator(), counters ?? AwkRuntimeCounters.Disabled,
                limits ?? AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance));
}
