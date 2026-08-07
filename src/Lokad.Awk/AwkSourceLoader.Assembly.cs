using System.Collections.Generic;
using System.Text;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    private static void AppendSourceSegment(
        StringBuilder builder,
        List<AwkSourceSegment> segments,
        AwkSourceName sourceName,
        AwkSourceSegmentKind sourceKind,
        string sourceText,
        int sourceStart,
        int sourceLength,
        int lineOffset,
        int columnOffset,
        AwkLineContinuationMap lineContinuations)
    {
        if (sourceLength == 0)
            return;

        var span = new AwkTextSpan(builder.Length, sourceLength);
        builder.Append(sourceText, sourceStart, sourceLength);
        AddSegment(segments, new AwkSourceSegment(
            sourceName,
            sourceKind,
            span,
            new AwkLineOffset(lineOffset),
            new AwkColumnOffset(columnOffset),
            lineContinuations));
    }

    private static void AppendExpandedSource(
        StringBuilder builder,
        List<AwkSourceSegment> segments,
        AwkExpandedSource source)
    {
        if (source.Text.Length == 0)
            return;

        var offset = builder.Length;
        builder.Append(source.Text);
        for (var index = 0; index < source.Segments.Count; index++)
        {
            var segment = source.Segments[index];
            AddSegment(
                segments,
                new AwkSourceSegment(
                    segment.SourceName,
                    segment.Kind,
                    new AwkTextSpan(segment.Span.Start + offset, segment.Span.Length),
                    segment.SourceLineOffset,
                    segment.SourceColumnOffset,
                    segment.LineContinuations));
        }
    }

    private static void AppendSyntheticNewLine(
        StringBuilder builder,
        List<AwkSourceSegment> segments,
        AwkSourceSegment? sourceSegment)
    {
        var start = builder.Length;
        builder.Append('\n');
        if (sourceSegment is { } segment)
        {
            AddSegment(
                segments,
                new AwkSourceSegment(
                    segment.SourceName,
                    segment.Kind,
                    new AwkTextSpan(start, 1),
                    segment.SourceLineOffset,
                    segment.SourceColumnOffset,
                    AwkLineContinuationMap.Empty));
        }
    }

    private static void AddSegment(List<AwkSourceSegment> segments, AwkSourceSegment segment)
    {
        if (segment.Span.Length == 0)
            return;

        if (segments.Count > 0)
        {
            var previous = segments[^1];
            if (previous.SourceName == segment.SourceName &&
                previous.Kind == segment.Kind &&
                previous.Span.End == segment.Span.Start)
            {
                segments[^1] = new AwkSourceSegment(
                    previous.SourceName,
                    previous.Kind,
                    new AwkTextSpan(previous.Span.Start, previous.Span.Length + segment.Span.Length),
                    previous.SourceLineOffset,
                    previous.SourceColumnOffset,
                    previous.LineContinuations.Append(segment.LineContinuations, previous.Span.Length));
                return;
            }
        }

        segments.Add(segment);
    }

}
