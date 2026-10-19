using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal readonly partial record struct AwkRegexMatch
{
    internal static void ValidateProvidedRanges(ReadOnlySpan<byte> input, AwkRegexCaptureRange whole,
        ReadOnlySpan<AwkRegexCaptureRange> groups, AwkRegexSearchOffset searchOffset,
        AwkMemoryAccounting governor, Action checkpoint)
    {
        if (whole.ByteIndex < searchOffset.ByteIndex || whole.Utf16Index < searchOffset.Utf16Index ||
            groups.Length != 0 && groups[0] != whole) throw InvalidRange();
        var count = AwkRuntimeMemory.CheckedAdd(groups.Length, 1);
        count = AwkRuntimeMemory.CheckedAdd(count, count);
        using var scratch = governor.ReserveTransient(count <= 128 ? 0 :
            AwkRuntimeMemory.EstimateRegexCoordinateArrayBytes(count));
        Span<long> boundaries = count <= 128 ? stackalloc long[count] : new long[count];
        var used = 0;
        Add(whole, input.Length, boundaries, ref used);
        foreach (var group in groups)
        {
            checkpoint();
            Add(group, input.Length, boundaries, ref used);
        }
        boundaries = boundaries[..used];
        boundaries.Sort();
        var reuseSearchOffset = used != 0 && (int)(boundaries[0] >> 32) >= searchOffset.ByteIndex;
        var byteIndex = reuseSearchOffset ? searchOffset.ByteIndex : 0;
        var utf16Index = reuseSearchOffset ? searchOffset.Utf16Index : 0;
        long nextCheckpoint = byteIndex;
        foreach (var boundary in boundaries)
        {
            checkpoint();
            var target = (int)(boundary >> 32);
            while (byteIndex < target)
            {
                if (byteIndex >= nextCheckpoint)
                {
                    checkpoint();
                    nextCheckpoint = (long)byteIndex + AwkRuntimeLimits.CancellationCheckBytes;
                }
                if (Rune.DecodeFromUtf8(input[byteIndex..], out var rune, out var consumed) != OperationStatus.Done)
                    throw InvalidRange();
                byteIndex += consumed;
                utf16Index += rune.Utf16SequenceLength;
            }
            if (byteIndex != target || utf16Index != (int)(uint)boundary) throw InvalidRange();
        }

        static void Add(AwkRegexCaptureRange range, int length, Span<long> destination, ref int used)
        {
            if (!range.Success) return;
            AwkRegexScalarBoundary.ThrowIfUnaligned(range.IsByteAligned);
            if (range.ByteIndex < 0 || range.ByteIndex > length || range.ByteLength < 0 ||
                range.ByteLength > length - range.ByteIndex || range.Utf16Index < 0 ||
                range.Utf16Length < 0 || range.Utf16Index > length || range.Utf16Length > length - range.Utf16Index)
                throw InvalidRange();
            destination[used++] = ((long)range.ByteIndex << 32) | (uint)range.Utf16Index;
            destination[used++] = ((long)(range.ByteIndex + range.ByteLength) << 32) |
                (uint)(range.Utf16Index + range.Utf16Length);
        }
    }

    private static AwkRuntimeException InvalidRange() =>
        new(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);

    internal static AwkLeasedRegexMatch FromProvidedMatch(AwkRegexCaptureRange whole, ReadOnlySpan<byte> input,
        ReadOnlySpan<AwkRegexCaptureRange> captures, AwkRegexSearchOffset offset,
        AwkRegexCoordinatePolicy coordinatePolicy,
        long maxCaptureBytes, AwkRuntimeCounters counters, AwkMemoryAccounting governor, Action checkpoint)
    {
        ValidateProvidedRanges(input, whole, captures, offset, governor, checkpoint);
        var retainedBytes = 0L;
        if (governor.IsEnabled || maxCaptureBytes != AwkRuntimeLimits.Unlimited)
        {
            var captureBytes = 0L;
            foreach (var capture in captures)
            {
                checkpoint();
                if (capture.Success)
                {
                    captureBytes = AwkRuntimeMemory.CheckedAdd(captureBytes, capture.ByteLength);
                    retainedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes,
                        AwkRuntimeMemory.EstimateTextBytes(capture.ByteLength));
                }
                if (maxCaptureBytes != AwkRuntimeLimits.Unlimited && captureBytes > maxCaptureBytes)
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            }
        }
        var lease = governor.ReserveTransient(AwkRuntimeMemory.EstimateRegexMatchBytes(captures.Length, retainedBytes));
        try
        {
            checkpoint();
            var groups = captures.Length == 0 ? [] : new AwkRegexGroup[captures.Length];
            counters.RecordRegexGroupsMaterialized(groups.Length);
            for (var index = 0; index < captures.Length; index++)
            {
                if ((index & 1023) == 0) checkpoint();
                var capture = captures[index];
                var value = capture.Success
                    ? AwkText.CopyTrustedBytes(input.Slice(capture.ByteIndex, capture.ByteLength),
                        counters, AwkMemoryAccounting.Disabled, checkpoint)
                    : AwkText.Empty;
                groups[index] = new(capture.Success,
                    coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? capture.ByteIndex : capture.Utf16Index,
                    coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? capture.ByteLength : capture.Utf16Length, value);
            }
            checkpoint();
            return new(new(true,
                coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? whole.ByteIndex : whole.Utf16Index,
                coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? whole.ByteLength : whole.Utf16Length,
                whole.ByteIndex, whole.ByteLength, groups.Length == 0 ? AwkText.Empty : groups[0].Value, groups),
                lease, retainedBytes);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
