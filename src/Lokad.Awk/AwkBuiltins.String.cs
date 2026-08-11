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

    private static AwkValue EvaluateToLower(AwkBuiltinContext context) =>
        EvaluateCaseConversion(context, AwkCaseConversion.Lower);

    private static AwkValue EvaluateToUpper(AwkBuiltinContext context) =>
        EvaluateCaseConversion(context, AwkCaseConversion.Upper);

    private static AwkValue EvaluateCaseConversion(
        AwkBuiltinContext context,
        AwkCaseConversion conversion)
    {
        static bool TryConvertAsciiCase(
            AwkText source,
            AwkCaseConversion conversion,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor governor,
            out AwkText result)
        {
            static byte FoldAsciiCase(byte value, AwkCaseConversion conversion) =>
                conversion is AwkCaseConversion.Upper
                    ? value is >= (byte)'a' and <= (byte)'z'
                        ? (byte)(value - 32)
                        : value
                    : value is >= (byte)'A' and <= (byte)'Z'
                        ? (byte)(value + 32)
                        : value;

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

                if (FoldAsciiCase(value, conversion) != value && firstChange < 0)
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
                converted[index] = FoldAsciiCase(converted[index], conversion);

            result = AwkText.FromTrustedOwnedBytes(converted, counters);
            return true;
        }

        if (context.ArgumentCount != 1)
            return AwkValue.FromText(string.Empty);

        var source = context.GetByteText(0);
        if (TryConvertAsciiCase(
            source,
            conversion,
            context.State.Counters,
            context.State.MemoryGovernor,
            out var asciiResult))
        {
            return context.State.CreateByteTextValue(asciiResult);
        }

        context.State.Counters.RecordStringInteropFallback();
        var decoded = source.AsString(context.State.Counters);
        return context.State.CreateStringValue(conversion is AwkCaseConversion.Upper
            ? decoded.ToUpperInvariant()
            : decoded.ToLowerInvariant());
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
