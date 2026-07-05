using System;
using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal static class AwkLineContinuations
{
    public static AwkLineContinuationNormalization Normalize(string text)
    {
        if (!ContainsLineContinuation(text))
            return new AwkLineContinuationNormalization(text, AwkLineContinuationMap.Empty);

        var builder = new StringBuilder(text.Length);
        var continuations = new List<AwkLineContinuationPoint>();
        var segmentStart = 0;
        var index = 0;
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

    private static bool ContainsLineContinuation(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (TryReadAt(text, index, text.Length, out _))
                return true;
        }

        return false;
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

    public int CountInRange(int start, int length)
    {
        if (IsEmpty)
            return 0;

        var end = start + length;
        var count = 0;
        for (var index = 0; index < Points.Count; index++)
        {
            var offset = Points[index].NormalizedOffset;
            if (offset >= start && offset <= end)
                count++;
        }

        return count;
    }

    public AwkLineContinuationMap Slice(int start, int length)
    {
        if (IsEmpty)
            return Empty;

        var end = start + length;
        var points = new List<AwkLineContinuationPoint>();
        for (var index = 0; index < Points.Count; index++)
        {
            var offset = Points[index].NormalizedOffset;
            if (offset >= start && offset <= end)
                points.Add(new AwkLineContinuationPoint(offset - start));
        }

        return points.Count == 0 ? Empty : new AwkLineContinuationMap(points);
    }

    public AwkLineContinuationMap Append(AwkLineContinuationMap next, int nextOffset)
    {
        if (IsEmpty)
            return next.Shift(nextOffset);

        if (next.IsEmpty)
            return this;

        var points = new List<AwkLineContinuationPoint>(Count + next.Count);
        for (var index = 0; index < Count; index++)
            points.Add(this[index]);

        for (var index = 0; index < next.Count; index++)
            points.Add(new AwkLineContinuationPoint(nextOffset + next[index].NormalizedOffset));

        return new AwkLineContinuationMap(points);
    }

    private AwkLineContinuationMap Shift(int offset)
    {
        if (IsEmpty || offset == 0)
            return this;

        var points = new List<AwkLineContinuationPoint>(Count);
        for (var index = 0; index < Count; index++)
            points.Add(new AwkLineContinuationPoint(offset + this[index].NormalizedOffset));

        return new AwkLineContinuationMap(points);
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
