using System;
using System.Globalization;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateRand(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 0)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(context.State.NextRandomNumber());
    }

    private static AwkValue EvaluateSrand(AwkBuiltinContext context)
    {
        if (context.ArgumentCount > 1)
            return AwkValue.FromNumber(0);

        var seed = context.ArgumentCount == 1
            ? AwkRandomSeedRequest.Explicit(new AwkRandomSeed((long)context.GetNumber(0)))
            : AwkRandomSeedRequest.CurrentTime;

        return AwkValue.FromNumber(context.State.SeedRandom(seed).Value);
    }

    private static AwkValue EvaluateSystime(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 0)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(context.State.CurrentUnixTimeSeconds().Value);
    }

    private static AwkValue EvaluateMktime(AwkBuiltinContext context)
    {
        static double MakeTime(string dateSpec, bool utc, TimeZoneInfo timeZone)
        {
            var text = dateSpec.AsSpan();
            Span<int> values = stackalloc int[6];
            var position = 0;
            for (var index = 0; index < values.Length; index++)
            {
                while (position < text.Length && char.IsWhiteSpace(text[position]))
                    position++;

                var partStart = position;
                while (position < text.Length && !char.IsWhiteSpace(text[position]))
                    position++;

                if (partStart == position)
                    return -1;

                if (!int.TryParse(
                        text[partStart..position],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out values[index]))
                {
                    return -1;
                }
            }

            try
            {
                if (utc)
                {
                    return new DateTimeOffset(
                            values[0],
                            values[1],
                            values[2],
                            values[3],
                            values[4],
                            values[5],
                            TimeSpan.Zero)
                        .ToUnixTimeSeconds();
                }

                var local = new DateTime(
                    values[0],
                    values[1],
                    values[2],
                    values[3],
                    values[4],
                    values[5],
                    DateTimeKind.Unspecified);
                return new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUnixTimeSeconds();
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        if (context.ArgumentCount is < 1 or > 2)
            return AwkValue.FromNumber(-1);

        var makeUtc = context.ArgumentCount == 2 && context.GetValue(1).IsTruthy();
        context.State.Counters.RecordStringInteropFallback();
        return AwkValue.FromNumber(MakeTime(context.GetString(0), makeUtc, context.State.TimeZone));
    }

    private static AwkValue EvaluateStrftime(AwkBuiltinContext context)
    {
        static string FormatTime(
            string format,
            AwkUnixTimestampSeconds timestamp,
            bool utc,
            TimeZoneInfo timeZone)
        {
            var value = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
            if (!utc)
                value = TimeZoneInfo.ConvertTime(value, timeZone);

            return GnuDateFormatting.Format(value, format);
        }

        if (context.ArgumentCount > 3)
            return AwkValue.FromText(string.Empty);

        context.State.Counters.RecordStringInteropFallback();
        var format = context.ArgumentCount >= 1
            ? context.GetString(0)
            : context.State.ResolveProcInfo(
                AwkBuiltins.Name(AwkBuiltinId.Strftime),
                "%a %b %e %H:%M:%S %Z %Y");
        var timestamp = context.ArgumentCount >= 2
            ? new AwkUnixTimestampSeconds((long)context.GetNumber(1))
            : context.State.CurrentUnixTimeSeconds();
        var utc = context.ArgumentCount >= 3 && context.GetValue(2).IsTruthy();
        return context.State.CreateStringValue(FormatTime(format, timestamp, utc, context.State.TimeZone));
    }
}
