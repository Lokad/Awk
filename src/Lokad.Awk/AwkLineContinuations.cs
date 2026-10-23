using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lokad.Awk;

internal static class AwkLineContinuations
{
    public static AwkLineContinuationNormalization Normalize(string text, CancellationToken cancellationToken,
        AwkFrontendMemory? memory = null, AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
        => NormalizeWithCheckpoint(text,
            cancellationToken.CanBeCanceled ? cancellationToken.ThrowIfCancellationRequested : null, memory, compatibilityMode);

    public static AwkLineContinuationNormalization NormalizeWithCheckpoint(string text, Action? checkpoint,
        AwkFrontendMemory? memory = null, AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
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
        var tokens = AwkLexer.Tokenize(text, checkpoint, compatibilityMode, frontend);
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
    private readonly IReadOnlyList<AwkLineContinuationPoint> _points;

    public AwkLineContinuationMap(IReadOnlyList<AwkLineContinuationPoint> points) => _points = points;

    public static AwkLineContinuationMap Empty { get; } = new([]);

    public int Count => _points.Count;

    public bool IsEmpty => Count == 0;

    public AwkLineContinuationPoint this[int index] => _points[index];

    public AwkLineContinuationPoint GetPoint(int index, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return _points[index];
    }

    public AwkLineContinuationMap Slice(int start, int length, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var indexed = _points;
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

    private int FindFirstAfter(int offset, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var indexed = _points;
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
