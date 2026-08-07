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
        CancellationToken cancellationToken)
    {
        var normalization = AwkLineContinuations.Normalize(source);
        var normalizedSource = normalization.Text;
        var builder = new StringBuilder(normalizedSource.Length);
        var segments = new List<AwkSourceSegment>();
        if (normalizedSource.IndexOf('@') < 0)
        {
            AppendSourceSegment(
                builder,
                segments,
                sourceName,
                sourceKind,
                normalizedSource,
                0,
                normalizedSource.Length,
                0,
                0,
                normalization.LineContinuations);
            return SourceExpansionResult.FromSource(AwkExpandedSource.Create(builder.ToString(), segments));
        }

        var index = 0;
        var sourceLine = 0;
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
            var lineContinuations = normalization.LineContinuations.Slice(lineStart, index - lineStart);
            var sourceLineAdvance = 1 + lineContinuations.Count;
            var directiveMarker = normalizedSource.IndexOf('@', lineStart, lineLength);
            if (directiveMarker < 0 ||
                !AwkDirectiveParser.TryParseIncludeLine(normalizedSource[lineStart..lineEnd], out var includePath))
            {
                AppendSourceSegment(
                    builder,
                    segments,
                    sourceName,
                    sourceKind,
                    normalizedSource,
                    lineStart,
                    index - lineStart,
                    sourceLine,
                    0,
                    lineContinuations);
                sourceLine += sourceLineAdvance;
                continue;
            }

            var include = await TryReadIncludeAsync(host, plan, includePath, cancellationToken)
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
        }

        return SourceExpansionResult.FromSource(AwkExpandedSource.Create(builder.ToString(), segments));
    }

}
