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
        AwkValue startValue, AwkValue lengthValue, int argumentCount)
    {
        using var destination = state.MemoryAccounting.CreateTransientLease();
        var source = new AwkByteTextOperand(state.FormatStringByteText(sourceValue));
        return state.CreateStringValue(EvaluateSubstrText(state, source, -1, startValue, lengthValue, argumentCount, destination));
    }

    internal static AwkText EvaluateSubstrText(AwkRuntimeState state, in AwkByteTextOperand source, int field,
        in AwkValue startValue, in AwkValue lengthValue, int argumentCount, AwkMemoryLease destination)
    {
        EvaluateSubstrSlice(state, source, field, startValue, lengthValue, argumentCount, destination,
            borrowOwned: false, out var slice, out var whole);
        return state.OwnStringResult(slice, field, whole, destination);
    }

    internal static void EvaluateSubstrSlice(AwkRuntimeState state, AwkByteTextOperand source, int field,
        in AwkValue startValue, in AwkValue lengthValue, int argumentCount, AwkMemoryLease destination,
        bool borrowOwned, out AwkByteTextOperand slice, out bool whole)
    {
        slice = default;
        whole = false;
        var zeroBasedStart = SubstringStart(state, in startValue);
        state.PrepareStringRangeSource(field, ref source, zeroBasedStart, 0);
        // Check the start before converting length: an out-of-range start has always
        // returned empty without demanding a potentially invalid numeric conversion.
        var byteMode = state.StringUnitMode == AwkStringUnitMode.Byte;
        if (!source.HasUnitAt(zeroBasedStart, byteMode, state.CancellationCheckpoint)) return;
        var length = SubstringLength(state, in lengthValue, argumentCount);
        if (length <= 0) return;
        state.PrepareStringRangeSource(field, ref source, zeroBasedStart, length);
        state.CancellationCheckpoint();
        slice = source.SliceUnits(zeroBasedStart, length, byteMode, state.Counters, state.MemoryAccounting,
            state.CancellationCheckpoint, destination, borrowOwned);
        whole = slice.ByteLength == source.ByteLength;
    }

    internal static int CountSubstring(AwkRuntimeState state, int sourceLength,
        in AwkValue start, in AwkValue length, int argumentCount)
    {
        var offset = SubstringStart(state, in start);
        if (offset >= sourceLength) return 0;
        var count = SubstringLength(state, in length, argumentCount);
        return count <= 0 ? 0 : Math.Min(sourceLength - offset, count);
    }

    private static int SubstringStart(AwkRuntimeState state, in AwkValue value)
    {
        var start = (int)(value.TryGetNumberValue(out var number) ? number : value.AsNumber(state.CancellationCheckpoint));
        return start < 1 ? 0 : start - 1;
    }

    private static int SubstringLength(AwkRuntimeState state, in AwkValue value, int argumentCount) =>
        argumentCount == 2 ? int.MaxValue :
            (int)(value.TryGetNumberValue(out var number) ? number : value.AsNumber(state.CancellationCheckpoint));

    internal static AwkValue EvaluateCaseValue(AwkRuntimeState state, AwkValue value, bool upper)
    {
        using var destination = state.MemoryAccounting.CreateTransientLease();
        return state.CreateStringValue(EvaluateCaseText(state, new(state.FormatStringByteText(value)), destination, upper));
    }

    internal static AwkText EvaluateCaseText(AwkRuntimeState state, in AwkByteTextOperand source,
        AwkMemoryLease destination, bool upper, int field = -1) =>
        EvaluateCaseConversion(source, field, state, destination,
            upper ? AwkCaseConversion.Upper : AwkCaseConversion.Lower);

    private static AwkText EvaluateCaseConversion(
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
            // Publish into the admitted result directly instead of copying all
            // bytes and then traversing the changed suffix again in place.
            if (firstChange > 0)
                AwkText.CopyBytes(bytes[..firstChange], converted.AsSpan(0, firstChange), checkpoint);
            for (var index = firstChange; index < converted.Length;)
            {
                var chunkOffset = index & (AwkRuntimeLimits.CancellationCheckBytes - 1);
                if (chunkOffset == 0)
                    checkpoint();
                var length = Math.Min(converted.Length - index, AwkRuntimeLimits.CancellationCheckBytes - chunkOffset);
                var input = bytes.Slice(index, length);
                var chunk = converted.AsSpan(index, length);
                if (isAscii)
                {
                    var status = conversion is AwkCaseConversion.Upper
                        ? Ascii.ToUpper(input, chunk, out var written)
                        : Ascii.ToLower(input, chunk, out written);
                    if (status != OperationStatus.Done || written != length)
                        throw new InvalidOperationException("Previously validated ASCII conversion failed.");
                }
                else
                {
                    var lower = conversion is AwkCaseConversion.Upper ? (byte)'a' : (byte)'A';
                    var delta = conversion is AwkCaseConversion.Upper ? -32 : 32;
                    for (var offset = 0; offset < chunk.Length; offset++)
                    {
                        var value = input[offset];
                        chunk[offset] = (uint)(value - lower) <= 25 ? (byte)(value + delta) : value;
                    }
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
            return unchanged ? state.OwnStringResult(source, field, destination: destination) : asciiResult;
        }

        state.Counters.RecordStringInteropFallback();
        var beforeSource = destination.Bytes;
        var ownedSource = state.OwnStringResult(source, field, destination: destination);
        var sourceBytes = destination.Bytes - beforeSource;
        var decoded = ownedSource.AsString(state.Counters, state.CancellationCheckpoint);
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
        var result = AwkText.FromUtf16(converted.AsSpan(), state.Counters,
            state.MemoryAccounting, state.CancellationCheckpoint, destination);
        // A borrowed source's owning copy is needed only through conversion.
        // The completed result keeps its own existing destination funding.
        if (sourceBytes > 0) destination.Release(sourceBytes);
        return result;
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
