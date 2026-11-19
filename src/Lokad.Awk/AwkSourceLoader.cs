namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    public static async Task<AwkSourceLoadResult> LoadAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        var plan = AwkSourceLoadPlan.FromInvocation(invocation, options, cancellationToken);
        plan.Memory.AdmitSourceUnits(plan.SourceCount);
        var sources = new List<AwkSourceUnit>(plan.SourceCount);
        var inlineSourceIndex = 0;
        for (var index = 0; index < plan.SourceCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                        includeDepth: 0,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (expanded is SourceExpansionResult.Failed inlineFailure)
                    return new AwkSourceLoadResult.Failed(inlineFailure.Diagnostic);

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
                        includeDepth: 1,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (include is SourceExpansionResult.Failed includeFailure)
                    return new AwkSourceLoadResult.Failed(includeFailure.Diagnostic);

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
                    cancellationToken)
                .ConfigureAwait(false);
            if (script is AwkScriptReadResult.Failed scriptFailure)
            {
                return new AwkSourceLoadResult.Failed(scriptFailure.Diagnostic);
            }

            if (script is not AwkScriptReadResult.Found found)
            {
                return AwkSourceLoadResult.Failure(
                    AwkMessages.FormatPathError(fileSource.ScriptPath.Display),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            if (plan.RegisterSource(found.ResolvedPath, AwkSourceSegmentKind.File) == AwkSourceRegistration.Conflict)
                return AwkSourceLoadResult.Failure(
                    AwkMessages.FormatSourceRoleConflict(found.SourceName.Text), AwkDiagnosticId.SourceLoadFailure);

            var expandedScript = await ExpandIncludesAsync(
                    host,
                    plan,
                    found.Text,
                    found.SourceName,
                    AwkSourceSegmentKind.File,
                    includeDepth: 0,
                    cancellationToken)
                .ConfigureAwait(false);
            if (expandedScript is SourceExpansionResult.Failed expansionFailure)
                return new AwkSourceLoadResult.Failed(expansionFailure.Diagnostic);

            if (expandedScript is not SourceExpansionResult.Loaded expandedFile)
                throw new System.InvalidOperationException("Unexpected source expansion result.");
            sources.Add(new AwkSourceUnit(
                found.SourceName,
                expandedFile.Source.Text,
                expandedFile.Source.Segments));
        }

        return AwkSourceLoadResult.FromSources(sources, cancellationToken.ThrowIfCancellationRequested, plan.Memory);
    }
}
