using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal readonly record struct AwkRegexMatch
{
    private readonly AwkText? _value;
    private readonly IReadOnlyList<AwkRegexGroup>? _groups;

    public AwkRegexMatch(
        bool success,
        int index,
        int length,
        int byteIndex,
        int byteLength,
        AwkText value,
        IReadOnlyList<AwkRegexGroup> groups)
    {
        Success = success;
        Index = index;
        Length = length;
        ByteIndex = byteIndex;
        ByteLength = byteLength;
        _value = value.ByteLength == 0 ? null : value;
        _groups = groups.Count == 0 ? null : groups;
    }

    public bool Success { get; }

    public int Index { get; }

    public int Length { get; }

    public int ByteIndex { get; }

    public int ByteLength { get; }

    public AwkText Value => _value ?? AwkText.Empty;

    public IReadOnlyList<AwkRegexGroup> Groups => _groups ?? [];

    public static AwkRegexMatch NoMatch { get; } =
        new(false, 0, 0, 0, 0, AwkText.Empty, []);


    internal static void ValidateRanges(ReadOnlySpan<byte> input, AwkRegexCaptureRange whole,
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

    internal static AwkLeasedRegexMatch FromRanges(AwkRegexCaptureRange whole, ReadOnlySpan<byte> input,
        ReadOnlySpan<AwkRegexCaptureRange> captures, AwkRegexSearchOffset offset,
        AwkRegexCoordinatePolicy coordinatePolicy, AwkRuntimeCounters counters, AwkMemoryAccounting governor, Action checkpoint)
    {
        ValidateRanges(input, whole, captures, offset, governor, checkpoint);
        var retainedBytes = 0L;
        if (governor.IsEnabled)
        {

            foreach (var capture in captures)
            {
                checkpoint();
                if (capture.Success)
                {

                    retainedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes,
                        AwkRuntimeMemory.EstimateTextBytes(capture.ByteLength));
                }

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

internal readonly struct AwkLeasedRegexMatch(
    AwkRegexMatch match,
    AwkMemoryLease memoryLease,
    long retainedTextMemoryBytes) : IDisposable
{
    private readonly AwkMemoryLease? _memoryLease = memoryLease;

    public static AwkLeasedRegexMatch NoMatch { get; } =
        new(AwkRegexMatch.NoMatch, AwkMemoryLease.Disabled, retainedTextMemoryBytes: 0);

    public AwkRegexMatch Match { get; } = match;

    public AwkMemoryLease MemoryLease => _memoryLease ?? AwkMemoryLease.Disabled;

    public long RetainedTextMemoryBytes { get; } = retainedTextMemoryBytes;

    public void Dispose() => MemoryLease.Dispose();
}

internal readonly record struct AwkRegexGroup
{
    private readonly AwkText? _value;

    public AwkRegexGroup(bool success, int index, int length, AwkText value)
    {
        Success = success;
        Index = index;
        Length = length;
        _value = value.ByteLength == 0 ? null : value;
    }

    public bool Success { get; }

    public int Index { get; }

    public int Length { get; }

    public AwkText Value => _value ?? AwkText.Empty;
}

internal readonly record struct AwkRegexMatchPosition(
    bool Success,
    int Index,
    int Length)
{
    public static AwkRegexMatchPosition FromSpan(
        AwkRegexMatchSpan match,
        AwkRegexCoordinatePolicy coordinatePolicy) =>
        coordinatePolicy == AwkRegexCoordinatePolicy.Byte
            ? new AwkRegexMatchPosition(match.Success, match.ByteIndex, match.ByteLength)
            : new AwkRegexMatchPosition(match.Success, match.Utf16Index, match.Utf16Length);
}

internal enum AwkRegexCoordinatePolicy
{
    Utf16,
    Byte
}

internal readonly record struct AwkRegexMatchSpan(
    bool Success,
    int ByteIndex,
    int ByteLength,
    int Utf16Index,
    int Utf16Length)
{
    public static AwkRegexMatchSpan NoMatch { get; } = new(false, 0, 0, 0, 0);

    public static AwkRegexMatchSpan Match(int byteIndex, int byteLength) =>
        new(true, byteIndex, byteLength, byteIndex, byteLength);

    public static AwkRegexMatchSpan Match(
        int byteIndex,
        int byteLength,
        int utf16Index,
        int utf16Length) =>
        new(true, byteIndex, byteLength, utf16Index, utf16Length);
}

internal readonly record struct AwkRegexSearchOffset(int ByteIndex, int Utf16Index)
{
    public static AwkRegexSearchOffset Start { get; } = new(0, 0);

    public static AwkRegexSearchOffset AfterMatch(AwkText input, AwkRegexMatchSpan match, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (match.ByteLength > 0 || match.ByteIndex >= input.ByteLength)
        {
            return new AwkRegexSearchOffset(
                match.ByteIndex + match.ByteLength,
                match.Utf16Index + match.Utf16Length);
        }

        var status = Rune.DecodeFromUtf8(input.AsSpan(checkpoint)[match.ByteIndex..], out var rune, out var bytesConsumed);
        return status == OperationStatus.Done && bytesConsumed > 0
            ? new AwkRegexSearchOffset(
                match.ByteIndex + bytesConsumed,
                match.Utf16Index + rune.Utf16SequenceLength)
            : new AwkRegexSearchOffset(match.ByteIndex + 1, match.Utf16Index + 1);
    }
}

internal readonly record struct AwkRegexCapturePlan
{
    private readonly int _maximumGroupCount;

    private AwkRegexCapturePlan(int maximumGroupCount)
    {
        _maximumGroupCount = maximumGroupCount;
    }

    public static AwkRegexCapturePlan None => default;

    public static AwkRegexCapturePlan All { get; } = new(int.MaxValue);

    public static AwkRegexCapturePlan ThroughGroup(int maximumGroupIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumGroupIndex);
        return new AwkRegexCapturePlan(checked(maximumGroupIndex + 1));
    }

    public int SelectGroupCount(int availableGroupCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(availableGroupCount);
        return Math.Min(_maximumGroupCount, availableGroupCount);
    }
}
