using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkScriptFileReader
{
    public static async Task<AwkScriptFileReadResult> TryReadAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        AwkResolvedPath path,
        AwkRuntimeResourceMonitor resources,
        CancellationToken cancellationToken)
    {
        plan.ThrowIfDenied(path.Absolute);
        static async Task<AwkScriptFileReadResult> TryReadFromAwkPathAsync(
            IAwkHost host,
            AwkSourceLoadPlan plan,
            string rawPath,
            AwkRuntimeResourceMonitor resources,
            CancellationToken cancellationToken)
        {
            var entries = plan.AwkPathEntries;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var candidate = entry == "."
                    ? rawPath
                    : entry + "/" + rawPath;
                var resolved = plan.ResolvePath(candidate);
                var script = await TryReadResolvedAsync(host, plan, resolved, resources, cancellationToken)
                    .ConfigureAwait(false);
                if (script is AwkScriptTextReadResult.Found found)
                {
                    return AwkScriptFileReadResult.FoundScript(new AwkScriptFile(
                        found.Text,
                        new AwkSourceName(AwkUtf8Codec.Decode(resolved.Display)),
                        new AwkSourceIdentity(resolved.Absolute.Path)));
                }

                if (script is AwkScriptTextReadResult.Failed failed)
                    return AwkScriptFileReadResult.Failure(failed.ErrorMessage, failed.Id);
            }

            return AwkScriptFileReadResult.MissingFile;
        }

        var direct = await TryReadResolvedAsync(host, plan, path, resources, cancellationToken).ConfigureAwait(false);
        if (direct is AwkScriptTextReadResult.Found found)
        {
            return AwkScriptFileReadResult.FoundScript(new AwkScriptFile(
                found.Text,
                new AwkSourceName(AwkUtf8Codec.Decode(path.Display)),
                new AwkSourceIdentity(path.Absolute.Path)));
        }

        if (direct is AwkScriptTextReadResult.Failed failed)
            return AwkScriptFileReadResult.Failure(failed.ErrorMessage, failed.Id);

        var rawPath = AwkUtf8Codec.Decode(path.Display);
        if (!rawPath.StartsWith('/'))
        {
            var fromAwkPath = await TryReadFromAwkPathAsync(host, plan, rawPath, resources, cancellationToken)
                .ConfigureAwait(false);
            if (fromAwkPath is not AwkScriptFileReadResult.Missing)
                return fromAwkPath;
        }

        return AwkScriptFileReadResult.MissingFile;
    }

    private static async Task<AwkScriptTextReadResult> TryReadResolvedAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        AwkResolvedPath path,
        AwkRuntimeResourceMonitor resources,
        CancellationToken cancellationToken)
    {
        if (plan.TryGetScriptText(path.Absolute, out var cached))
            return cached;

        var result = await ReadResolvedAsync(
                host,
                path,
                resources,
                cancellationToken,
                plan)
            .ConfigureAwait(false);
        plan.CacheScriptText(path.Absolute, result);
        return result;

        static async Task<AwkScriptTextReadResult> ReadResolvedAsync(
            IAwkHost host,
            AwkResolvedPath path,
            AwkRuntimeResourceMonitor resources,
            CancellationToken cancellationToken,
            AwkSourceLoadPlan plan)
        {
            async Task CloseForCleanupAsync(AwkFileDescriptor descriptor)
            {
                try
                {
                    resources.RecordCleanupHostCall();
                    await plan.CleanupDeadline.AwaitAsync(
                        host.CloseDescriptorAsync(descriptor, CancellationToken.None)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Cleanup cannot replace the failure that interrupted source loading.
                }
            }

            resources.RecordHostCall();
            var opened = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken),
                    plan.HostOperationTimeout,
                    cancellationToken)
                .ConfigureAwait(false);
            if (opened.Missing)
                return AwkScriptTextReadResult.MissingText;

            if (opened.Failed)
            {
                return AwkScriptTextReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, plan.ResolveHostError(opened.Error)),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            AwkScriptTextReadResult readResult;
            try
            {
                readResult = await ReadOpenedSourceAsync(
                        host,
                        opened.FileDescriptor,
                        path,
                        resources,
                        cancellationToken,
                        plan)
                    .ConfigureAwait(false);
            }
            catch
            {
                await CloseForCleanupAsync(opened.FileDescriptor).ConfigureAwait(false);

                throw;
            }

            try
            {
                resources.RecordHostCall();
                var closed = await AwkHostBoundaryPolicy.AwaitAsync(
                        host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None),
                        plan.CloseOperationTimeout,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                if (closed != AwkOperationStatus.Success &&
                    readResult is not AwkScriptTextReadResult.Failed)
                {
                    return AwkScriptTextReadResult.Failure(
                        AwkMessages.FormatPathError(path.Display, "Close failed."),
                        AwkDiagnosticId.SourceLoadFailure);
                }
            }
            catch (Exception exception)
            {
                await CloseForCleanupAsync(opened.FileDescriptor).ConfigureAwait(false);
                if (readResult is AwkScriptTextReadResult.Failed)
                    return readResult;

                if (exception is AwkRuntimeException)
                    throw;

                return AwkScriptTextReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, "Close failed."),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            return readResult;

            static async Task<AwkScriptTextReadResult> ReadOpenedSourceAsync(
                IAwkHost host,
                AwkFileDescriptor descriptor,
                AwkResolvedPath path,
                AwkRuntimeResourceMonitor resources,
                CancellationToken cancellationToken,
                AwkSourceLoadPlan plan)
            {
                try
                {
                    var builder = new StringBuilder();
                    var reader = new AsyncLineReader(
                        host,
                        descriptor,
                        cancellationToken,
                        int.MaxValue,
                        resources,
                        hostOperationTimeout: plan.HostOperationTimeout,
                        exposeHostErrorDetails: plan.ExposeHostErrorDetails,
                        maxHostErrorBytes: plan.MaxHostErrorBytes);
                    while (await reader.TryBufferLineAsync().ConfigureAwait(false))
                    {
                        var line = reader.ReadLine();
                        resources.RecordSourceBytes(line.Length);
                        if (line.IsEmpty)
                            continue;

                        try
                        {
                            builder.Append(AwkUtf8Codec.Decode(line));
                        }
                        catch (DecoderFallbackException)
                        {
                            return AwkScriptTextReadResult.Failure(
                                AwkMessages.FormatPathError(path.Display, "invalid UTF-8 source"),
                                AwkDiagnosticId.SourceLoadFailure);
                        }
                    }

                    return AwkScriptTextReadResult.FoundText(builder.ToString());
                }
                catch (AwkHostReadException exception)
                {
                    return AwkScriptTextReadResult.Failure(
                        AwkMessages.FormatPathError(path.Display, exception.Error.Message),
                        AwkDiagnosticId.SourceLoadFailure);
                }
            }
        }
    }
}

internal abstract record AwkScriptFileReadResult
{
    public static AwkScriptFileReadResult MissingFile { get; } =
        new Missing();

    public static AwkScriptFileReadResult FoundScript(AwkScriptFile script) =>
        new Found(script);

    public static AwkScriptFileReadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new Failed(errorMessage, id);

    internal sealed record Missing : AwkScriptFileReadResult;

    internal sealed record Found(AwkScriptFile Script) : AwkScriptFileReadResult;

    internal sealed record Failed(
        ReadOnlyMemory<byte> ErrorMessage,
        AwkDiagnosticId Id) : AwkScriptFileReadResult;
}

internal abstract record AwkScriptTextReadResult
{
    public static AwkScriptTextReadResult MissingText { get; } =
        new Missing();

    public static AwkScriptTextReadResult FoundText(string text) =>
        new Found(text);

    public static AwkScriptTextReadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new Failed(errorMessage, id);

    internal sealed record Missing : AwkScriptTextReadResult;

    internal sealed record Found(string Text) : AwkScriptTextReadResult;

    internal sealed record Failed(
        ReadOnlyMemory<byte> ErrorMessage,
        AwkDiagnosticId Id) : AwkScriptTextReadResult;
}
