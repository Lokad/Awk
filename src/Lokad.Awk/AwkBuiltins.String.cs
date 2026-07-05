using System;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateLength(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(context.State.CountStringUnits(context.GetByteText(0)));
    }

    private static AwkValue EvaluateIndex(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(context.State.IndexOfStringUnits(context.GetByteText(0), context.GetByteText(1)));
    }

    private static AwkValue EvaluateSubstr(AwkBuiltinContext context)
    {
        if (context.ArgumentCount is < 2 or > 3)
            return AwkValue.FromText(string.Empty);

        var source = context.GetByteText(0);
        var start = (int)context.GetNumber(1);
        if (start < 1)
            start = 1;

        var sourceLength = context.State.CountStringUnits(source);
        if (start > sourceLength)
            return AwkValue.FromText(string.Empty);

        var zeroBasedStart = start - 1;
        if (context.ArgumentCount == 2)
            return context.State.CreateByteTextValue(
                context.State.SubstringByStringUnits(source, zeroBasedStart, sourceLength - zeroBasedStart));

        var length = (int)context.GetNumber(2);
        if (length <= 0)
            return AwkValue.FromText(string.Empty);

        var boundedLength = Math.Min(length, sourceLength - zeroBasedStart);
        return context.State.CreateByteTextValue(context.State.SubstringByStringUnits(source, zeroBasedStart, boundedLength));
    }

    private static AwkValue EvaluateToLower(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromText(string.Empty);

        if (TryConvertAsciiCase(
            context.GetByteText(0),
            toUpper: false,
            context.State.Counters,
            context.State.MemoryGovernor,
            out var asciiLower))
        {
            return context.State.CreateByteTextValue(asciiLower);
        }

        context.State.Counters.RecordStringInteropFallback();
        return context.State.CreateStringValue(context.GetString(0).ToLowerInvariant());
    }

    private static AwkValue EvaluateToUpper(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromText(string.Empty);

        if (TryConvertAsciiCase(
            context.GetByteText(0),
            toUpper: true,
            context.State.Counters,
            context.State.MemoryGovernor,
            out var asciiUpper))
        {
            return context.State.CreateByteTextValue(asciiUpper);
        }

        context.State.Counters.RecordStringInteropFallback();
        return context.State.CreateStringValue(context.GetString(0).ToUpperInvariant());
    }

    private static bool TryConvertAsciiCase(
        AwkText source,
        bool toUpper,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        out AwkText result)
    {
        var bytes = source.AsSpan();
        var firstChange = -1;
        for (var index = 0; index < bytes.Length; index++)
        {
            var value = bytes[index];
            if (value > 0x7F)
            {
                result = AwkText.Empty;
                return false;
            }

            if (FoldAsciiCase(value, toUpper) != value && firstChange < 0)
                firstChange = index;
        }

        if (firstChange < 0)
        {
            result = source;
            return true;
        }

        var converted = AwkText.AllocateOwnedBytes(bytes.Length, governor);
        bytes.CopyTo(converted);
        for (var index = firstChange; index < converted.Length; index++)
        {
            converted[index] = FoldAsciiCase(converted[index], toUpper);
        }

        result = AwkText.FromTrustedOwnedBytes(converted, counters);
        return true;
    }

    private static byte FoldAsciiCase(byte value, bool toUpper) =>
        toUpper
            ? value is >= (byte)'a' and <= (byte)'z'
                ? (byte)(value - 32)
                : value
            : value is >= (byte)'A' and <= (byte)'Z'
                ? (byte)(value + 32)
                : value;
}
