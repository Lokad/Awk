using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkSourceLoader
{
    private static async Task<SourceExpansionResult> TryReadIncludeAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        string includePath,
        int includeDepth,
        AwkRuntimeResourceMonitor resources,
        CancellationToken cancellationToken)
    {
        resources.RecordInclude(includeDepth);
        AwkResolvedPath resolved;
        try
        {
            resolved = plan.ResolvePath(includePath);
        }
        catch (AwkRuntimeException error) when (error.Id == AwkDiagnosticId.HostIoFailure)
        {
            return SourceExpansionResult.Failure(error.MessageBytes, error.Id);
        }
        var script = await AwkScriptFileReader.TryReadAsync(
                host,
                plan,
                resolved,
                resources,
                cancellationToken)
            .ConfigureAwait(false);
        if (script is AwkScriptFileReadResult.Missing && !includePath.EndsWith(".awk", StringComparison.Ordinal))
        {
            resolved = plan.ResolvePath(includePath + ".awk");
            script = await AwkScriptFileReader.TryReadAsync(
                    host,
                    plan,
                    resolved,
                    resources,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (script is AwkScriptFileReadResult.Failed failed)
        {
            return SourceExpansionResult.Failure(failed.ErrorMessage, failed.Id);
        }

        if (script is not AwkScriptFileReadResult.Found found)
        {
            return SourceExpansionResult.Failure(
                AwkMessages.FormatPathError(resolved.Display),
                AwkDiagnosticId.SourceLoadFailure);
        }

        var registration = plan.RegisterSource(found.Script, AwkSourceSegmentKind.Include);
        if (registration == AwkSourceRegistration.Conflict)
            return SourceExpansionResult.Failure(
                AwkMessages.FormatSourceRoleConflict(found.Script.SourceName.Text), AwkDiagnosticId.SourceLoadFailure);
        if (registration == AwkSourceRegistration.Repeated)
            return SourceExpansionResult.FromSource(AwkExpandedSource.Empty);

        return await ExpandIncludesAsync(
                host,
                plan,
                found.Script.Text,
                found.Script.SourceName,
                AwkSourceSegmentKind.Include,
                includeDepth,
                resources,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private abstract record SourceExpansionResult
    {
        public static Loaded FromSource(AwkExpandedSource source) =>
            new Loaded(source);

        public static Failed Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
            new Failed(errorMessage, id);

        internal sealed record Loaded(AwkExpandedSource Source) : SourceExpansionResult;

        internal sealed record Failed(
            ReadOnlyMemory<byte> ErrorMessage,
            AwkDiagnosticId Id) : SourceExpansionResult;
    }
}
