using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

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
        slice = source.SliceUnits(zeroBasedStart, length, byteMode, state, destination, borrowOwned);
        whole = slice.ByteLength == source.ByteLength;
    }

    private static int SubstringStart(AwkRuntimeState state, in AwkValue value)
    {
        var start = (int)(value.TryGetNumberValue(out var number) ? number : value.AsNumber(state.CancellationCheckpoint));
        return start < 1 ? 0 : start - 1;
    }

    private static int SubstringLength(AwkRuntimeState state, in AwkValue value, int argumentCount) =>
        argumentCount == 2 ? int.MaxValue :
            (int)(value.TryGetNumberValue(out var number) ? number : value.AsNumber(state.CancellationCheckpoint));

    internal static AwkText EvaluateCaseText(AwkRuntimeState state, in AwkByteTextOperand source,
        AwkMemoryLease destination, bool upper, int field = -1) =>
        EvaluateCaseConversion(source, field, state, destination,
            upper ? AwkCaseConversion.Upper : AwkCaseConversion.Lower);

    internal static bool TryWriteShortAsciiCase(AwkRuntimeState state, in AwkByteTextOperand source,
        bool upper, AwkUtf8Builder scratch)
    {
        var bytes = source.AsSpan(state.CancellationCheckpoint);
        if (!Ascii.IsValid(bytes) || (upper ? bytes.IndexOfAnyInRange((byte)'a', (byte)'z') :
            bytes.IndexOfAnyInRange((byte)'A', (byte)'Z')) < 0) return false;

        // Admission may retire a host borrow. Resolve the same record range
        // after growth before reading the input into the frame's scratch.
        var captured = source.TryGetRecordSource(out var record) ? record : new AwkCopySource(bytes);
        var result = scratch.PrepareEmptyBuffer(bytes.Length);
        state.ThrowIfCancellationRequested();
        bytes = captured.AsSpan();
        var status = upper ? Ascii.ToUpper(bytes, result, out var written) : Ascii.ToLower(bytes, result, out written);
        if (status != OperationStatus.Done || written != result.Length)
            throw new InvalidOperationException("Previously validated ASCII conversion failed.");
        scratch.PublishLength(written);
        return true;
    }

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
            AwkRuntimeState state,
            AwkMemoryLease destination,
            out AwkText result,
            out bool unchanged)
        {
            unchanged = false;
            var bytes = source.AsSpan(state.CancellationCheckpoint);
            var firstChange = -1;
            var isAscii = true;
            for (var index = 0; index < bytes.Length;)
            {
                state.CancellationCheckpoint();
                var length = Math.Min(bytes.Length - index, AwkRuntimeLimits.CancellationCheckBytes);
                var chunk = bytes.Slice(index, length);
                if (!Ascii.IsValid(chunk))
                {
                    if (state.StringUnitMode is not AwkStringUnitMode.Byte)
                    {
                        result = default;
                        return false;
                    }
                    isAscii = false;
                }

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

            // Result admission can promote a borrowed record before the conversion reads it again.
            var inputSource = source.TryGetRecordSource(out var captured) ? captured : new AwkCopySource(bytes);
            var converted = AwkText.AllocateOwnedBytes(bytes.Length, state.MemoryAccounting, destination, state.CancellationCheckpoint);
            bytes = inputSource.AsSpan();
            // Publish into the admitted result directly instead of copying all
            // bytes and then traversing the changed suffix again in place.
            if (firstChange > 0)
                AwkText.CopyBytes(bytes[..firstChange], converted.AsSpan(0, firstChange), state.CancellationCheckpoint);
            for (var index = firstChange; index < converted.Length;)
            {
                var chunkOffset = index & (AwkRuntimeLimits.CancellationCheckBytes - 1);
                if (chunkOffset == 0)
                    state.CancellationCheckpoint();
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

                index += length;
            }

            result = state.AdoptText(converted, isAscii);
            return true;
        }

        if (TryConvertAsciiCase(
            source,
            conversion,
            state,
            destination,
            out var asciiResult,
            out var unchanged))
        {
            return unchanged ? state.OwnStringResult(source, field, destination: destination) : asciiResult;
        }

        if (source.TryGetOwned(out var owned))
        {
            var decoded = state.DecodeText(owned);
            state.ThrowIfCancellationRequested();
            using var casing = state.MemoryAccounting.ReserveTransient(
                AwkRuntimeMemory.EstimateUtf16StringBytes(decoded.Length));
            return EncodeCaseResult(decoded, state, destination, conversion);
        }

        // Borrowed UTF-8 can be decoded without publishing a value to cache its
        // string. Its byte length bounds the UTF-16 length of both strings.
        var stringBytes = AwkRuntimeMemory.EstimateUtf16StringBytes(source.ByteLength);
        using var scratch = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(stringBytes, stringBytes));
        state.ThrowIfCancellationRequested();
        var bytes = source.AsSpan(state.CancellationCheckpoint);
        state.ThrowIfCancellationRequested();
        var temporary = AwkUtf8Codec.DecodeRuntimeText(bytes);
        state.Counters.Add(AwkCounter.StringDecodes);
        return EncodeCaseResult(temporary, state, destination, conversion);
    }

    private static AwkText EncodeCaseResult(string decoded, AwkRuntimeState state,
        AwkMemoryLease destination, AwkCaseConversion conversion)
    {
        state.ThrowIfCancellationRequested();
        var converted = conversion is AwkCaseConversion.Upper
            ? decoded.ToUpperInvariant()
            : decoded.ToLowerInvariant();
        state.ThrowIfCancellationRequested();
        return state.EncodeText(converted, destination);
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
