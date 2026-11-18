namespace Lokad.Awk;

internal sealed class AwkIoHostAdapter(
    IAwkHost host,
    AwkPath currentDirectory,
    AwkFileDescriptor stdIn,
    AwkFileDescriptor stdOut,
    AwkFileDescriptor stdErr,
    CancellationToken cancellationToken,
    Func<AwkMemoryLease, IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
    AwkRuntimeOptions options,
    AwkMemoryAccounting accounting,
    AwkPath? accessiblePathRoot = null,
    bool forwardCommandEnvironment = true)
{
    private CancellationTokenSource? _commandCancellation;

    public AwkMemoryLease AdmitPathRequest(string path, bool hasDisplay = false)
    {
        if (!accounting.IsEnabled || !AwkPathRequest.IsValidPath(path)) return AwkMemoryLease.Disabled;
        // Original display UTF-8 and the request; cwd/root are borrowed immutable values.
        cancellationToken.ThrowIfCancellationRequested();
        var displayBytes = hasDisplay ? 0 : AwkUtf8Codec.GetEncodedByteCount(path);
        return accounting.ReserveTransient(AwkRuntimeMemory.EstimatePathRequestBytes(displayBytes));
    }

    public AwkFileOperand PreparePath(string path)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return AwkPathResolution.PrepareRuntimeArgument(path, currentDirectory,
            accessiblePathRoot: accessiblePathRoot);
    }

    public Task WriteStandardOutputAsync(ReadOnlyMemory<byte> content) => AppendAsync(stdOut, content);

    public Task WriteStandardErrorAsync(ReadOnlyMemory<byte> content) => AppendAsync(stdErr, content);

    public Task AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content)
    {
        try
        {
            if (content.Length == 0)
                return Task.CompletedTask;

            ObserveHostCall();
            options.Counters.Add(AwkCounter.OutputBytes, content.Length);
            var append = AwaitHostAsync(host.AppendAsync(descriptor, content, cancellationToken));
            if (!append.IsCompletedSuccessfully)
                return CompleteAppendAsync(descriptor, append);

            ThrowIfAppendFailed(descriptor, append.Result);
            return Task.CompletedTask;
        }
        catch (Exception error) { return AwkHostBoundaryPolicy.AwaitFailureAsync(error); }
    }

    private async Task CompleteAppendAsync(AwkFileDescriptor descriptor, Task<AwkAppendResult> append)
    {
        var status = await append.ConfigureAwait(false);
        ThrowIfAppendFailed(descriptor, status);
    }

    public async Task<AwkPipeDescriptors> CreatePipeAsync()
    {
        ObserveHostCall();
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
        ObserveHostCall();
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
        ObserveHostCall();
        var opened = await host.OpenFileAsync(path, mode, cancellationToken).ConfigureAwait(false);
        AwkHostPathPolicy.ThrowIfDenied(opened);
        return opened;
    }

    public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor)
    {
        ObserveHostCall();
        return await host.CloseDescriptorAsync(descriptor, cancellationToken)
            .ConfigureAwait(false);
    }

    public void ThrowIfCloseFailed(AwkFileDescriptor descriptor, AwkDescriptorCloseResult result, string? identity = null)
    {
        if (!result.Success)
            throw AwkHostBoundaryPolicy.CloseFailure(descriptor, result, options.ExposeHostErrorDetails, options.MaxHostErrorBytes,
                identity is null ? default : AwkUtf8Codec.Encode(identity));
    }

    public async Task CloseDescriptorForCleanupAsync(AwkFileDescriptor descriptor)
    {
        options.Counters.Add(AwkCounter.HostCalls);
        await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
    }

    public string ResolveError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, options.ExposeHostErrorDetails, options.MaxHostErrorBytes);

    private void ObserveHostCall()
    {
        cancellationToken.ThrowIfCancellationRequested();
        options.Counters.Add(AwkCounter.HostCalls);
    }

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
