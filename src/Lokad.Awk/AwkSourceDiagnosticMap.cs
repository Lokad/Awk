using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSourceDiagnosticMap
{
    public static IReadOnlyList<AwkDiagnostic> MapDiagnostics(
        AwkSourceUnit source,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        static AwkDiagnostic MapDiagnostic(AwkSourceUnit source, AwkDiagnostic diagnostic)
        {
            if (diagnostic.SourceLocation.HasValue ||
                !TryFindSegment(source.Segments, diagnostic.Span.Start, out var segment) ||
                !ShouldAnnotateSource(source, segment))
            {
                return diagnostic;
            }

            return diagnostic.WithSourceLocation(CreateLocation(source, segment, diagnostic.Span.Start));
        }

        if (diagnostics.Count == 0)
            return diagnostics;

        var mapped = new AwkDiagnostic[diagnostics.Count];
        for (var index = 0; index < diagnostics.Count; index++)
        {
            mapped[index] = MapDiagnostic(source, diagnostics[index]);
        }

        return mapped;
    }

    private static bool ShouldAnnotateSource(AwkSourceUnit source, AwkSourceSegment segment) =>
        segment.Kind != AwkSourceSegmentKind.Inline ||
        source.Segments.Count > 1 ||
        source.Name != "<inline:1>";

    private static bool TryFindSegment(
        IReadOnlyList<AwkSourceSegment> segments,
        int position,
        out AwkSourceSegment segment)
    {
        for (var index = 0; index < segments.Count; index++)
        {
            var candidate = segments[index];
            if (position >= candidate.Span.Start && position < candidate.Span.End)
            {
                segment = candidate;
                return true;
            }
        }

        for (var index = segments.Count - 1; index >= 0; index--)
        {
            var candidate = segments[index];
            if (position == candidate.Span.End)
            {
                segment = candidate;
                return true;
            }
        }

        segment = default;
        return false;
    }

    private static AwkSourceLocation CreateLocation(
        AwkSourceUnit source,
        AwkSourceSegment segment,
        int position)
    {
        var relativeOffset = position - segment.Span.Start;
        if (relativeOffset < 0)
            relativeOffset = 0;

        if (relativeOffset > segment.Span.Length)
            relativeOffset = segment.Span.Length;

        var line = segment.LineOffset + 1;
        var column = segment.ColumnOffset + 1;
        var end = segment.Span.Start + relativeOffset;
        if (end > source.Text.Length)
            end = source.Text.Length;

        var lineContinuations = segment.LineContinuations;
        var lineContinuationIndex = 0;
        for (var index = segment.Span.Start; index < end; index++)
        {
            ApplyLineContinuations(
                lineContinuations,
                ref lineContinuationIndex,
                index - segment.Span.Start,
                ref line,
                ref column);

            if (source.Text[index] == '\r')
            {
                line++;
                column = 1;
                if (index + 1 < end && source.Text[index + 1] == '\n')
                    index++;

                continue;
            }

            if (source.Text[index] == '\n')
            {
                line++;
                column = 1;
                continue;
            }

            column++;
        }

        ApplyLineContinuations(
            lineContinuations,
            ref lineContinuationIndex,
            relativeOffset,
            ref line,
            ref column);

        return new AwkSourceLocation(segment.Name, line, column);
    }

    private static void ApplyLineContinuations(
        AwkLineContinuationMap lineContinuations,
        ref int lineContinuationIndex,
        int relativeOffset,
        ref int line,
        ref int column)
    {
        while (lineContinuationIndex < lineContinuations.Count &&
            lineContinuations[lineContinuationIndex].NormalizedOffset == relativeOffset)
        {
            line++;
            column = 1;
            lineContinuationIndex++;
        }
    }
}
