namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    private static async Task<SourceExpansionResult> TryReadIncludeAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        string includePath,
        int includeDepth,
        CancellationToken cancellationToken)
    {
        var source = await TryReadIncludeSourceAsync(host, plan, includePath, includeDepth,
            cancellationToken).ConfigureAwait(false);
        return source is SourceExpansionResult.Unexpanded unexpanded
            ? await ExpandIncludesAsync(host, plan, unexpanded.Script.Text, unexpanded.Script.SourceName,
                AwkSourceSegmentKind.Include, includeDepth, cancellationToken).ConfigureAwait(false)
            : source;
    }

    private static async Task<SourceExpansionResult> TryReadIncludeSourceAsync(
        IAwkHost host, AwkSourceLoadPlan plan, string includePath, int includeDepth,
        CancellationToken cancellationToken)
    {
        if (includeDepth > plan.Options.Limits.MaxIncludeDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorIncludeDepthLimit, AwkDiagnosticId.RuntimeResourceLimit);
        AwkFileOperand resolved;
        try
        {
            resolved = plan.PreparePath(includePath);
        }
        catch (AwkRuntimeException error) when (error.Id == AwkDiagnosticId.HostIoFailure)
        {
            return SourceExpansionResult.Failure(error.MessageBytes, error.Id);
        }
        var script = await AwkScriptFileReader.TryReadAsync(
                host,
                plan,
                resolved,
                cancellationToken)
            .ConfigureAwait(false);
        if (script is AwkScriptReadResult.Missing && !includePath.EndsWith(".awk", StringComparison.Ordinal))
        {
            resolved = plan.PreparePath(includePath + ".awk");
            script = await AwkScriptFileReader.TryReadAsync(
                    host,
                    plan,
                    resolved,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (script is AwkScriptReadResult.Failed failed)
        {
            return new SourceExpansionResult.Failed(failed.Diagnostic);
        }

        if (script is not AwkScriptReadResult.Found found)
        {
            return SourceExpansionResult.Failure(
                AwkMessages.FormatPathError(resolved.Display),
                AwkDiagnosticId.SourceLoadFailure);
        }

        var registration = plan.RegisterSource(found.ResolvedPath, AwkSourceSegmentKind.Include);
        if (registration == AwkSourceRegistration.Conflict)
            return SourceExpansionResult.Failure(
                AwkMessages.FormatSourceRoleConflict(found.SourceName.Text), AwkDiagnosticId.SourceLoadFailure);
        if (registration == AwkSourceRegistration.Repeated)
            return SourceExpansionResult.FromSource(AwkExpandedSource.Empty);

        return new SourceExpansionResult.Unexpanded(found);
    }

    private abstract record SourceExpansionResult
    {
        public static Loaded FromSource(AwkExpandedSource source) =>
            new Loaded(source);

        public static Failed Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
            new Failed(new(errorMessage, id));

        internal sealed record Unexpanded(AwkScriptReadResult.Found Script) : SourceExpansionResult;

        internal sealed record Loaded(AwkExpandedSource Source) : SourceExpansionResult;

        internal sealed record Failed(AwkFailureDiagnostic Diagnostic) : SourceExpansionResult;
    }
}
