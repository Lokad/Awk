using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkIoHostAdapter(
    IAwkHost host,
    AwkPath currentDirectory,
    AwkFileDescriptor stdIn,
    AwkFileDescriptor stdOut,
    AwkFileDescriptor stdErr,
    CancellationToken cancellationToken,
    Func<IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
    AwkRuntimeCounters counters,
    AwkRuntimeResourceMonitor resources)
{
    public AwkResolvedPath ResolvePath(string path)
    {
        try
        {
            return AwkPathResolution.ResolveArgument(path, currentDirectory);
        }
        catch (FormatException)
        {
            throw new AwkRuntimeException(
                AwkMessages.FormatPathError(AwkUtf8Codec.Encode(path), "Invalid path"),
                AwkDiagnosticId.HostIoFailure);
        }
    }

    public async Task WriteStandardOutputAsync(string content)
    {
        if (content.Length == 0)
            return;

        await WriteStandardOutputAsync(AwkUtf8Codec.Encode(content, counters)).ConfigureAwait(false);
    }

    public async Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await host.AppendAsync(stdOut, content, cancellationToken).ConfigureAwait(false);
        ThrowIfAppendFailed(stdOut, status);
    }

    public async Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await host.AppendAsync(stdErr, content, cancellationToken).ConfigureAwait(false);
        ThrowIfAppendFailed(stdErr, status);
    }

    public async Task AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await host.AppendAsync(descriptor, content, cancellationToken).ConfigureAwait(false);
        ThrowIfAppendFailed(descriptor, status);
    }

    public async Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode)
    {
        resources.RecordHostCall();
        return await host.CreatePipeAsync(mode, cancellationToken).ConfigureAwait(false);
    }

    public AwkCommandInvocation CreateOutputPipeCommand(string commandName, AwkFileDescriptor stdIn) =>
        new(
            commandName,
            Array.Empty<string>(),
            snapshotEnvironment(),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    public AwkCommandInvocation CreateInputPipeCommand(string commandName, AwkFileDescriptor stdOut) =>
        new(
            commandName,
            Array.Empty<string>(),
            snapshotEnvironment(),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    public async Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command)
    {
        resources.RecordHostCall();
        var exitCodes = await host.RunCommandsAsync([command], cancellationToken).ConfigureAwait(false);
        if (exitCodes.Count != 1)
        {
            throw new AwkRuntimeException(
                AwkMessages.FormatInvalidCommandResultCount(exitCodes.Count),
                AwkDiagnosticId.HostIoFailure);
        }

        return exitCodes[0];
    }

    public async Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode)
    {
        resources.RecordHostCall();
        return await host.OpenFileAsync(path, mode, cancellationToken).ConfigureAwait(false);
    }

    public async Task CloseDescriptorAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordHostCall();
        var closed = await host.CloseDescriptorAsync(descriptor, cancellationToken).ConfigureAwait(false);
        if (closed != AwkOperationStatus.Success)
        {
            throw new AwkRuntimeException(
                AwkMessages.FormatDescriptorCloseError(descriptor),
                AwkDiagnosticId.HostIoFailure);
        }
    }

    public async Task CloseDescriptorForCleanupAsync(AwkFileDescriptor descriptor)
    {
        counters.RecordHostCall();
        _ = await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
    }

    public AsyncLineReader CreateLineReader(AwkFileDescriptor descriptor, int maxLineCount) =>
        new(host, descriptor, cancellationToken, maxLineCount, counters, resources);

    private static void ThrowIfAppendFailed(AwkFileDescriptor descriptor, AwkExitCode status)
    {
        if (status.Succeeded)
            return;

        throw new AwkRuntimeException(
            AwkMessages.FormatAppendError(descriptor, status.Value),
            AwkDiagnosticId.HostIoFailure);
    }
}
