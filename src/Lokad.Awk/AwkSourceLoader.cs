using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    public static async Task<AwkSourceLoadResult> LoadAsync(
        IAwkHost host,
        AwkInvocation invocation,
        CancellationToken cancellationToken)
    {
        var plan = AwkSourceLoadPlan.FromInvocation(invocation);
        var sources = new List<AwkSourceUnit>(plan.SourceCount);
        var inlineSourceIndex = 0;
        for (var index = 0; index < plan.SourceCount; index++)
        {
            var source = plan.GetSource(index);
            if (source is AwkSourceReference.InlineSource inline)
            {
                inlineSourceIndex++;
                var sourceName = plan.GetInlineSourceName(inlineSourceIndex);
                var expanded = await ExpandIncludesAsync(
                        host,
                        plan,
                        inline.InlineScript,
                        sourceName,
                        AwkSourceSegmentKind.Inline,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (expanded.Status == SourceExpansionStatus.Failed)
                    return AwkSourceLoadResult.Failure(expanded.ErrorMessage, expanded.Id);

                sources.Add(new AwkSourceUnit(sourceName, expanded.Source.Text, expanded.Source.Segments));
                continue;
            }

            if (source is AwkSourceReference.IncludeSource includeSource)
            {
                var includePath = includeSource.ScriptPath;
                var include = await TryReadIncludeAsync(
                        host,
                        plan,
                        AwkUtf8Codec.Decode(includePath.Display),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (include.Status == SourceExpansionStatus.Failed)
                    return AwkSourceLoadResult.Failure(include.ErrorMessage, include.Id);

                sources.Add(new AwkSourceUnit(
                    new AwkSourceName(AwkUtf8Codec.Decode(includePath.Display)),
                    include.Source.Text,
                    include.Source.Segments));
                continue;
            }

            var fileSource = (AwkSourceReference.FileSource)source;
            var script = await AwkScriptFileReader.TryReadAsync(
                    host,
                    plan,
                    fileSource.ScriptPath,
                    cancellationToken)
                .ConfigureAwait(false);
            if (script.Status == AwkScriptFileReadStatus.Failed)
            {
                return AwkSourceLoadResult.Failure(script.ErrorMessage, script.Id);
            }

            if (script.Status != AwkScriptFileReadStatus.Found)
            {
                return AwkSourceLoadResult.Failure(
                    AwkMessages.FormatPathError(fileSource.ScriptPath.Display),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            var expandedScript = await ExpandIncludesAsync(
                    host,
                    plan,
                    script.Script.Text,
                    script.Script.SourceName,
                    AwkSourceSegmentKind.File,
                    cancellationToken)
                .ConfigureAwait(false);
            if (expandedScript.Status == SourceExpansionStatus.Failed)
                return AwkSourceLoadResult.Failure(expandedScript.ErrorMessage, expandedScript.Id);

            sources.Add(new AwkSourceUnit(
                script.Script.SourceName,
                expandedScript.Source.Text,
                expandedScript.Source.Segments));
        }

        return AwkSourceLoadResult.FromSources(sources);
    }
}
