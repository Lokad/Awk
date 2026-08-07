using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSourceDiagnosticMap
{
    public static IReadOnlyList<AwkDiagnostic> MapDiagnostics(
        AwkSourceUnit source,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        static bool TryFindSegment(
            IReadOnlyList<AwkSourceSegment> segments,
            int position,
            out AwkSourceSegment segment)
        {
            var low = 0;
            var high = segments.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (segments[middle].Span.Start <= position)
                    low = middle + 1;
                else
                    high = middle;
            }

            if (low > 0)
            {
                var candidate = segments[low - 1];
                if (position <= candidate.Span.End)
                {
                    segment = candidate;
                    return true;
                }
            }

            segment = default;
            return false;
        }

        static AwkSourceLocation CreateLocation(
            AwkSourceUnit source,
            AwkSourceSegment segment,
            int position,
            SourceLineIndex lineIndex)
        {
            var relativeOffset = Math.Clamp(position - segment.Span.Start, 0, segment.Span.Length);
            var end = Math.Min(segment.Span.Start + relativeOffset, source.Text.Length);
            var sourceLines = lineIndex.GetPrefix(segment.Span.Start, end);
            var continuationCount = segment.LineContinuations.CountAtOrBefore(relativeOffset);

            var lastLineStart = sourceLines.LastStart;
            if (continuationCount > 0)
            {
                var lastContinuation = segment.Span.Start +
                    segment.LineContinuations[continuationCount - 1].NormalizedOffset;
                if (lastContinuation > lastLineStart)
                    lastLineStart = lastContinuation;
            }

            var line = segment.LineOffset + 1 + sourceLines.Count + continuationCount;
            var column = lastLineStart >= 0
                ? 1 + end - lastLineStart
                : segment.ColumnOffset + 1 + relativeOffset;
            return new AwkSourceLocation(
                segment.Name,
                new AwkLineNumber(line),
                new AwkColumnNumber(column));
        }

        static AwkDiagnostic MapDiagnostic(
            AwkSourceUnit source,
            AwkDiagnostic diagnostic,
            SourceLineIndex lineIndex)
        {
            if (diagnostic.SourceLocation.HasValue ||
                !TryFindSegment(source.Segments, diagnostic.Span.Start, out var segment) ||
                (segment.Kind == AwkSourceSegmentKind.Inline &&
                    source.Segments.Count == 1 &&
                    source.Name == "<inline:1>"))
            {
                return diagnostic;
            }

            return diagnostic.WithSourceLocation(
                CreateLocation(source, segment, diagnostic.Span.Start, lineIndex));
        }

        if (diagnostics.Count == 0)
            return diagnostics;

        var lineIndex = new SourceLineIndex(source.Text);
        var mapped = new AwkDiagnostic[diagnostics.Count];
        for (var index = 0; index < diagnostics.Count; index++)
        {
            mapped[index] = MapDiagnostic(source, diagnostics[index], lineIndex);
        }

        return mapped;
    }

    private sealed class SourceLineIndex
    {
        private readonly string _source;
        private readonly int[] _starts;

        public SourceLineIndex(string source)
        {
            _source = source;
            var count = 1;
            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] == '\r')
                {
                    if (index + 1 < source.Length && source[index + 1] == '\n')
                        index++;

                    count++;
                }
                else if (source[index] == '\n')
                {
                    count++;
                }
            }

            _starts = new int[count];
            _starts[0] = 0;
            var next = 1;
            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] == '\r')
                {
                    if (index + 1 < source.Length && source[index + 1] == '\n')
                        index++;

                    _starts[next++] = index + 1;
                }
                else if (source[index] == '\n')
                {
                    _starts[next++] = index + 1;
                }
            }
        }

        public SourceLinePrefix GetPrefix(int segmentStart, int end)
        {
            var first = FindFirstAfter(segmentStart);
            var afterLast = FindFirstAfter(end);
            var count = afterLast - first;
            var lastStart = count == 0 ? -1 : _starts[afterLast - 1];

            // A diagnostic may point between the two characters of CRLF. The old
            // cursor counted the CR at that position even though the combined
            // line start lies after the LF, so preserve that boundary behavior.
            if (end > segmentStart &&
                end < _source.Length &&
                _source[end - 1] == '\r' &&
                _source[end] == '\n')
            {
                count++;
                lastStart = end;
            }

            return new SourceLinePrefix(count, lastStart);

            int FindFirstAfter(int position)
            {
                var low = 0;
                var high = _starts.Length;
                while (low < high)
                {
                    var middle = low + ((high - low) / 2);
                    if (_starts[middle] <= position)
                        low = middle + 1;
                    else
                        high = middle;
                }

                return low;
            }
        }
    }

    private readonly record struct SourceLinePrefix(int Count, int LastStart);
}
