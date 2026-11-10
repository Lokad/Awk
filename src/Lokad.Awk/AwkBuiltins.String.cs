using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

    internal static double EvaluateIndexNumber(AwkRuntimeState state, AwkValue source, AwkValue needle) =>
        EvaluateIndexText(state, new(state.FormatStringByteText(source.ForceScalar())), new(state.FormatStringByteText(needle)));

    internal static double EvaluateIndexText(AwkRuntimeState state, AwkByteTextOperand source, AwkByteTextOperand needle) =>
        state.IndexOfStringUnits(source, needle);

    internal static AwkValue EvaluateIndexValues(AwkRuntimeState state, AwkValue source, AwkValue needle) =>
        AwkValue.FromNumber(EvaluateIndexNumber(state, source, needle));

    internal static AwkValue EvaluateSubstrValues(AwkRuntimeState state, AwkValue sourceValue,
        AwkValue startValue, AwkValue lengthValue, int argumentCount) =>
        EvaluateSubstrOperand(state, new(sourceValue), startValue, lengthValue, argumentCount);

    internal static AwkValue EvaluateSubstrOperand(AwkRuntimeState state, in AwkScalarOperand sourceValue,
        AwkValue startValue, AwkValue lengthValue, int argumentCount)
    {
        var source = sourceValue.Text(state);
        var start = (int)startValue.AsNumber(state.CancellationCheckpoint);
        if (start < 1) start = 1;
        var zeroBasedStart = start - 1;
        sourceValue.PrepareRangeSource(state, ref source, zeroBasedStart, 0);
        // Check the start before converting length: an out-of-range start has always
        // returned empty without demanding a potentially invalid numeric conversion.
        if (!state.HasStringUnitAt(source, zeroBasedStart)) return AwkValue.FromText(string.Empty);
        var length = argumentCount == 2 ? int.MaxValue : (int)lengthValue.AsNumber(state.CancellationCheckpoint);
        if (length <= 0) return AwkValue.FromText(string.Empty);
        sourceValue.PrepareRangeSource(state, ref source, zeroBasedStart, length);
        var slice = state.SubstringByStringUnits(source, zeroBasedStart, length);
        return state.CreateStringValue(sourceValue.OwnSlice(state, slice, slice.ByteLength == source.ByteLength));
    }

    internal static AwkValue EvaluateCaseValue(AwkRuntimeState state, AwkValue value, bool upper)
    {
        using var destination = state.MemoryAccounting.CreateTransientLease();
        return EvaluateCaseText(state, new(state.FormatStringByteText(value)), destination, upper);
    }

    internal static AwkValue EvaluateCaseText(AwkRuntimeState state, in AwkByteTextOperand source,
        AwkMemoryLease destination, bool upper, int field = -1) =>
        EvaluateCaseConversion(source, field, state, destination,
            upper ? AwkCaseConversion.Upper : AwkCaseConversion.Lower);

    private static AwkValue EvaluateCaseConversion(
        in AwkByteTextOperand source,
        int field,
        AwkRuntimeState state,
        AwkMemoryLease destination,
        AwkCaseConversion conversion)
    {
        static bool TryConvertAsciiCase(
            in AwkByteTextOperand source,
            AwkCaseConversion conversion,
            bool byteMode,
            AwkRuntimeCounters counters,
            AwkMemoryAccounting governor,
            AwkMemoryLease destination,
            Action checkpoint,
            out AwkText result,
            out bool unchanged)
        {
            unchanged = false;
            var bytes = source.AsSpan(checkpoint);
            var firstChange = -1;
            var isAscii = true;
            for (var index = 0; index < bytes.Length;)
            {
                checkpoint();
                var length = Math.Min(bytes.Length - index, AwkRuntimeLimits.CancellationCheckBytes);
                var chunk = bytes.Slice(index, length);
                if (!Ascii.IsValid(chunk))
                {
                    if (!byteMode)
                    {
                        result = default;
                        return false;
                    }
                    isAscii = false;
                }
                counters.RecordAsciiCasePreparationChunk(length);
                if (firstChange < 0)
                {
                    var change = conversion is AwkCaseConversion.Upper
                        ? chunk.IndexOfAnyInRange((byte)'a', (byte)'z')
                        : chunk.IndexOfAnyInRange((byte)'A', (byte)'Z');
                    if (change >= 0)
                        firstChange = index + change;
                }
                index += length;
            }

            if (firstChange < 0)
            {
                result = default;
                unchanged = true;
                return true;
            }

            var converted = AwkText.AllocateOwnedBytes(bytes.Length, governor, destination, checkpoint);
            AwkText.CopyBytes(bytes, converted, checkpoint);
            for (var index = firstChange; index < converted.Length;)
            {
                var chunkOffset = index & (AwkRuntimeLimits.CancellationCheckBytes - 1);
                if (chunkOffset == 0)
                    checkpoint();
                var length = Math.Min(converted.Length - index, AwkRuntimeLimits.CancellationCheckBytes - chunkOffset);
                var chunk = converted.AsSpan(index, length);
                if (isAscii)
                {
                    var status = conversion is AwkCaseConversion.Upper
                        ? Ascii.ToUpperInPlace(chunk, out var written)
                        : Ascii.ToLowerInPlace(chunk, out written);
                    if (status != OperationStatus.Done || written != length)
                        throw new InvalidOperationException("Previously validated ASCII conversion failed.");
                }
                else
                {
                    var lower = conversion is AwkCaseConversion.Upper ? (byte)'a' : (byte)'A';
                    var delta = conversion is AwkCaseConversion.Upper ? -32 : 32;
                    for (var offset = 0; offset < chunk.Length; offset++)
                        if ((uint)(chunk[offset] - lower) <= 25)
                            chunk[offset] = (byte)(chunk[offset] + delta);
                }
                counters.RecordAsciiCaseConversionChunk(length);
                index += length;
            }

            result = isAscii
                ? AwkText.FromTrustedAsciiOwnedBytes(converted, counters)
                : AwkText.FromTrustedOwnedBytes(converted, counters);
            return true;
        }

        if (TryConvertAsciiCase(
            source,
            conversion,
            state.StringUnitMode is AwkStringUnitMode.Byte,
            state.Counters,
            state.MemoryAccounting,
            destination,
            state.CancellationCheckpoint,
            out var asciiResult,
            out var unchanged))
        {
            return state.CreateStringValue(unchanged ? state.OwnStringResult(source, field) : asciiResult);
        }

        state.Counters.RecordStringInteropFallback();
        var decoded = state.OwnStringResult(source, field)
            .AsString(state.Counters, state.CancellationCheckpoint);
        state.ThrowIfCancellationRequested();
        // Invariant casing preserves UTF-16 length. Keep the conversion result funded
        // until its UTF-8 copy is constructed; the source owns its decoding cache.
        using var scratch = state.MemoryAccounting.ReserveAllocationPeak(
            AwkRuntimeMemory.EstimateUtf16StringBytes(decoded.Length));
        state.ThrowIfCancellationRequested();
        var converted = conversion is AwkCaseConversion.Upper
            ? decoded.ToUpperInvariant()
            : decoded.ToLowerInvariant();
        state.ThrowIfCancellationRequested();
        return state.CreateStringValue(AwkText.FromUtf16(converted.AsSpan(), state.Counters,
            state.MemoryAccounting, state.CancellationCheckpoint, destination));
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
