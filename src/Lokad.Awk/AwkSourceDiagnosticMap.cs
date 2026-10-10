using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSourceDiagnosticMap
{
    public static IReadOnlyList<AwkDiagnostic> MapDiagnostics(
        AwkSourceUnit source,
        IReadOnlyList<AwkDiagnostic> diagnostics,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        bool TryFindSegment(
            IReadOnlyList<AwkSourceSegment> segments,
            int position,
            out AwkSourceSegment segment)
        {
            var low = 0;
            var high = segments.Count;
            while (low < high)
            {
                checkpoint?.Invoke();
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

        AwkSourceLocation CreateLocation(
            AwkSourceUnit source,
            AwkSourceSegment segment,
            int position,
            SourceLineIndex lineIndex)
        {
            var relativeOffset = Math.Clamp(position - segment.Span.Start, 0, segment.Span.Length);
            var end = Math.Min(segment.Span.Start + relativeOffset, source.Text.Length);
            var sourceLines = lineIndex.GetPrefix(segment.Span.Start, end);
            var continuationCount = segment.LineContinuations.CountAtOrBefore(relativeOffset, checkpoint);

            var lastLineStart = sourceLines.LastStart;
            if (continuationCount > 0)
            {
                var lastContinuation = segment.Span.Start +
                    segment.LineContinuations.GetPoint(continuationCount - 1, checkpoint).NormalizedOffset;
                if (lastContinuation > lastLineStart)
                    lastLineStart = lastContinuation;
            }

            var line = segment.SourceLineOffset.Value + 1 + sourceLines.Count + continuationCount;
            var column = lastLineStart >= 0
                ? 1 + end - lastLineStart
                : segment.SourceColumnOffset.Value + 1 + relativeOffset;
            return new AwkSourceLocation(
                segment.SourceName,
                new AwkLineNumber(line),
                new AwkColumnNumber(column));
        }

        if (diagnostics.Count == 0)
            return diagnostics;

        SourceLineIndex? lineIndex = null;
        AwkDiagnostic[]? mapped = null;
        for (var index = 0; index < diagnostics.Count; index++)
        {
            checkpoint?.Invoke();
            var diagnostic = diagnostics[index];
            if (diagnostic.SourceLocation.HasValue ||
                !TryFindSegment(source.Segments, diagnostic.Span.Start, out var segment) ||
                (segment.Kind == AwkSourceSegmentKind.Inline &&
                    source.Segments.Count == 1 &&
                    source.SourceName == new AwkSourceName("<inline:1>")))
            {
                continue;
            }

            if (mapped is null)
            {
                mapped = new AwkDiagnostic[diagnostics.Count];
                for (var copyIndex = 0; copyIndex < diagnostics.Count; copyIndex++)
                {
                    checkpoint?.Invoke();
                    mapped[copyIndex] = diagnostics[copyIndex];
                }
            }

            lineIndex ??= new SourceLineIndex(source.Text, checkpoint);
            mapped[index] = diagnostic.WithSourceLocation(
                CreateLocation(source, segment, diagnostic.Span.Start, lineIndex));
        }

        checkpoint?.Invoke();
        return mapped ?? diagnostics;
    }

    private sealed class SourceLineIndex
    {
        private readonly string _source;
        private readonly List<int> _starts;
        private readonly Action? _checkpoint;

        public SourceLineIndex(string source, Action? checkpoint)
        {
            _source = source;
            _checkpoint = checkpoint;
            _starts = [0];
            long nextCheckpoint = 0;
            for (var index = 0; index < source.Length; index++)
            {
                if (index >= nextCheckpoint)
                {
                    _checkpoint?.Invoke();
                    nextCheckpoint = (long)index + 1024;
                }
                if (source[index] == '\r')
                {
                    if (index + 1 < source.Length && source[index + 1] == '\n')
                        index++;

                    _starts.Add(index + 1);
                }
                else if (source[index] == '\n')
                {
                    _starts.Add(index + 1);
                }
            }
        }

        public SourceLinePrefix GetPrefix(int segmentStart, int end)
        {
            _checkpoint?.Invoke();
            var first = FindFirstAfter(segmentStart);
            var afterLast = FindFirstAfter(end);
            var count = afterLast - first;
            var lastStart = count == 0 ? -1 : _starts[afterLast - 1];

            // A position between CR and LF belongs to the following logical line;
            // count that boundary even though its combined line start follows LF.
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
                var high = _starts.Count;
                while (low < high)
                {
                    _checkpoint?.Invoke();
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
