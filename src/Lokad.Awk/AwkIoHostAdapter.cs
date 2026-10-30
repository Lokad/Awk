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
    Func<AwkMemoryLease, IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
    AwkRuntimeResourceMonitor resources,
    AwkMemoryAccounting accounting,
    AwkPath? accessiblePathRoot = null,
    bool forwardCommandEnvironment = true,
    bool exposeHostErrorDetails = true,
    int maxHostErrorBytes = 4 << 10)
{
    private CancellationTokenSource? _commandCancellation;

    public AwkMemoryPeakAdmission AdmitPathRequest(string path, bool hasDisplay = false)
    {
        if (!accounting.IsEnabled || !AwkRawPath.IsValid(path)) return default;
        // Original display UTF-8 and the request; cwd/root are borrowed immutable values.
        resources.Checkpoint();
        var displayBytes = hasDisplay ? 0 : AwkUtf8Codec.GetEncodedByteCount(path);
        return accounting.ReserveAllocationPeak(AwkRuntimeMemory.CheckedAdd(128, displayBytes));
    }

    public AwkFileOperand PreparePath(string path)
    {
        resources.Checkpoint();
        return AwkPathResolution.PrepareRuntimeArgument(path, currentDirectory,
            accessiblePathRoot: accessiblePathRoot);
    }

    public async Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await AwaitHostAsync(host.AppendAsync(stdOut, content, cancellationToken)).ConfigureAwait(false);
        ThrowIfAppendFailed(stdOut, status);
    }

    public async Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await AwaitHostAsync(host.AppendAsync(stdErr, content, cancellationToken)).ConfigureAwait(false);
        ThrowIfAppendFailed(stdErr, status);
    }

    public async Task AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
            return;

        resources.RecordHostCall();
        resources.RecordOutputBytes(content.Length);
        var status = await AwaitHostAsync(host.AppendAsync(descriptor, content, cancellationToken)).ConfigureAwait(false);
        ThrowIfAppendFailed(descriptor, status);
    }

    public async Task<AwkPipeDescriptors> CreatePipeAsync()
    {
        resources.RecordHostCall();
        var result = await host.CreatePipeAsync(cancellationToken).ConfigureAwait(false);
        if (result.Success)
            return result.Descriptors;

        throw new AwkRuntimeException(
            AwkMessages.FormatPipeCreationError(result.Status, ResolveError(result.Error)),
            AwkDiagnosticId.HostIoFailure);
    }

    public AwkCommandInvocation CreateOutputPipeCommand(string commandName, AwkFileDescriptor stdIn,
        AwkMemoryLease memory) =>
        new(
            commandName,
            Array.Empty<string>(),
            SnapshotCommandEnvironment(commandName, memory),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    public AwkCommandInvocation CreateInputPipeCommand(string commandName, AwkFileDescriptor stdOut,
        AwkMemoryLease memory) =>
        new(
            commandName,
            Array.Empty<string>(),
            SnapshotCommandEnvironment(commandName, memory),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    private IReadOnlyList<AwkEnvironmentVariable> SnapshotCommandEnvironment(string commandName,
        AwkMemoryLease memory)
    {
        memory.ReserveAdditional(AwkRuntimeMemory.CheckedAdd(512,
            AwkRuntimeMemory.EstimateUtf16StringBytes(commandName.Length)));
        return forwardCommandEnvironment ? snapshotEnvironment(memory) : [];
    }

    public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command)
    {
        // Failure before handoff leaves both new pipe endpoints with the caller.
        resources.RecordHostCall();
        var commandToken = (_commandCancellation ??=
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)).Token;
        return AwaitHostAsync(host.RunCommandAsync(command, commandToken));
    }

    public void CancelCommandsForCleanup()
    {
        try { _commandCancellation?.Cancel(); }
        catch (AggregateException) { /* Still close descriptors and await users if a callback fails. */ }
    }

    public void DisposeCommandCancellation()
    {
        _commandCancellation?.Dispose();
        _commandCancellation = null;
    }

    public async Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode)
    {
        resources.RecordHostCall();
        var opened = await host.OpenFileAsync(path, mode, cancellationToken).ConfigureAwait(false);
        AwkHostPathPolicy.ThrowIfDenied(opened);
        return opened;
    }

    public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordHostCall();
        return await host.CloseDescriptorAsync(descriptor, cancellationToken)
            .ConfigureAwait(false);
    }

    public void ThrowIfCloseFailed(AwkFileDescriptor descriptor, AwkDescriptorCloseResult result, string? identity = null)
    {
        if (!result.Success)
            throw AwkHostBoundaryPolicy.CloseFailure(descriptor, result, exposeHostErrorDetails, maxHostErrorBytes,
                identity is null ? default : AwkUtf8Codec.Encode(identity));
    }

    public async Task CloseDescriptorForCleanupAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordCleanupHostCall();
        await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
    }

    public string ResolveError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, exposeHostErrorDetails, maxHostErrorBytes);

    private Task<T> AwaitHostAsync<T>(Task<T> operation) =>
        AwkHostBoundaryPolicy.AwaitAsync(operation, cancellationToken);

    private void ThrowIfAppendFailed(AwkFileDescriptor descriptor, AwkAppendResult result)
    {
        if (result.IsAccepted)
            return;

        var destination = descriptor == stdOut ? "standard output" :
            descriptor == stdErr ? "standard error" : "file descriptor " + descriptor.Id;
        var detail = result.Status == AwkAppendStatus.DownstreamClosed
            ? "downstream closed"
            : "write failed: " + ResolveError(result.Error);
        throw new AwkHostAppendException(descriptor, AwkMessages.FormatAppendError(destination, detail));
    }
}
