using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lokad.Utf8Regex;

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

    public static AwkLeasedRegexMatch FromMatch(
        Match match,
        string input,
        int byteIndex,
        int byteLength,
        AwkRegexCoordinatePolicy coordinatePolicy,
        AwkRegexCapturePlan capturePlan,
        long maxCaptureBytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        int[]? groupMap = null) =>
        FromCaptures(new AwkRegexCaptureView(match, input, groupMap), input, byteIndex, byteLength,
            coordinatePolicy, capturePlan, maxCaptureBytes, counters, governor);

    internal static AwkLeasedRegexMatch FromCaptures(
        AwkRegexCaptureView selected, string input, int byteIndex, int byteLength,
        AwkRegexCoordinatePolicy coordinatePolicy, AwkRegexCapturePlan capturePlan,
        long maxCaptureBytes, AwkRuntimeCounters counters, AwkMemoryGovernor governor)
    {
        const int MaxStackBoundaryCount = 128;

        static long PackCoordinates(int sortCoordinate, int payload) =>
            ((long)(uint)sortCoordinate << 32) | (uint)payload;

        static int SortCoordinate(long packed) =>
            unchecked((int)(packed >> 32));

        static int Payload(long packed) =>
            unchecked((int)packed);

        static long MeasureRetainedTextMemory(
            long maxCaptureBytes,
            AwkRegexCaptureView groups,
            int groupCount)
        {
            var captureBytes = 0L;
            var retainedTextBytes = 0L;
            for (var index = 0; index < groupCount; index++)
            {
                var byteCount = Encoding.UTF8.GetByteCount(groups[index].ValueSpan);
                captureBytes = AwkRuntimeMemory.CheckedAdd(
                    captureBytes,
                    byteCount);
                retainedTextBytes = AwkRuntimeMemory.CheckedAdd(
                    retainedTextBytes,
                    AwkRuntimeMemory.EstimateTextBytes(byteCount));
                if (maxCaptureBytes != AwkRuntimeLimits.Unlimited && captureBytes > maxCaptureBytes)
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            }

            return retainedTextBytes;
        }

        static void ResolveByteBoundaries(
            AwkRegexCaptureView selected,
            string input,
            int matchByteIndex,
            int groupCount,
            Span<long> boundaries)
        {
            var match = selected.Whole;
            var minimumUtf16Index = match.Index;
            for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                var group = selected[groupIndex];
                var start = group.Success ? group.Index : match.Index;
                var end = group.Success ? group.Index + group.Length : match.Index;
                minimumUtf16Index = Math.Min(minimumUtf16Index, start);
                boundaries[groupIndex * 2] = PackCoordinates(start, groupIndex * 2);
                boundaries[(groupIndex * 2) + 1] = PackCoordinates(end, (groupIndex * 2) + 1);
            }

            boundaries.Sort();
            var utf16Cursor = minimumUtf16Index;
            var byteCursor = matchByteIndex - Encoding.UTF8.GetByteCount(
                input.AsSpan(minimumUtf16Index, match.Index - minimumUtf16Index));
            for (var index = 0; index < boundaries.Length; index++)
            {
                var packed = boundaries[index];
                var utf16Index = SortCoordinate(packed);
                byteCursor = AwkRuntimeMemory.CheckedAdd(
                    byteCursor,
                    Encoding.UTF8.GetByteCount(input.AsSpan(utf16Cursor, utf16Index - utf16Cursor)));
                utf16Cursor = utf16Index;
                boundaries[index] = PackCoordinates(Payload(packed), byteCursor);
            }

            boundaries.Sort();
        }

        if (!selected.Success)
            return AwkLeasedRegexMatch.NoMatch;

        var whole = selected.Whole;
        var groupCount = capturePlan.SelectGroupCount(selected.Count);
        AwkRegexScalarBoundary.ThrowIfSplit(input, whole.Index, whole.Length);
        for (var index = 0; index < groupCount; index++)
        {
            var group = selected[index];
            if (group.Success)
                AwkRegexScalarBoundary.ThrowIfSplit(input, group.Index, group.Length);
        }

        var retainedTextMemoryBytes = !governor.IsEnabled && maxCaptureBytes == AwkRuntimeLimits.Unlimited
            ? 0
            : MeasureRetainedTextMemory(maxCaptureBytes, selected, groupCount);
        var matchLease = governor.ReserveTransient(AwkRuntimeMemory.EstimateRegexMatchBytes(
            groupCount,
            retainedTextMemoryBytes));

        try
        {
            var boundaryCount = coordinatePolicy == AwkRegexCoordinatePolicy.Byte
                ? checked(groupCount * 2)
                : 0;
            long[]? rentedBoundaries = null;
            scoped Span<long> byteBoundaries = [];
            if (boundaryCount > MaxStackBoundaryCount)
            {
                rentedBoundaries = ArrayPool<long>.Shared.Rent(boundaryCount);
                byteBoundaries = rentedBoundaries.AsSpan(0, boundaryCount);
            }
            else if (boundaryCount > 0)
            {
                byteBoundaries = stackalloc long[boundaryCount];
            }

            try
            {
                if (!byteBoundaries.IsEmpty)
                    ResolveByteBoundaries(selected, input, byteIndex, groupCount, byteBoundaries);

                var groups = groupCount == 0 ? [] : new AwkRegexGroup[groupCount];
                counters.RecordRegexGroupsMaterialized(groups.Length);
                for (var index = 0; index < groups.Length; index++)
                {
                    var group = selected[index];
                    var groupIndex = coordinatePolicy == AwkRegexCoordinatePolicy.Byte
                        ? group.Success ? Payload(byteBoundaries[index * 2]) : 0
                        : group.Index;
                    var groupLength = coordinatePolicy == AwkRegexCoordinatePolicy.Byte
                        ? group.Success
                            ? Payload(byteBoundaries[(index * 2) + 1]) - groupIndex
                            : 0
                        : group.Length;
                    groups[index] = new AwkRegexGroup(
                        group.Success,
                        groupIndex,
                        groupLength,
                        AwkText.FromUtf16(group.ValueSpan, counters, AwkMemoryGovernor.Disabled));
                }

                var value = groups.Length == 0 ? AwkText.Empty : groups[0].Value;
                return new AwkLeasedRegexMatch(
                    new AwkRegexMatch(
                        true,
                        SelectCoordinate(coordinatePolicy, byteIndex, whole.Index),
                        SelectCoordinate(coordinatePolicy, byteLength, whole.Length),
                        byteIndex,
                        byteLength,
                        value,
                        groups),
                    matchLease,
                    retainedTextMemoryBytes);
            }
            finally
            {
                if (rentedBoundaries is not null)
                    ArrayPool<long>.Shared.Return(rentedBoundaries);
            }
        }
        catch
        {
            matchLease.Dispose();
            throw;
        }

        static int SelectCoordinate(
            AwkRegexCoordinatePolicy coordinatePolicy,
            int byteValue,
            int utf16Value) =>
            coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? byteValue : utf16Value;
    }

    public static AwkLeasedRegexMatch FromUtf8Match(
        Utf8MatchContext match,
        AwkRegexCoordinatePolicy coordinatePolicy,
        AwkRegexCapturePlan capturePlan,
        long maxCaptureBytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        static long MeasureRetainedTextMemory(
            long maxCaptureBytes,
            Utf8MatchContext match,
            int groupCount)
        {
            var captureBytes = 0L;
            var retainedTextBytes = 0L;
            for (var index = 0; index < groupCount; index++)
            {
                var group = match.GetGroup(index);
                if (group.Success)
                {
                    captureBytes = AwkRuntimeMemory.CheckedAdd(captureBytes, group.LengthInBytes);
                    retainedTextBytes = AwkRuntimeMemory.CheckedAdd(
                        retainedTextBytes,
                        AwkRuntimeMemory.EstimateTextBytes(group.LengthInBytes));
                }
                if (maxCaptureBytes != AwkRuntimeLimits.Unlimited && captureBytes > maxCaptureBytes)
                    throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            }

            return retainedTextBytes;
        }

        if (!match.Success)
            return AwkLeasedRegexMatch.NoMatch;

        var groupCount = capturePlan.SelectGroupCount(match.GroupCount);
        AwkRegexScalarBoundary.ThrowIfUnaligned(match.IsByteAligned);
        for (var index = 0; index < groupCount; index++)
        {
            var group = match.GetGroup(index);
            if (group.Success)
                AwkRegexScalarBoundary.ThrowIfUnaligned(group.IsByteAligned);
        }

        var retainedTextMemoryBytes = !governor.IsEnabled && maxCaptureBytes == AwkRuntimeLimits.Unlimited
            ? 0
            : MeasureRetainedTextMemory(maxCaptureBytes, match, groupCount);
        var matchLease = governor.ReserveTransient(AwkRuntimeMemory.EstimateRegexMatchBytes(
            groupCount,
            retainedTextMemoryBytes));
        try
        {
            var groups = groupCount == 0 ? [] : new AwkRegexGroup[groupCount];
            counters.RecordRegexGroupsMaterialized(groups.Length);
            for (var index = 0; index < groups.Length; index++)
            {
                var group = match.GetGroup(index);
                var groupValue = group.TryGetValueBytes(out var groupBytes)
                    ? AwkText.CopyTrustedBytes(groupBytes, counters, AwkMemoryGovernor.Disabled)
                    : AwkText.FromString(group.GetValueString(), counters, AwkMemoryGovernor.Disabled);
                groups[index] = new AwkRegexGroup(
                    group.Success,
                    SelectCoordinate(coordinatePolicy, group.IndexInBytes, group.IndexInUtf16),
                    SelectCoordinate(coordinatePolicy, group.LengthInBytes, group.LengthInUtf16),
                    groupValue);
            }

            var value = groups.Length == 0 ? AwkText.Empty : groups[0].Value;
            return new AwkLeasedRegexMatch(
                new AwkRegexMatch(
                    true,
                    SelectCoordinate(coordinatePolicy, match.IndexInBytes, match.IndexInUtf16),
                    SelectCoordinate(coordinatePolicy, match.LengthInBytes, match.LengthInUtf16),
                    match.IndexInBytes,
                    match.LengthInBytes,
                    value,
                    groups),
                matchLease,
                retainedTextMemoryBytes);
        }
        catch
        {
            matchLease.Dispose();
            throw;
        }

        static int SelectCoordinate(
            AwkRegexCoordinatePolicy coordinatePolicy,
            int byteValue,
            int utf16Value) =>
            coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? byteValue : utf16Value;

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

    public static AwkRegexSearchOffset AfterMatch(AwkText input, AwkRegexMatchSpan match)
    {
        if (match.ByteLength > 0 || match.ByteIndex >= input.ByteLength)
        {
            return new AwkRegexSearchOffset(
                match.ByteIndex + match.ByteLength,
                match.Utf16Index + match.Utf16Length);
        }

        var status = Rune.DecodeFromUtf8(input.AsSpan()[match.ByteIndex..], out var rune, out var bytesConsumed);
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
    internal bool RequestsSubexpressions => _maximumGroupCount > 1;

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
