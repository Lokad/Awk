using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lokad.Utf8Regex;

namespace Lokad.Awk;

internal readonly record struct AwkRegexMatch(
    bool Success,
    int Index,
    int Length,
    int ByteIndex,
    int ByteLength,
    AwkText Value,
    IReadOnlyList<AwkRegexGroup> Groups)
{
    public static AwkRegexMatch NoMatch { get; } =
        new(false, 0, 0, 0, 0, AwkText.Empty, []);

    public static AwkRegexMatch FromMatch(
        Match match,
        string input,
        int byteIndex,
        int byteLength,
        AwkRegexCoordinatePolicy coordinatePolicy,
        long maxCaptureBytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        const int MaxStackBoundaryCount = 128;

        static long PackCoordinates(int sortCoordinate, int payload) =>
            ((long)(uint)sortCoordinate << 32) | (uint)payload;

        static int SortCoordinate(long packed) =>
            unchecked((int)(packed >> 32));

        static int Payload(long packed) =>
            unchecked((int)packed);

        static void ResolveByteBoundaries(
            Match match,
            string input,
            int matchByteIndex,
            Span<long> boundaries)
        {
            var minimumUtf16Index = match.Index;
            for (var groupIndex = 0; groupIndex < match.Groups.Count; groupIndex++)
            {
                var group = match.Groups[groupIndex];
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

        if (!match.Success)
            return NoMatch;

        ThrowIfCaptureBytesExceeded(maxCaptureBytes, match.Groups);

        var boundaryCount = coordinatePolicy == AwkRegexCoordinatePolicy.Byte
            ? checked(match.Groups.Count * 2)
            : 0;
        long[]? rentedBoundaries = null;
        Span<long> stackBoundaries = stackalloc long[MaxStackBoundaryCount];
        scoped Span<long> byteBoundaries;
        if (boundaryCount == 0)
        {
            byteBoundaries = [];
        }
        else if (boundaryCount <= MaxStackBoundaryCount)
        {
            byteBoundaries = stackBoundaries[..boundaryCount];
        }
        else
        {
            rentedBoundaries = ArrayPool<long>.Shared.Rent(boundaryCount);
            byteBoundaries = rentedBoundaries.AsSpan(0, boundaryCount);
        }

        try
        {
            if (!byteBoundaries.IsEmpty)
                ResolveByteBoundaries(match, input, byteIndex, byteBoundaries);

            var groups = new AwkRegexGroup[match.Groups.Count];
            counters.RecordRegexGroupsMaterialized(groups.Length);
            for (var index = 0; index < groups.Length; index++)
            {
                var group = match.Groups[index];
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
                    AwkText.FromUtf16(group.ValueSpan, counters, governor));
            }

            var value = groups.Length == 0
                ? AwkText.FromUtf16(match.ValueSpan, counters, governor)
                : groups[0].Value;
            return new AwkRegexMatch(
                true,
                SelectCoordinate(coordinatePolicy, byteIndex, match.Index),
                SelectCoordinate(coordinatePolicy, byteLength, match.Length),
                byteIndex,
                byteLength,
                value,
                groups);
        }
        finally
        {
            if (rentedBoundaries is not null)
                ArrayPool<long>.Shared.Return(rentedBoundaries);
        }
    }

    public static AwkRegexMatch FromUtf8Match(
        Utf8MatchContext match,
        AwkRegexCoordinatePolicy coordinatePolicy,
        long maxCaptureBytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (!match.Success)
            return NoMatch;

        ThrowIfCaptureBytesExceeded(maxCaptureBytes, match);

        var groups = new AwkRegexGroup[match.GroupCount];
        counters.RecordRegexGroupsMaterialized(groups.Length);
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = FromUtf8Group(match.GetGroup(index), coordinatePolicy, counters, governor);
        }

        var value = groups.Length == 0
            ? CreateText(match, counters, governor)
            : groups[0].Value;
        return new AwkRegexMatch(
            true,
            SelectCoordinate(coordinatePolicy, match.IndexInBytes, match.IndexInUtf16),
            SelectCoordinate(coordinatePolicy, match.LengthInBytes, match.LengthInUtf16),
            match.IndexInBytes,
            match.LengthInBytes,
            value,
            groups);
    }

    private static AwkRegexGroup FromUtf8Group(
        Utf8GroupContext group,
        AwkRegexCoordinatePolicy coordinatePolicy,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        new(
            group.Success,
            SelectCoordinate(coordinatePolicy, group.IndexInBytes, group.IndexInUtf16),
            SelectCoordinate(coordinatePolicy, group.LengthInBytes, group.LengthInUtf16),
            CreateText(group, counters, governor));

    private static AwkText CreateText(Utf8MatchContext match, AwkRuntimeCounters counters, AwkMemoryGovernor governor) =>
        match.TryGetValueBytes(out var bytes)
            ? AwkText.CopyTrustedBytes(bytes, counters, governor)
            : AwkText.FromString(match.GetValueString(), counters, governor);

    private static AwkText CreateText(Utf8GroupContext group, AwkRuntimeCounters counters, AwkMemoryGovernor governor) =>
        group.TryGetValueBytes(out var bytes)
            ? AwkText.CopyTrustedBytes(bytes, counters, governor)
            : AwkText.FromString(group.GetValueString(), counters, governor);

    private static int SelectCoordinate(
        AwkRegexCoordinatePolicy coordinatePolicy,
        int byteValue,
        int utf16Value) =>
        coordinatePolicy == AwkRegexCoordinatePolicy.Byte ? byteValue : utf16Value;

    private static void ThrowIfCaptureBytesExceeded(
        long maxCaptureBytes,
        GroupCollection groups)
    {
        if (maxCaptureBytes == AwkRuntimeLimits.Unlimited)
            return;

        var captureBytes = 0L;
        for (var index = 0; index < groups.Count; index++)
        {
            captureBytes = AwkRuntimeMemory.CheckedAdd(
                captureBytes,
                Encoding.UTF8.GetByteCount(groups[index].ValueSpan));
            if (captureBytes > maxCaptureBytes)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        }
    }

    private static void ThrowIfCaptureBytesExceeded(
        long maxCaptureBytes,
        Utf8MatchContext match)
    {
        if (maxCaptureBytes == AwkRuntimeLimits.Unlimited)
            return;

        var captureBytes = 0L;
        for (var index = 0; index < match.GroupCount; index++)
        {
            var group = match.GetGroup(index);
            if (group.Success)
                captureBytes = AwkRuntimeMemory.CheckedAdd(captureBytes, group.LengthInBytes);
            if (captureBytes > maxCaptureBytes)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        }
    }

}

internal readonly record struct AwkRegexGroup(
    bool Success,
    int Index,
    int Length,
    AwkText Value);

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
