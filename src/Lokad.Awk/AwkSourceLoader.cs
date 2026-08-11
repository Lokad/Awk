using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    public static async Task<AwkSourceLoadResult> LoadAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkRuntimeResourceMonitor resources,
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
                var sourceName = new AwkSourceName($"<inline:{inlineSourceIndex}>");
                var expanded = await ExpandIncludesAsync(
                        host,
                        plan,
                        inline.InlineScript,
                        sourceName,
                        AwkSourceSegmentKind.Inline,
                        resources,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (expanded is SourceExpansionResult.Failed inlineFailure)
                    return AwkSourceLoadResult.Failure(inlineFailure.ErrorMessage, inlineFailure.Id);

                if (expanded is not SourceExpansionResult.Loaded loaded)
                    throw new System.InvalidOperationException("Unexpected source expansion result.");

                sources.Add(new AwkSourceUnit(sourceName, loaded.Source.Text, loaded.Source.Segments));
                continue;
            }

            if (source is AwkSourceReference.IncludeSource includeSource)
            {
                var includePath = includeSource.ScriptPath;
                var include = await TryReadIncludeAsync(
                        host,
                        plan,
                        AwkUtf8Codec.Decode(includePath.Display),
                        resources,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (include is SourceExpansionResult.Failed includeFailure)
                    return AwkSourceLoadResult.Failure(includeFailure.ErrorMessage, includeFailure.Id);

                if (include is not SourceExpansionResult.Loaded loaded)
                    throw new System.InvalidOperationException("Unexpected source expansion result.");

                sources.Add(new AwkSourceUnit(
                    new AwkSourceName(AwkUtf8Codec.Decode(includePath.Display)),
                    loaded.Source.Text,
                    loaded.Source.Segments));
                continue;
            }

            var fileSource = (AwkSourceReference.FileSource)source;
            var script = await AwkScriptFileReader.TryReadAsync(
                    host,
                    plan,
                    fileSource.ScriptPath,
                    resources,
                    cancellationToken)
                .ConfigureAwait(false);
            if (script is AwkScriptFileReadResult.Failed scriptFailure)
            {
                return AwkSourceLoadResult.Failure(scriptFailure.ErrorMessage, scriptFailure.Id);
            }

            if (script is not AwkScriptFileReadResult.Found found)
            {
                return AwkSourceLoadResult.Failure(
                    AwkMessages.FormatPathError(fileSource.ScriptPath.Display),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            var expandedScript = await ExpandIncludesAsync(
                    host,
                    plan,
                    found.Script.Text,
                    found.Script.SourceName,
                    AwkSourceSegmentKind.File,
                    resources,
                    cancellationToken)
                .ConfigureAwait(false);
            if (expandedScript is SourceExpansionResult.Failed expansionFailure)
                return AwkSourceLoadResult.Failure(expansionFailure.ErrorMessage, expansionFailure.Id);

            if (expandedScript is not SourceExpansionResult.Loaded expandedFile)
                throw new System.InvalidOperationException("Unexpected source expansion result.");

            sources.Add(new AwkSourceUnit(
                found.Script.SourceName,
                expandedFile.Source.Text,
                expandedFile.Source.Segments));
        }

        return AwkSourceLoadResult.FromSources(sources);
    }
}
