using System;

namespace Lokad.Awk;

internal sealed class AwkTimeRuntime(Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds, TimeZoneInfo timeZone)
{
    // Host-supplied zone configuration is immutable invocation metadata. Cache
    // its rule view once instead of copying it during every date conversion.
    private readonly TimeZoneInfo.AdjustmentRule[] _rules = timeZone.GetAdjustmentRules();

    public TimeZoneInfo TimeZone { get; } = timeZone;

    public AwkUnixTimestampSeconds CurrentUnixTimeSeconds() => currentUnixTimeSeconds();

    public long MakeTime(AwkText specification, bool utc, AwkRuntimeResourceMonitor resources, AwkMemoryAccounting memoryAccounting) =>
        AwkCalendarTime.MakeTime(specification.AsSpan(), utc, TimeZone, _rules, resources, memoryAccounting);
}
