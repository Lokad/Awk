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
        if (direct.Status == AwkScriptTextReadStatus.Found)
        {
            return AwkScriptFileReadResult.FoundScript(new AwkScriptFile(
                direct.Text,
                new AwkSourceName(AwkUtf8Codec.Decode(path.Display)),
                new AwkSourceIdentity(path.Absolute.Path)));
        }

        if (direct.Status == AwkScriptTextReadStatus.Failed)
            return AwkScriptFileReadResult.Failure(direct.ErrorMessage, direct.Id);

        var rawPath = AwkUtf8Codec.Decode(path.Display);
        if (!rawPath.StartsWith("/", StringComparison.Ordinal))
        {
            var fromAwkPath = await TryReadFromAwkPathAsync(host, plan, rawPath, cancellationToken)
                .ConfigureAwait(false);
            if (fromAwkPath.Status is AwkScriptFileReadStatus.Found or AwkScriptFileReadStatus.Failed)
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
            if (script.Status == AwkScriptTextReadStatus.Found)
            {
                return AwkScriptFileReadResult.FoundScript(new AwkScriptFile(
                    script.Text,
                    new AwkSourceName(AwkUtf8Codec.Decode(resolved.Display)),
                    new AwkSourceIdentity(resolved.Absolute.Path)));
            }

            if (script.Status == AwkScriptTextReadStatus.Failed)
                return AwkScriptFileReadResult.Failure(script.ErrorMessage, script.Id);
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
            if (!closed && readResult.Status != AwkScriptTextReadStatus.Failed)
            {
                return AwkScriptTextReadResult.Failure(
                    AwkMessages.FormatPathError(path.Display, "Close failed."),
                    AwkDiagnosticId.SourceLoadFailure);
            }
        }
        catch (Exception) when (readResult.Status == AwkScriptTextReadStatus.Failed)
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
            var reader = new AsyncLineReader(host, descriptor, cancellationToken);
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

internal readonly struct AwkScriptFileReadResult
{
    private AwkScriptFileReadResult(
        AwkScriptFileReadStatus status,
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

    public AwkScriptFileReadStatus Status { get; }

    public AwkScriptFile Script =>
        Status == AwkScriptFileReadStatus.Found
            ? (AwkScriptFile)_payload
            : throw new InvalidOperationException("No script file is present.");

    public ReadOnlyMemory<byte> ErrorMessage { get; }

    public AwkDiagnosticId Id { get; }

    public static AwkScriptFileReadResult MissingFile { get; } =
        new(AwkScriptFileReadStatus.Missing, NoPayload.Instance, default, default);

    public static AwkScriptFileReadResult FoundScript(AwkScriptFile script) =>
        new(AwkScriptFileReadStatus.Found, script, default, default);

    public static AwkScriptFileReadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new(AwkScriptFileReadStatus.Failed, NoPayload.Instance, errorMessage, id);

    private sealed class NoPayload
    {
        public static readonly NoPayload Instance = new();
    }
}

internal enum AwkScriptFileReadStatus
{
    Missing,
    Found,
    Failed
}

internal readonly struct AwkScriptTextReadResult
{
    private AwkScriptTextReadResult(
        AwkScriptTextReadStatus status,
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

    public AwkScriptTextReadStatus Status { get; }

    public string Text =>
        Status == AwkScriptTextReadStatus.Found
            ? (string)_payload
            : throw new InvalidOperationException("No script text is present.");

    public ReadOnlyMemory<byte> ErrorMessage { get; }

    public AwkDiagnosticId Id { get; }

    public static AwkScriptTextReadResult MissingText { get; } =
        new(AwkScriptTextReadStatus.Missing, NoPayload.Instance, default, default);

    public static AwkScriptTextReadResult FoundText(string text) =>
        new(AwkScriptTextReadStatus.Found, text, default, default);

    public static AwkScriptTextReadResult Failure(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId id) =>
        new(AwkScriptTextReadStatus.Failed, NoPayload.Instance, errorMessage, id);

    private sealed class NoPayload
    {
        public static readonly NoPayload Instance = new();
    }
}

internal enum AwkScriptTextReadStatus
{
    Missing,
    Found,
    Failed
}
