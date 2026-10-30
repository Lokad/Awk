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
        ReadOnlySpan<AwkRegexCaptureRange> groups, int searchOffset, Action checkpoint)
    {
        if (whole.ByteIndex < searchOffset || groups.Length != 0 && groups[0] != whole) throw InvalidRange();
        Validate(input, whole);
        foreach (var group in groups)
        {
            checkpoint();
            Validate(input, group);
        }
        checkpoint();

        static void Validate(ReadOnlySpan<byte> bytes, AwkRegexCaptureRange range)
        {
            if (!range.Success) return;
            if (range.ByteIndex < 0 || range.ByteIndex > bytes.Length || range.ByteLength < 0 ||
                range.ByteLength > bytes.Length - range.ByteIndex) throw InvalidRange();
            // AwkText has already validated the subject. Boundaries need no coordinate map.
            AwkRegexScalarBoundary.ThrowIfUnaligned(IsBoundary(bytes, range.ByteIndex) &&
                IsBoundary(bytes, range.ByteIndex + range.ByteLength));
        }
    }

    internal static bool IsBoundary(ReadOnlySpan<byte> bytes, int index) =>
        index == bytes.Length || (bytes[index] & 0xC0) != 0x80;

    private static AwkRuntimeException InvalidRange() =>
        new(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);

    internal static AwkLeasedRegexMatch FromRanges(AwkRegexCaptureRange whole, ReadOnlySpan<byte> input,
        ReadOnlySpan<AwkRegexCaptureRange> captures, int offset,
        AwkRegexCoordinatePolicy coordinatePolicy, AwkRuntimeCounters counters, AwkMemoryAccounting governor,
        Action checkpoint, ref AwkRegexScalarCoordinates cursor, bool includePositions = true)
    {
        ValidateRanges(input, whole, captures, offset, checkpoint);
        var coordinateCount = coordinatePolicy == AwkRegexCoordinatePolicy.Scalar && includePositions
            ? AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.CheckedAdd(captures.Length, 1), 2)) : 0;
        using var scratch = governor.ReserveTransient(coordinateCount <= 128 ? 0 :
            AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.EstimateRegexCoordinateArrayBytes(coordinateCount), 2));
        Span<long> boundaries = coordinateCount <= 128 ? stackalloc long[coordinateCount] : new long[coordinateCount];
        Span<int> positions = coordinateCount <= 128 ? stackalloc int[coordinateCount] : new int[coordinateCount];
        positions.Clear();
        if (coordinateCount != 0)
        {
            var used = 0;
            Add(whole, 0, boundaries, ref used);
            for (var index = 0; index < captures.Length; index++)
            {
                checkpoint();
                Add(captures[index], (index + 1) * 2, boundaries, ref used);
            }
            boundaries = boundaries[..used];
            boundaries.Sort();
            foreach (var boundary in boundaries)
            {
                var target = (int)(boundary >> 32);
                positions[(int)(uint)boundary] = cursor.MoveTo(input, target, counters, checkpoint);
            }
        }
        var retainedBytes = 0L;
        if (governor.IsEnabled)
            foreach (var capture in captures)
            {
                checkpoint();
                if (capture.Success) retainedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes,
                    AwkRuntimeMemory.EstimateTextBytes(capture.ByteLength));
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
                        counters, AwkMemoryAccounting.Disabled, checkpoint) : AwkText.Empty;
                var start = coordinateCount == 0 ? capture.ByteIndex : positions[(index + 1) * 2];
                var length = coordinateCount == 0 ? capture.ByteLength : positions[(index + 1) * 2 + 1] - start;
                groups[index] = new(capture.Success, start, length, value);
            }
            checkpoint();
            var wholeStart = coordinateCount == 0 ? whole.ByteIndex : positions[0];
            var wholeLength = coordinateCount == 0 ? whole.ByteLength : positions[1] - wholeStart;
            return new(new(true, wholeStart, wholeLength, whole.ByteIndex, whole.ByteLength,
                groups.Length == 0 ? AwkText.Empty : groups[0].Value, groups), lease, retainedBytes);
        }
        catch { lease.Dispose(); throw; }

        static void Add(AwkRegexCaptureRange range, int slot, Span<long> destination, ref int used)
        {
            if (!range.Success) return;
            destination[used++] = ((long)range.ByteIndex << 32) | (uint)slot;
            destination[used++] = ((long)(range.ByteIndex + range.ByteLength) << 32) | (uint)(slot + 1);
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

internal readonly record struct AwkRegexMatchPosition(bool Success, int Index, int Length)
{
    public static AwkRegexMatchPosition FromSpan(AwkRegexMatchSpan match, AwkText input,
        AwkRegexCoordinatePolicy policy, AwkRuntimeCounters counters, Action checkpoint)
    {
        if (!match.Success || policy == AwkRegexCoordinatePolicy.Byte)
            return new(match.Success, match.ByteIndex, match.ByteLength);
        var cursor = new AwkRegexScalarCoordinates();
        var bytes = input.AsSpan(checkpoint);
        var start = cursor.MoveTo(bytes, match.ByteIndex, counters, checkpoint);
        var end = cursor.MoveTo(bytes, match.ByteIndex + match.ByteLength, counters, checkpoint);
        return new(true, start, end - start);
    }
}

internal enum AwkRegexCoordinatePolicy { Scalar, Byte }

internal readonly record struct AwkRegexMatchSpan(bool Success, int ByteIndex, int ByteLength)
{
    public static AwkRegexMatchSpan NoMatch => default;
    public static AwkRegexMatchSpan Match(int byteIndex, int byteLength) => new(true, byteIndex, byteLength);

    public static int AfterMatch(AwkText input, AwkRegexMatchSpan match, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var end = match.ByteIndex + match.ByteLength;
        if (match.ByteLength != 0 || end == input.ByteLength) return end;
        if (Rune.DecodeFromUtf8(input.AsSpan(checkpoint)[end..], out _, out var consumed) != OperationStatus.Done)
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
        return end + consumed;
    }
}

// Only language-facing position publication needs scalar counting. Repeated capture
// enumeration reuses this small cursor; no mandatory whole-subject index is retained.
internal struct AwkRegexScalarCoordinates
{
    private int _byteOffset;
    private int _scalarOffset;

    public int MoveTo(ReadOnlySpan<byte> input, int target, AwkRuntimeCounters counters, Action checkpoint)
    {
        var before = _byteOffset;
        long next = _byteOffset;
        while (_byteOffset < target)
        {
            if (_byteOffset >= next) { checkpoint(); next = (long)_byteOffset + AwkRuntimeLimits.CancellationCheckBytes; }
            if (Rune.DecodeFromUtf8(input[_byteOffset..], out _, out var consumed) != OperationStatus.Done)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _byteOffset += consumed;
            _scalarOffset++;
        }
        next = _byteOffset;
        while (_byteOffset > target)
        {
            if (_byteOffset <= next) { checkpoint(); next = (long)_byteOffset - AwkRuntimeLimits.CancellationCheckBytes; }
            if (Rune.DecodeLastFromUtf8(input[.._byteOffset], out _, out var consumed) != OperationStatus.Done)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _byteOffset -= consumed;
            _scalarOffset--;
        }
        counters.RecordRegexCoordinateBytes(Math.Abs(target - before));
        checkpoint();
        return _scalarOffset;
    }
}

internal readonly record struct AwkRegexCapturePlan
{
    private readonly int _maximumGroupCount;
    private AwkRegexCapturePlan(int maximumGroupCount, bool includesPositions = false)
    {
        _maximumGroupCount = maximumGroupCount;
        IncludesPositions = includesPositions;
    }
    public bool IncludesPositions { get; }
    public static AwkRegexCapturePlan None => default;
    public static AwkRegexCapturePlan All { get; } = new(int.MaxValue, includesPositions: true);
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
