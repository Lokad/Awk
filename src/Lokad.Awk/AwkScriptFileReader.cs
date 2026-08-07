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
        CancellationToken cancellationToken)
    {
        var direct = await TryReadResolvedAsync(host, path, cancellationToken).ConfigureAwait(false);
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
        if (!rawPath.StartsWith("/", StringComparison.Ordinal))
        {
            var fromAwkPath = await TryReadFromAwkPathAsync(host, plan, rawPath, cancellationToken)
                .ConfigureAwait(false);
            if (fromAwkPath is not AwkScriptFileReadResult.Missing)
                return fromAwkPath;
        }

        return AwkScriptFileReadResult.MissingFile;
    }

    private static async Task<AwkScriptFileReadResult> TryReadFromAwkPathAsync(
        IAwkHost host,
        AwkSourceLoadPlan plan,
        string rawPath,
        CancellationToken cancellationToken)
    {
        if (!plan.TryGetEnvironmentValue("AWKPATH", out var awkPath) || awkPath.Length == 0)
            return AwkScriptFileReadResult.MissingFile;

        var entries = awkPath.Split(':', StringSplitOptions.None);
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index].Length == 0 ? "." : entries[index];
            var candidate = entry == "."
                ? rawPath
                : entry.TrimEnd('/') + "/" + rawPath;
            var resolved = plan.ResolvePath(candidate);
            var script = await TryReadResolvedAsync(host, resolved, cancellationToken)
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

    private static async Task<AwkScriptTextReadResult> TryReadResolvedAsync(
        IAwkHost host,
        AwkResolvedPath path,
        CancellationToken cancellationToken)
    {
        var opened = await host.OpenFileAsync(path.Absolute, AwkFileOpenMode.Read, cancellationToken)
            .ConfigureAwait(false);
        if (!opened.Success)
            return AwkScriptTextReadResult.MissingText;

        AwkScriptTextReadResult readResult;
        try
        {
            readResult = await ReadOpenedSourceAsync(
                    host,
                    opened.FileDescriptor,
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            try
            {
                _ = await host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A cleanup failure must not replace a read failure or cancellation.
            }

            throw;
        }

        try
        {
            var closed = await host.CloseDescriptorAsync(opened.FileDescriptor, CancellationToken.None)
                .ConfigureAwait(false);
            if (!closed && readResult is not AwkScriptTextReadResult.Failed)
            {
                return AwkScriptTextReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, "Close failed."),
                    AwkDiagnosticId.SourceLoadFailure);
            }
        }
        catch (Exception) when (readResult is AwkScriptTextReadResult.Failed)
        {
            return readResult;
        }
        catch (Exception)
        {
            return AwkScriptTextReadResult.Failure(
                AwkMessages.FormatPathError(path.Display, "Close failed."),
                AwkDiagnosticId.SourceLoadFailure);
        }

        return readResult;
    }

    private static async Task<AwkScriptTextReadResult> ReadOpenedSourceAsync(
        IAwkHost host,
        AwkFileDescriptor descriptor,
        AwkResolvedPath path,
        CancellationToken cancellationToken)
    {
        try
        {
            var builder = new StringBuilder();
            var counters = AwkRuntimeCounters.Disabled;
            var reader = new AsyncLineReader(
                host,
                descriptor,
                cancellationToken,
                int.MaxValue,
                counters,
                new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters));
            while (!await reader.ReachedEof().ConfigureAwait(false))
            {
                var line = reader.ReadLine();
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
