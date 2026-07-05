using System;

namespace Lokad.Awk;

internal sealed class AwkTimeRuntime(Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds, TimeZoneInfo timeZone)
{
    public TimeZoneInfo TimeZone { get; } = timeZone;

    public AwkUnixTimestampSeconds CurrentUnixTimeSeconds() => currentUnixTimeSeconds();
}
