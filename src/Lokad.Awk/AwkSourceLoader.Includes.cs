using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    private static async Task<SourceExpansionResult> ExpandIncludesAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        string source,
        AwkSourceName sourceName,
        AwkSourceSegmentKind sourceKind,
        AwkRuntimeResourceMonitor resources,
        CancellationToken cancellationToken)
    {
        var normalization = AwkLineContinuations.Normalize(source);
        var normalizedSource = normalization.Text;
        if (normalizedSource.IndexOf('@', StringComparison.Ordinal) < 0)
        {
            if (normalizedSource.Length == 0)
                return SourceExpansionResult.FromSource(AwkExpandedSource.Empty);

            return SourceExpansionResult.FromSource(AwkExpandedSource.Create(
                normalizedSource,
                [new AwkSourceSegment(
                    sourceName,
                    sourceKind,
                    new AwkTextSpan(0, normalizedSource.Length),
                    new AwkLineOffset(0),
                    new AwkColumnOffset(0),
                    normalization.LineContinuations)]));
        }

        var builder = new StringBuilder(normalizedSource.Length);
        var segments = new List<AwkSourceSegment>();
        var index = 0;
        var sourceLine = 0;
        var sourceChunkStart = 0;
        var sourceChunkLine = 0;
        while (index < normalizedSource.Length)
        {
            var lineStart = index;
            while (index < normalizedSource.Length && normalizedSource[index] is not '\r' and not '\n')
                index++;

            var lineEnd = index;
            if (index < normalizedSource.Length)
            {
                index++;
                if (normalizedSource[index - 1] == '\r' && index < normalizedSource.Length && normalizedSource[index] == '\n')
                    index++;
            }

            var lineLength = lineEnd - lineStart;
            var sourceLineAdvance = 1 +
                normalization.LineContinuations.CountAtOrBefore(index) -
                normalization.LineContinuations.CountAtOrBefore(lineStart - 1);
            var directiveMarker = normalizedSource.IndexOf('@', lineStart, lineLength);
            if (directiveMarker < 0 ||
                !AwkDirectiveParser.TryParseIncludeLine(normalizedSource[lineStart..lineEnd], out var includePath))
            {
                sourceLine += sourceLineAdvance;
                continue;
            }

            AppendSourceSegment(
                builder,
                segments,
                sourceName,
                sourceKind,
                normalizedSource,
                sourceChunkStart,
                lineStart - sourceChunkStart,
                sourceChunkLine,
                0,
                normalization.LineContinuations.Slice(
                    sourceChunkStart,
                    lineStart - sourceChunkStart));
            var include = await TryReadIncludeAsync(host, plan, includePath, resources, cancellationToken)
                .ConfigureAwait(false);
            if (include is SourceExpansionResult.Failed)
                return include;

            if (include is not SourceExpansionResult.Loaded loadedInclude)
                throw new InvalidOperationException("Unexpected source expansion result.");

            AppendExpandedSource(builder, segments, loadedInclude.Source);
            if (loadedInclude.Source.Text.Length > 0 && loadedInclude.Source.Text[^1] != '\n')
            {
                AwkSourceSegment? last = loadedInclude.Source.Segments.Count == 0
                    ? null
                    : loadedInclude.Source.Segments[^1];
                AppendSyntheticNewLine(builder, segments, last);
            }

            sourceLine += sourceLineAdvance;
            sourceChunkStart = index;
            sourceChunkLine = sourceLine;
        }

        AppendSourceSegment(
            builder,
            segments,
            sourceName,
            sourceKind,
            normalizedSource,
            sourceChunkStart,
            normalizedSource.Length - sourceChunkStart,
            sourceChunkLine,
            0,
            normalization.LineContinuations.Slice(
                sourceChunkStart,
                normalizedSource.Length - sourceChunkStart));

        return SourceExpansionResult.FromSource(AwkExpandedSource.Create(builder.ToString(), segments));
        static void AppendSourceSegment(
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

        static void AppendExpandedSource(
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

        static void AppendSyntheticNewLine(
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
    }

}
