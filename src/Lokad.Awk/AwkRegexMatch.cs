using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal readonly record struct AwkRegexMatch
{
    private readonly AwkText _value;
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
        _value = value;
        _groups = groups.Count == 0 ? null : groups;
    }

    public bool Success { get; }

    public int Index { get; }

    public int Length { get; }

    public int ByteIndex { get; }

    public int ByteLength { get; }

    public AwkText Value => _value;

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
    private readonly AwkText _value;

    public AwkRegexGroup(bool success, int index, int length, AwkText value)
    {
        Success = success;
        Index = index;
        Length = length;
        _value = value;
    }

    public bool Success { get; }

    public int Index { get; }

    public int Length { get; }

    public AwkText Value => _value;
}

internal readonly record struct AwkRegexMatchPosition(bool Success, int Index, int Length)
{
    public static AwkRegexMatchPosition FromSpan(AwkRegexMatchSpan match, AwkText input, AwkRegexCoordinatePolicy policy, Action checkpoint)
    {
        if (!match.Success || policy == AwkRegexCoordinatePolicy.Byte)
            return new(match.Success, match.ByteIndex, match.ByteLength);
        var cursor = new AwkRegexScalarCoordinates();
        var bytes = input.AsSpan(checkpoint);
        var start = cursor.MoveTo(bytes, match.ByteIndex, checkpoint);
        var end = cursor.MoveTo(bytes, match.ByteIndex + match.ByteLength, checkpoint);
        return new(true, start, end - start);
    }
}

internal enum AwkRegexCoordinatePolicy { Scalar, Byte }

internal readonly record struct AwkRegexMatchSpan(bool Success, int ByteIndex, int ByteLength)
{
    public static AwkRegexMatchSpan NoMatch => default;
    public static AwkRegexMatchSpan Match(int byteIndex, int byteLength) => new(true, byteIndex, byteLength);

    public static int AfterMatch(ReadOnlySpan<byte> input, AwkRegexMatchSpan match, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var end = match.ByteIndex + match.ByteLength;
        if (match.ByteLength != 0 || end == input.Length) return end;
        if (Rune.DecodeFromUtf8(input[end..], out _, out var consumed) != OperationStatus.Done)
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

    public int MoveTo(ReadOnlySpan<byte> input, int target, Action checkpoint)
    {
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
