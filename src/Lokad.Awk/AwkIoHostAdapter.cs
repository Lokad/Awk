using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    public AwkMemoryPeakAdmission AdmitPathResolution(string path)
    {
        if (!accounting.IsEnabled || !AwkRawPath.IsValid(path)) return default;
        var characters = AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(path.Length, currentDirectory.Path.Length), 1);
        // Combined/canonical UTF-16 strings, display UTF-8 and segment-list growth.
        var separators = 0;
        foreach (var character in path)
            if (character == '/') separators++;
        foreach (var character in currentDirectory.Path)
            if (character == '/') separators++;
        return accounting.ReserveAllocationPeak(AwkRuntimeMemory.CheckedAdd(128,
            AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedMultiply(characters, 8),
                AwkRuntimeMemory.CheckedMultiply(separators, 32))));
    }

    // A lookup only inspects already admitted streams; it never opens a host path.
    public AwkPath ResolveExistingPath(string path) =>
        AwkPathResolution.ResolveRuntimeArgument(path, currentDirectory).Absolute;

    public bool TryResolveExistingPath(string path, [NotNullWhen(true)] out AwkPath? resolved)
    {
        if (AwkRawPath.TryParse(path, out var raw))
        {
            try
            {
                resolved = raw.ToAbsolute(currentDirectory);
                return true;
            }
            catch (AwkPathException)
            {
                // An admitted pipe command need not be a resolvable filesystem path.
            }
        }
        resolved = null;
        return false;
    }

    public AwkResolvedPath ResolvePath(string path)
    {
        var resolved = AwkPathResolution.ResolveRuntimeArgument(path, currentDirectory);
        AwkHostPathPolicy.ThrowIfDenied(resolved.Absolute, accessiblePathRoot);
        return resolved;
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

    public async Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode)
    {
        resources.RecordHostCall();
        var result = await host.CreatePipeAsync(mode, cancellationToken).ConfigureAwait(false);
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
        return CompleteAsync(host.RunCommandsAsync([command], commandToken));

        async Task<AwkExitCode> CompleteAsync(Task<IReadOnlyList<AwkExitCode>> operation)
        {
            var exitCodes = await AwaitHostAsync(operation).ConfigureAwait(false);
            if (exitCodes.Count != 1)
                throw new AwkRuntimeException(AwkMessages.FormatInvalidCommandResultCount(exitCodes.Count),
                    AwkDiagnosticId.HostIoFailure);
            return exitCodes[0];
        }
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

    public async Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode)
    {
        AwkHostPathPolicy.ThrowIfDenied(path, accessiblePathRoot);
        resources.RecordHostCall();
        return await host.OpenFileAsync(path, mode, cancellationToken).ConfigureAwait(false);
    }

    public async Task CloseDescriptorAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordHostCall();
        var closed = await host.CloseDescriptorAsync(descriptor, cancellationToken)
            .ConfigureAwait(false);
        if (closed != AwkOperationStatus.Success)
        {
            throw new AwkRuntimeException(
                AwkMessages.FormatDescriptorCloseError(descriptor),
                AwkDiagnosticId.HostIoFailure);
        }
    }

    public async Task CloseDescriptorForCleanupAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordCleanupHostCall();
        await host.CloseDescriptorAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
    }

    public AsyncLineReader CreateLineReader(AwkFileDescriptor descriptor, int maxLineCount) =>
        new(
            host,
            descriptor,
            cancellationToken,
            maxLineCount,
            resources,
            exposeHostErrorDetails: exposeHostErrorDetails,
            maxHostErrorBytes: maxHostErrorBytes);

    public string ResolveError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, exposeHostErrorDetails, maxHostErrorBytes);

    private Task<T> AwaitHostAsync<T>(Task<T> operation) =>
        AwkHostBoundaryPolicy.AwaitAsync(operation, cancellationToken);

    private static void ThrowIfAppendFailed(AwkFileDescriptor descriptor, AwkExitCode status)
    {
        if (status.Succeeded)
            return;

        throw new AwkRuntimeException(
            AwkMessages.FormatAppendError(descriptor, status.Value),
            AwkDiagnosticId.HostIoFailure);
    }
}
