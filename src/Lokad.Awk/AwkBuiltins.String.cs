using System;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

    private static AwkValue EvaluateCaseConversion(
        AwkBuiltinContext context,
        AwkCaseConversion conversion)
    {
        static bool TryConvertAsciiCase(
            AwkText source,
            AwkCaseConversion conversion,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor governor,
            Action checkpoint,
            out AwkText result)
        {
            static byte FoldAsciiCase(byte value, AwkCaseConversion conversion) =>
                conversion is AwkCaseConversion.Upper
                    ? value is >= (byte)'a' and <= (byte)'z'
                        ? (byte)(value - 32)
                        : value
                    : AwkByteFacts.ToAsciiLower(value);

            var bytes = source.AsSpan();
            var firstChange = -1;
            for (var index = 0; index < bytes.Length; index++)
            {
                if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                    checkpoint();
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
            {
                if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                    checkpoint();
                converted[index] = FoldAsciiCase(converted[index], conversion);
            }

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
            context.State.CancellationCheckpoint,
            out var asciiResult))
        {
            return context.State.CreateByteTextValue(asciiResult);
        }

        context.State.Counters.RecordStringInteropFallback();
        var decoded = source.AsString(context.State.Counters);
        context.State.ThrowIfCancellationRequested();
        var converted = conversion is AwkCaseConversion.Upper
            ? decoded.ToUpperInvariant()
            : decoded.ToLowerInvariant();
        context.State.ThrowIfCancellationRequested();
        return context.State.CreateStringValue(converted);
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
