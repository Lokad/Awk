using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

    internal static AwkValue EvaluateIndexValues(AwkRuntimeState state, AwkValue source, AwkValue needle) =>
        AwkValue.FromNumber(state.IndexOfStringUnits(state.FormatStringByteText(source.ForceScalar()), state.FormatStringByteText(needle)));

    internal static AwkValue EvaluateSubstrValues(AwkRuntimeState state, AwkValue sourceValue,
        AwkValue startValue, AwkValue lengthValue, int argumentCount)
    {
        var source = state.FormatStringByteText(sourceValue);
        var start = (int)startValue.AsNumber(state.CancellationCheckpoint);
        if (start < 1) start = 1;
        var sourceLength = state.CountStringUnits(source);
        if (start > sourceLength) return AwkValue.FromText(string.Empty);
        var zeroBasedStart = start - 1;
        if (argumentCount == 2)
            return state.CreateByteTextValue(state.SubstringByStringUnits(source, zeroBasedStart, sourceLength - zeroBasedStart));
        var length = (int)lengthValue.AsNumber(state.CancellationCheckpoint);
        if (length <= 0) return AwkValue.FromText(string.Empty);
        return state.CreateByteTextValue(state.SubstringByStringUnits(source, zeroBasedStart, Math.Min(length, sourceLength - zeroBasedStart)));
    }

    internal static AwkValue EvaluateCaseValue(AwkRuntimeState state, AwkValue value, bool upper) =>
        EvaluateCaseConversion(state.FormatStringByteText(value), state, upper ? AwkCaseConversion.Upper : AwkCaseConversion.Lower);

    private static AwkValue EvaluateCaseConversion(
        AwkText source,
        AwkRuntimeState state,
        AwkCaseConversion conversion)
    {
        static bool TryConvertAsciiCase(
            AwkText source,
            AwkCaseConversion conversion,
            AwkRuntimeCounters counters,
            AwkMemoryAccounting governor,
            Action checkpoint,
            out AwkText result)
        {
            var bytes = source.AsSpan(checkpoint);
            var firstChange = -1;
            for (var index = 0; index < bytes.Length;)
            {
                checkpoint();
                var length = Math.Min(bytes.Length - index, AwkExecutionGovernor.WorkChunkBytes);
                var chunk = bytes.Slice(index, length);
                if (!Ascii.IsValid(chunk))
                {
                    result = AwkText.Empty;
                    return false;
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
                result = source;
                return true;
            }

            var converted = AwkText.AllocateOwnedBytes(bytes.Length, governor);
            AwkText.CopyBytes(bytes, converted, checkpoint);
            for (var index = firstChange; index < converted.Length;)
            {
                var chunkOffset = index & (AwkExecutionGovernor.WorkChunkBytes - 1);
                if (chunkOffset == 0)
                    checkpoint();
                var length = Math.Min(converted.Length - index, AwkExecutionGovernor.WorkChunkBytes - chunkOffset);
                var chunk = converted.AsSpan(index, length);
                var status = conversion is AwkCaseConversion.Upper
                    ? Ascii.ToUpperInPlace(chunk, out var written)
                    : Ascii.ToLowerInPlace(chunk, out written);
                if (status != OperationStatus.Done || written != length)
                    throw new InvalidOperationException("Previously validated ASCII conversion failed.");
                counters.RecordAsciiCaseConversionChunk(length);
                index += length;
            }

            result = AwkText.FromTrustedAsciiOwnedBytes(converted, counters);
            return true;
        }

        if (TryConvertAsciiCase(
            source,
            conversion,
            state.Counters,
            state.MemoryAccounting,
            state.CancellationCheckpoint,
            out var asciiResult))
        {
            return state.CreateByteTextValue(asciiResult);
        }

        state.Counters.RecordStringInteropFallback();
        var decoded = source.AsString(state.Counters, state.CancellationCheckpoint);
        state.ThrowIfCancellationRequested();
        var converted = conversion is AwkCaseConversion.Upper
            ? decoded.ToUpperInvariant()
            : decoded.ToLowerInvariant();
        state.ThrowIfCancellationRequested();
        return state.CreateStringValue(converted);
    }

    private enum AwkCaseConversion
    {
        Lower,
        Upper
    }
}
