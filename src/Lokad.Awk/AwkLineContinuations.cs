using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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

    public static bool TrySkipAfterBackslash(
        ReadOnlySpan<char> text,
        ref int escapedIndex,
        int endExclusive)
    {
        if (!TryReadAt(text, escapedIndex - 1, endExclusive, out var continuation))
            return false;

        escapedIndex += continuation.Length - 2;
        return true;
    }

    private static bool TryReadAt(
        ReadOnlySpan<char> text,
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
    private readonly string? _text;
    private readonly AwkLineContinuationMap? _lineContinuations;

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
    private readonly AwkLineContinuationMapNode _node;
    private readonly int _count;
    private AwkLineContinuationPoint[]? _materializedPoints;

    public AwkLineContinuationMap(IReadOnlyList<AwkLineContinuationPoint> points)
    {
        _node = new AwkLineContinuationMapNode.Leaf(points);
        _count = points.Count;
    }

    private AwkLineContinuationMap(
        AwkLineContinuationMap first,
        AwkLineContinuationMap second,
        int secondOffset)
    {
        _node = new AwkLineContinuationMapNode.Concat(first, second, secondOffset);
        _count = checked(first.Count + second.Count);
    }

    public static AwkLineContinuationMap Empty { get; } = new([]);

    public int Count => _count;

    public bool IsEmpty => Count == 0;

    private IReadOnlyList<AwkLineContinuationPoint> Points
    {
        get
        {
            if (_node is AwkLineContinuationMapNode.Leaf leaf)
                return leaf.Points;

            var materialized = Volatile.Read(ref _materializedPoints);
            if (materialized != null)
                return materialized;

            materialized = MaterializePoints();
            return Interlocked.CompareExchange(ref _materializedPoints, materialized, null) ?? materialized;

            AwkLineContinuationPoint[] MaterializePoints()
            {
                var points = new AwkLineContinuationPoint[Count];
                var writeIndex = 0;
                var pending = new Stack<(AwkLineContinuationMap Map, int Offset)>();
                pending.Push((this, 0));
                while (pending.Count > 0)
                {
                    var (map, offset) = pending.Pop();
                    if (map._node is AwkLineContinuationMapNode.Concat concat)
                    {
                        pending.Push((concat.Second, checked(offset + concat.SecondOffset)));
                        pending.Push((concat.First, offset));
                        continue;
                    }

                    var leaf = (AwkLineContinuationMapNode.Leaf)map._node;
                    for (var index = 0; index < leaf.Points.Count; index++)
                    {
                        points[writeIndex++] = new AwkLineContinuationPoint(
                            checked(offset + leaf.Points[index].NormalizedOffset));
                    }
                }

                return points;
            }
        }
    }

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

        return new AwkLineContinuationMap(this, next, nextOffset);
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

    private abstract record AwkLineContinuationMapNode
    {
        internal sealed record Leaf(IReadOnlyList<AwkLineContinuationPoint> Points) : AwkLineContinuationMapNode;

        internal sealed record Concat(
            AwkLineContinuationMap First,
            AwkLineContinuationMap Second,
            int SecondOffset) : AwkLineContinuationMapNode;
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
