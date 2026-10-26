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
        var rawPath = AwkUtf8Codec.Decode(path.Display);
        var awkPath = plan.AwkPath;
        if (rawPath.Contains('/') || awkPath.Length == 0)
            return await ReadCandidateAsync(path).ConfigureAwait(false);

        for (var start = 0; ;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var end = awkPath.IndexOf(':', start);
            var length = (end < 0 ? awkPath.Length : end) - start;
            var candidate = path;
            if (length != 0 && !(length == 1 && awkPath[start] == '.'))
            {
                var candidateLength = AwkRuntimeMemory.CheckedAdd(length,
                    AwkRuntimeMemory.CheckedAdd(rawPath.Length, 1));
                plan.Memory.AdmitTextCopy(candidateLength);
                cancellationToken.ThrowIfCancellationRequested();
                candidate = plan.ResolvePath(string.Concat(awkPath.AsSpan(start, length), "/", rawPath));
            }
            var result = await ReadCandidateAsync(candidate).ConfigureAwait(false);
            if (result is not AwkScriptFileReadResult.Missing)
                return result;
            if (end < 0)
                return AwkScriptFileReadResult.MissingFile;
            start = end + 1;
        }

        async Task<AwkScriptFileReadResult> ReadCandidateAsync(AwkResolvedPath candidate)
        {
            plan.ThrowIfDenied(candidate.Absolute);
            var result = await TryReadResolvedAsync(host, plan, candidate, resources, cancellationToken)
                .ConfigureAwait(false);
            return result switch
            {
                AwkScriptTextReadResult.Found found => AwkScriptFileReadResult.FoundScript(new AwkScriptFile(
                    found.Text, new AwkSourceName(AwkUtf8Codec.Decode(candidate.Display)),
                    new AwkSourceIdentity(candidate.Absolute.Path))),
                AwkScriptTextReadResult.Failed failed => AwkScriptFileReadResult.Failure(failed.ErrorMessage, failed.Id),
                _ => AwkScriptFileReadResult.MissingFile
            };
        }
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
            var descriptorRetired = false;
            async Task CloseForCleanupAsync(AwkFileDescriptor descriptor)
            {
                if (descriptorRetired) return;
                try
                {
                    resources.RecordCleanupHostCall();
                    await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Cleanup cannot replace the failure that interrupted source loading.
                }
                finally { descriptorRetired = true; }
            }

            resources.RecordHostCall();
            var opened = await host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken)
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
                var closed = await host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None)
                    .ConfigureAwait(false);
                descriptorRetired = closed.DescriptorRetired;
                if (!closed.Success)
                {
                    await CloseForCleanupAsync(opened.FileDescriptor).ConfigureAwait(false);
                    if (readResult is AwkScriptTextReadResult.Failed)
                        return readResult;

                    return AwkScriptTextReadResult.Failure(
                        AwkMessages.FormatPathError(path.Display, closed.Error.HasMessage
                            ? "Close failed: " + plan.ResolveHostError(closed.Error) : "Close failed."),
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
                        exposeHostErrorDetails: plan.ExposeHostErrorDetails,
                        maxHostErrorBytes: plan.MaxHostErrorBytes);
                    while (await reader.TryBufferLineAsync().ConfigureAwait(false))
                    {
                        resources.Checkpoint();
                        var line = reader.ReadLine();
                        if (line.IsEmpty)
                            continue;

                        try
                        {
                            plan.Memory.AdmitTextCopy(line.Length);
                            resources.Checkpoint();
                            builder.Append(AwkUtf8Codec.Decode(line));
                        }
                        catch (DecoderFallbackException)
                        {
                            return AwkScriptTextReadResult.Failure(
                                AwkMessages.FormatPathError(path.Display, "invalid UTF-8 source"),
                                AwkDiagnosticId.SourceLoadFailure);
                        }
                    }

                    resources.Checkpoint();
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
