using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lokad.Awk;

internal static class AwkLineContinuations
{
    public static AwkLineContinuationNormalization Normalize(string text, int maxTokens, CancellationToken cancellationToken,
        AwkFrontendMemory? memory = null)
        => NormalizeWithCheckpoint(text, maxTokens,
            cancellationToken.CanBeCanceled ? cancellationToken.ThrowIfCancellationRequested : null, memory);

    public static AwkLineContinuationNormalization NormalizeWithCheckpoint(string text, int maxTokens, Action? checkpoint,
        AwkFrontendMemory? memory = null)
    {
        checkpoint?.Invoke();
        var firstContinuation = 0;
        while (firstContinuation < text.Length &&
            !TryReadAt(text, firstContinuation, text.Length, out _))
        {
            if ((firstContinuation & 1023) == 0)
                checkpoint?.Invoke();
            firstContinuation++;
        }

        if (firstContinuation == text.Length)
            return new AwkLineContinuationNormalization(text, AwkLineContinuationMap.Empty);

        var frontend = memory ?? AwkFrontendMemory.Disabled;
        frontend.AdmitTextCopy(text.Length);
        var builder = new StringBuilder(text.Length);
        var continuations = new List<AwkLineContinuationPoint>();
        var tokens = AwkLexer.Tokenize(text, maxTokens, checkpoint, memory: frontend);
        var tokenIndex = 0;
        var segmentStart = 0;
        var index = firstContinuation;
        long nextCheckpoint = index;
        while (index < text.Length)
        {
            if (index >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)index + 1024;
            }
            if (TryReadAt(text, index, text.Length, out var continuation))
            {
                AwkParsingSupport.AppendText(builder, text.AsSpan(segmentStart, index - segmentStart), checkpoint);
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Span.End <= index)
                {
                    checkpoint?.Invoke();
                    tokenIndex++;
                }
                var token = tokenIndex < tokens.Count ? tokens[tokenIndex] : default;
                var insideToken = token.Span.Start <= index && index < token.Span.End;
                if (insideToken && token.Kind == AwkTokenKind.BadToken)
                {
                    // Preserve malformed literal ingress for the lexical diagnostic;
                    // normalization must not turn an unescaped newline into valid text.
                    builder.Append(text, index, continuation.Length);
                }
                else if (insideToken && token.Kind == AwkTokenKind.Comment)
                {
                    // A backslash does not extend an awk comment onto the next line.
                    builder.Append(text, index + 1, continuation.Length - 1);
                }
                else
                {
                    var insideLiteral = insideToken && token.Kind is
                        AwkTokenKind.String or AwkTokenKind.InternationalizedString or
                        AwkTokenKind.Regex or AwkTokenKind.StrongRegex;
                    if (!insideLiteral && builder.Length > 0 && !char.IsWhiteSpace(builder[^1]) &&
                        index + continuation.Length < text.Length &&
                        !char.IsWhiteSpace(text[index + continuation.Length]))
                        builder.Append(' ');
                    continuations.Add(new AwkLineContinuationPoint(builder.Length));
                }
                index += continuation.Length;
                segmentStart = index;
                continue;
            }

            index++;
        }

        AwkParsingSupport.AppendText(builder, text.AsSpan(segmentStart), checkpoint);
        var normalized = builder.ToString();
        checkpoint?.Invoke();
        return new AwkLineContinuationNormalization(
            normalized,
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

    private IReadOnlyList<AwkLineContinuationPoint> GetPoints(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (_node is AwkLineContinuationMapNode.Leaf leaf)
            return leaf.Points;

        var materialized = Volatile.Read(ref _materializedPoints);
        if (materialized != null)
            return materialized;

        materialized = MaterializePoints();
        checkpoint?.Invoke();
        return Interlocked.CompareExchange(ref _materializedPoints, materialized, null) ?? materialized;

        AwkLineContinuationPoint[] MaterializePoints()
        {
            var points = new AwkLineContinuationPoint[Count];
            var writeIndex = 0;
            var pending = new Stack<PendingMap>();
            pending.Push(new PendingMap(this, 0));
            while (pending.Count > 0)
            {
                checkpoint?.Invoke();
                var (map, offset) = pending.Pop();
                if (map._node is AwkLineContinuationMapNode.Concat concat)
                {
                    pending.Push(new PendingMap(concat.Second, checked(offset + concat.SecondOffset)));
                    pending.Push(new PendingMap(concat.First, offset));
                    continue;
                }

                var leaf = (AwkLineContinuationMapNode.Leaf)map._node;
                for (var index = 0; index < leaf.Points.Count; index++)
                {
                    checkpoint?.Invoke();
                    points[writeIndex++] = new AwkLineContinuationPoint(
                        checked(offset + leaf.Points[index].NormalizedOffset));
                }
            }

            return points;
        }
    }

    private readonly struct PendingMap
    {
        private readonly AwkLineContinuationMap? _map;

        public PendingMap(AwkLineContinuationMap map, int offset)
        {
            _map = map;
            Offset = offset;
        }

        public AwkLineContinuationMap Map => _map ?? Empty;

        public int Offset { get; }

        public void Deconstruct(out AwkLineContinuationMap map, out int offset)
        {
            map = Map;
            offset = Offset;
        }
    }

    public AwkLineContinuationPoint this[int index] => GetPoint(index);

    public AwkLineContinuationPoint GetPoint(int index, Action? checkpoint = null) => GetPoints(checkpoint)[index];

    public AwkLineContinuationMap Slice(int start, int length, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var indexed = GetPoints(checkpoint);
        if (IsEmpty)
            return Empty;

        var end = checked(start + length);
        var first = FindFirstAtOrAfter(start);
        var afterLast = FindFirstAfter(end, checkpoint);
        if (first == afterLast)
            return Empty;

        var points = new List<AwkLineContinuationPoint>(afterLast - first);
        for (var index = first; index < afterLast; index++)
        {
            checkpoint?.Invoke();
            points.Add(new AwkLineContinuationPoint(indexed[index].NormalizedOffset - start));
        }

        return new AwkLineContinuationMap(points);

        int FindFirstAtOrAfter(int offset)
        {
            var low = 0;
            var high = indexed.Count;
            while (low < high)
            {
                checkpoint?.Invoke();
                var middle = low + ((high - low) / 2);
                if (indexed[middle].NormalizedOffset < offset)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

    }

    public int CountAtOrBefore(int normalizedOffset, Action? checkpoint = null) => FindFirstAfter(normalizedOffset, checkpoint);

    public AwkLineContinuationMap Append(AwkLineContinuationMap next, int nextOffset, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        static AwkLineContinuationMap Shift(
            AwkLineContinuationMap map,
            int offset, Action? checkpoint)
        {
            if (map.IsEmpty || offset == 0)
                return map;

            var shiftedPoints = new List<AwkLineContinuationPoint>(map.Count);
            for (var index = 0; index < map.Count; index++)
            {
                checkpoint?.Invoke();
                shiftedPoints.Add(new AwkLineContinuationPoint(
                    offset + map.GetPoint(index, checkpoint).NormalizedOffset));
            }

            return new AwkLineContinuationMap(shiftedPoints);
        }

        if (IsEmpty)
            return Shift(next, nextOffset, checkpoint);

        if (next.IsEmpty)
            return this;

        return new AwkLineContinuationMap(this, next, nextOffset);
    }

    private int FindFirstAfter(int offset, Action? checkpoint = null)
    {
        var indexed = GetPoints(checkpoint);
        var low = 0;
        var high = indexed.Count;
        while (low < high)
        {
            checkpoint?.Invoke();
            var middle = low + ((high - low) / 2);
            if (indexed[middle].NormalizedOffset <= offset)
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
