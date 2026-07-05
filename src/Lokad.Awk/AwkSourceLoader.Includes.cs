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

            var line = normalizedSource[lineStart..lineEnd];
            var lineLength = lineEnd - lineStart;
            var lineContinuations = normalization.LineContinuations.Slice(lineStart, lineLength);
            var sourceLineAdvance = 1 + lineContinuations.Count;
            if (!AwkDirectiveParser.TryParseIncludeLine(line, out var includePath))
            {
                AppendSourceSegment(
                    builder,
                    segments,
                    sourceName,
                    sourceKind,
                    normalizedSource[lineStart..index],
                    sourceLine,
                    0,
                    normalization.LineContinuations.Slice(lineStart, index - lineStart));
                sourceLine += sourceLineAdvance;
                continue;
            }

            var include = await TryReadIncludeAsync(host, plan, includePath, cancellationToken)
                .ConfigureAwait(false);
            if (include.Status == SourceExpansionStatus.Failed)
                return include;

            AppendExpandedSource(builder, segments, include.Source);
            if (include.Source.Text.Length > 0 && include.Source.Text[^1] != '\n')
            {
                AwkSourceSegment? last = include.Source.Segments.Count == 0
                    ? null
                    : include.Source.Segments[^1];
                AppendSyntheticNewLine(builder, segments, last);
            }

            sourceLine += sourceLineAdvance;
        }

        return SourceExpansionResult.FromSource(AwkExpandedSource.Create(builder.ToString(), segments));
    }

}
