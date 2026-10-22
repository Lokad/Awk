using System;

namespace Lokad.Awk;

internal sealed class AwkTimeRuntime(Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds, TimeZoneInfo timeZone)
{
    private TimeZoneInfo.AdjustmentRule[]? _rules;

    public TimeZoneInfo TimeZone { get; } = timeZone;

    // Explicit local DST flags need the rule metadata. Other conversions borrow
    // the immutable host zone without copying its rule array.
    public ReadOnlySpan<TimeZoneInfo.AdjustmentRule> AdjustmentRules =>
        _rules ??= TimeZone.GetAdjustmentRules();

    public AwkUnixTimestampSeconds CurrentUnixTimeSeconds() => currentUnixTimeSeconds();

    public long MakeTime(AwkText specification, bool utc, AwkRuntimeResourceMonitor resources, AwkMemoryAccounting memoryAccounting) =>
        AwkCalendarTime.MakeTime(specification.AsSpan(resources.Checkpoint), utc, this, resources, memoryAccounting);
}
