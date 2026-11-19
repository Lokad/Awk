using System.Text;

namespace Lokad.Awk;

internal static class AwkScriptFileReader
{
    public static async Task<AwkScriptReadResult> TryReadAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        AwkFileOperand path,
        CancellationToken cancellationToken)
    {
        path = plan.BindHostPolicy(path);
        var rawPath = AwkUtf8Codec.Decode(path.Display);
        var awkPath = plan.AwkPath;
        if (rawPath.Contains('/') || awkPath.Length == 0)
            return await ReadCandidateAsync(host, plan, path, cancellationToken).ConfigureAwait(false);

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
                candidate = plan.PreparePath(string.Concat(awkPath.AsSpan(start, length), "/", rawPath));
            }
            var result = await ReadCandidateAsync(host, plan, candidate, cancellationToken).ConfigureAwait(false);
            if (result is not AwkScriptReadResult.Missing || end < 0)
                return result;
            start = end + 1;
        }
    }

    private static async Task<AwkScriptReadResult> ReadCandidateAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        AwkFileOperand path,
        CancellationToken cancellationToken)
    {
        if (!plan.TryGetRequestText(path.Request, out var result))
        {
            result = await ReadRequestedFileAsync(host, path, cancellationToken, plan).ConfigureAwait(false);
            plan.CacheRequestText(path.Request, result);
        }

        // Keep cached text independent of the spelling used to reach it.
        return result is AwkScriptReadResult.Found found
            ? found with { SourceName = new AwkSourceName(AwkUtf8Codec.Decode(path.Display)) }
            : result;

        static async Task<AwkScriptReadResult> ReadRequestedFileAsync(
            IAwkHost host,
            AwkFileOperand path,
            CancellationToken cancellationToken,
            AwkSourceLoadPlan plan)
        {
            var descriptorRetired = false;
            async Task CloseForCleanupAsync(AwkFileDescriptor descriptor)
            {
                if (descriptorRetired) return;
                try
                {
                    plan.Options.Counters.Add(AwkCounter.HostCalls);
                    await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Cleanup cannot replace the failure that interrupted source loading.
                }
                finally { descriptorRetired = true; }
            }

            cancellationToken.ThrowIfCancellationRequested();
            plan.Options.Counters.Add(AwkCounter.HostCalls);
            var opened = await host.OpenFileAsync(path.Request, AwkFileOpenMode.Read, cancellationToken)
                .ConfigureAwait(false);
            AwkHostPathPolicy.ThrowIfDenied(opened);
            if (!opened.Success && opened.Error.Kind == AwkOperationErrorKind.NotFound)
                return AwkScriptReadResult.NotFound;

            if (!opened.Success)
            {
                return AwkScriptReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, plan.ResolveHostError(opened.Error)),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            AwkScriptReadResult readResult;
            try
            {
                readResult = plan.TryGetResolvedText(opened.ResolvedPath, out var previous) ? previous :
                    await ReadOpenedSourceAsync(
                        host,
                        opened.FileDescriptor,
                        path,
                        opened.ResolvedPath,
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
                cancellationToken.ThrowIfCancellationRequested();
                plan.Options.Counters.Add(AwkCounter.HostCalls);
                var closed = await host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None)
                    .ConfigureAwait(false);
                descriptorRetired = closed.DescriptorRetired;
                if (!closed.Success)
                {
                    await CloseForCleanupAsync(opened.FileDescriptor).ConfigureAwait(false);
                    if (readResult is AwkScriptReadResult.Failed)
                        return readResult;

                    return AwkScriptReadResult.Failure(
                        AwkMessages.FormatPathError(path.Display, closed.Error.HasMessage
                            ? "Close failed: " + plan.ResolveHostError(closed.Error) : "Close failed."),
                        AwkDiagnosticId.SourceLoadFailure);
                }
            }
            catch (Exception exception)
            {
                await CloseForCleanupAsync(opened.FileDescriptor).ConfigureAwait(false);
                if (exception is not AwkHostException)
                    throw;
                if (readResult is AwkScriptReadResult.Failed)
                    return readResult;

                return AwkScriptReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, "Close failed."),
                    AwkDiagnosticId.SourceLoadFailure);
            }

            return readResult;

            static async Task<AwkScriptReadResult> ReadOpenedSourceAsync(
                IAwkHost host,
                AwkFileDescriptor descriptor,
                AwkFileOperand path,
                AwkPath identity,
                CancellationToken cancellationToken,
                AwkSourceLoadPlan plan)
            {
                try
                {
                    plan.Memory.AdmitTextCopy(32);
                    cancellationToken.ThrowIfCancellationRequested();
                    var builder = new StringBuilder();
                    var decoder = AwkUtf8Codec.CreateDecoder();
                    using var reader = new AwkByteReader(host, descriptor, cancellationToken, plan.Options);
                    try
                    {
                        while (await reader.TryBufferAsync().ConfigureAwait(false))
                        {
                            plan.Memory.AdmitTextCopy(reader.Content.Length);
                            cancellationToken.ThrowIfCancellationRequested();
                            var content = reader.Content;
                            DecodeChunk(content.Span, reader.ReachedEof);
                            reader.Advance(content.Length);
                        }
                        DecodeChunk(ReadOnlySpan<byte>.Empty, true);

                        void DecodeChunk(ReadOnlySpan<byte> bytes, bool final)
                        {
                            Span<char> characters = stackalloc char[1_024];
                            bool completed;
                            do
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                decoder.Convert(bytes, characters, final,
                                    out var consumed, out var written, out completed);
                                builder.Append(characters[..written]);
                                bytes = bytes[consumed..];
                            } while (!completed);
                        }
                    }
                    catch (DecoderFallbackException)
                    {
                        return AwkScriptReadResult.Failure(
                            AwkMessages.FormatPathError(path.Display, "invalid UTF-8 source"),
                            AwkDiagnosticId.SourceLoadFailure);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    return new AwkScriptReadResult.Found(builder.ToString(), identity);
                }
                catch (AwkHostReadException exception)
                {
                    return AwkScriptReadResult.Failure(
                        AwkMessages.FormatPathError(path.Display, exception.Error.Message),
                        AwkDiagnosticId.SourceLoadFailure);
                }
            }
        }
    }
}

internal abstract record AwkScriptReadResult
{
    public static AwkScriptReadResult NotFound { get; } = new Missing();

    public static AwkScriptReadResult Failure(ReadOnlyMemory<byte> message, AwkDiagnosticId id) =>
        new Failed(AwkDiagnostics.SourceLoad(message, id));

    internal sealed record Missing : AwkScriptReadResult;

    internal sealed record Found(string Text, AwkPath ResolvedPath) : AwkScriptReadResult
    {
        public AwkSourceName SourceName { get; init; }
    }

    internal sealed record Failed(AwkFailureDiagnostic Diagnostic) : AwkScriptReadResult;
}
