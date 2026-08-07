using System;
using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal static class AwkLineContinuations
{
    public static AwkLineContinuationNormalization Normalize(string text)
    {
        var firstContinuation = 0;
        while (firstContinuation < text.Length &&
            !TryReadAt(text, firstContinuation, text.Length, out _))
        {
            firstContinuation++;
        }

        if (firstContinuation == text.Length)
            return new AwkLineContinuationNormalization(text, AwkLineContinuationMap.Empty);

        var builder = new StringBuilder(text.Length);
        var continuations = new List<AwkLineContinuationPoint>();
        var segmentStart = 0;
        var index = firstContinuation;
        while (index < text.Length)
        {
            if (TryReadAt(text, index, text.Length, out var continuation))
            {
                builder.Append(text, segmentStart, index - segmentStart);
                continuations.Add(new AwkLineContinuationPoint(builder.Length));
                index += continuation.Length;
                segmentStart = index;
                continue;
            }

            index++;
        }

        builder.Append(text, segmentStart, text.Length - segmentStart);
        return new AwkLineContinuationNormalization(
            builder.ToString(),
            new AwkLineContinuationMap(continuations));
    }

    public static bool TrySkipAfterBackslash(string text, ref int escapedIndex, int endExclusive)
    {
        if (!TryReadAt(text, escapedIndex - 1, endExclusive, out var continuation))
            return false;

        escapedIndex += continuation.Length - 2;
        return true;
    }

    private static bool TryReadAt(
        string text,
        int backslashIndex,
        int endExclusive,
        out AwkLineContinuation continuation)
    {
        continuation = default;
        if (backslashIndex < 0 ||
            backslashIndex + 1 >= endExclusive ||
            text[backslashIndex] != '\\')
        {
            return false;
        }

        var next = text[backslashIndex + 1];
        if (next == '\n')
        {
            continuation = new AwkLineContinuation(2);
            return true;
        }

        if (next != '\r')
            return false;

        var length = backslashIndex + 2 < endExclusive && text[backslashIndex + 2] == '\n'
            ? 3
            : 2;
        continuation = new AwkLineContinuation(length);
        return true;
    }
}

internal readonly record struct AwkLineContinuationNormalization
{
    private readonly string _text;
    private readonly AwkLineContinuationMap _lineContinuations;

    public AwkLineContinuationNormalization(string text, AwkLineContinuationMap lineContinuations)
    {
        _text = text;
        _lineContinuations = lineContinuations;
    }

    public string Text => _text ?? string.Empty;

    public AwkLineContinuationMap LineContinuations => _lineContinuations ?? AwkLineContinuationMap.Empty;
}

internal sealed class AwkLineContinuationMap
{
    private readonly IReadOnlyList<AwkLineContinuationPoint> _points;

    public AwkLineContinuationMap(IReadOnlyList<AwkLineContinuationPoint> points)
    {
        _points = points;
    }

    public static AwkLineContinuationMap Empty { get; } = new([]);

    public int Count => Points.Count;

    public bool IsEmpty => Count == 0;

    private IReadOnlyList<AwkLineContinuationPoint> Points => _points ?? [];

    public AwkLineContinuationPoint this[int index] => Points[index];

    public AwkLineContinuationMap Slice(int start, int length)
    {
        if (IsEmpty)
            return Empty;

        var end = checked(start + length);
        var first = FindFirstAtOrAfter(start);
        var afterLast = FindFirstAfter(end);
        if (first == afterLast)
            return Empty;

        var points = new List<AwkLineContinuationPoint>(afterLast - first);
        for (var index = first; index < afterLast; index++)
            points.Add(new AwkLineContinuationPoint(Points[index].NormalizedOffset - start));

        return new AwkLineContinuationMap(points);

        int FindFirstAtOrAfter(int offset)
        {
            var low = 0;
            var high = Points.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (Points[middle].NormalizedOffset < offset)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

    }

    public int CountAtOrBefore(int normalizedOffset) => FindFirstAfter(normalizedOffset);

    public AwkLineContinuationMap Append(AwkLineContinuationMap next, int nextOffset)
    {
        static AwkLineContinuationMap Shift(
            AwkLineContinuationMap map,
            int offset)
        {
            if (map.IsEmpty || offset == 0)
                return map;

            var shiftedPoints = new List<AwkLineContinuationPoint>(map.Count);
            for (var index = 0; index < map.Count; index++)
            {
                shiftedPoints.Add(new AwkLineContinuationPoint(
                    offset + map[index].NormalizedOffset));
            }

            return new AwkLineContinuationMap(shiftedPoints);
        }

        if (IsEmpty)
            return Shift(next, nextOffset);

        if (next.IsEmpty)
            return this;

        var points = new List<AwkLineContinuationPoint>(Count + next.Count);
        for (var index = 0; index < Count; index++)
            points.Add(this[index]);

        for (var index = 0; index < next.Count; index++)
            points.Add(new AwkLineContinuationPoint(nextOffset + next[index].NormalizedOffset));

        return new AwkLineContinuationMap(points);
    }

    private int FindFirstAfter(int offset)
    {
        var low = 0;
        var high = Points.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (Points[middle].NormalizedOffset <= offset)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}

internal readonly record struct AwkLineContinuationPoint
{
    public AwkLineContinuationPoint(int normalizedOffset)
    {
        if (normalizedOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(normalizedOffset), "Line-continuation offsets must be zero or positive.");

        NormalizedOffset = normalizedOffset;
    }

    public int NormalizedOffset { get; }
}

internal readonly record struct AwkLineContinuation
{
    public AwkLineContinuation(int length)
    {
        if (length < 2)
            throw new ArgumentOutOfRangeException(nameof(length), "Line continuations include a backslash and newline.");

        Length = length;
    }

    public int Length { get; }
}
