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
        CancellationToken cancellationToken)
    {
        var resolved = plan.ResolvePath(includePath);
        var script = await AwkScriptFileReader.TryReadAsync(
                host,
                plan,
                resolved,
                cancellationToken)
            .ConfigureAwait(false);
        if (script.Status == AwkScriptFileReadStatus.Missing && !includePath.EndsWith(".awk", StringComparison.Ordinal))
        {
            resolved = plan.ResolvePath(includePath + ".awk");
            script = await AwkScriptFileReader.TryReadAsync(
                    host,
                    plan,
                    resolved,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (script.Status != AwkScriptFileReadStatus.Found)
        {
            if (script.Status == AwkScriptFileReadStatus.Failed)
                return SourceExpansionResult.Failure(script.ErrorMessage, script.Id);

            return SourceExpansionResult.Failure(
                AwkMessages.FormatPathError(resolved.Display),
                AwkDiagnosticId.SourceLoadFailure);
        }

        if (!plan.TryMarkIncluded(script.Script))
            return SourceExpansionResult.FromSource(AwkExpandedSource.Empty);

        return await ExpandIncludesAsync(
                host,
                plan,
                script.Script.Text,
                script.Script.SourceName,
                AwkSourceSegmentKind.Include,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private readonly struct SourceExpansionResult
    {
        private SourceExpansionResult(
            SourceExpansionStatus status,
            object payload,
            ReadOnlyMemory<byte> errorMessage,
            AwkDiagnosticId id)
        {
            ArgumentNullException.ThrowIfNull(payload);
            Status = status;
            _payload = payload;
            ErrorMessage = errorMessage;
            Id = id;
        }

        private readonly object _payload;

        public SourceExpansionStatus Status { get; }

        public AwkExpandedSource Source =>
            Status == SourceExpansionStatus.Loaded
                ? (AwkExpandedSource)_payload
                : throw new InvalidOperationException("No expanded source is present.");

        public ReadOnlyMemory<byte> ErrorMessage { get; }

        public AwkDiagnosticId Id { get; }

        public static SourceExpansionResult FromSource(AwkExpandedSource source) =>
            new(SourceExpansionStatus.Loaded, source, default, default);

        public static SourceExpansionResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
            new(SourceExpansionStatus.Failed, NoPayload.Instance, errorMessage, id);

        private sealed class NoPayload
        {
            public static readonly NoPayload Instance = new();
        }
    }

    private enum SourceExpansionStatus
    {
        Loaded,
        Failed
    }
}
