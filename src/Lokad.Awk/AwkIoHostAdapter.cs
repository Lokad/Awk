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
    Func<IReadOnlyList<AwkEnvironmentVariable>> snapshotEnvironment,
    AwkRuntimeCounters counters,
    AwkRuntimeResourceMonitor resources,
    AwkPath? accessiblePathRoot = null,
    TimeSpan? commandTimeout = null,
    TimeSpan? cleanupTimeout = null,
    bool forwardCommandEnvironment = true,
    TimeSpan? hostOperationTimeout = null,
    bool exposeHostErrorDetails = true,
    int maxHostErrorBytes = 4 << 10,
    AwkCleanupDeadline? cleanupDeadline = null)
{
    private readonly TimeSpan _commandTimeout = commandTimeout ?? TimeSpan.FromMinutes(5);
    private readonly TimeSpan _hostOperationTimeout = hostOperationTimeout ?? TimeSpan.FromMinutes(5);
    private readonly TimeSpan _closeOperationTimeout =
        Min(hostOperationTimeout ?? TimeSpan.FromMinutes(5), cleanupTimeout ?? TimeSpan.FromSeconds(30));
    private readonly AwkCleanupDeadline _cleanupDeadline =
        cleanupDeadline ?? new AwkCleanupDeadline(cleanupTimeout ?? TimeSpan.FromSeconds(30));

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

    public Task WriteStandardOutputAsync(string content)
    {
        if (content.Length == 0)
            return Task.CompletedTask;

        return WriteStandardOutputAsync(AwkUtf8Codec.Encode(content, counters));
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
        var result = await AwaitHostAsync(host.CreatePipeAsync(mode, cancellationToken)).ConfigureAwait(false);
        if (result.Success)
            return result.Descriptors;

        throw new AwkRuntimeException(
            AwkMessages.FormatPipeCreationError(result.Status, ResolveError(result.Error)),
            AwkDiagnosticId.HostIoFailure);
    }

    public AwkCommandInvocation CreateOutputPipeCommand(string commandName, AwkFileDescriptor stdIn) =>
        new(
            commandName,
            Array.Empty<string>(),
            SnapshotCommandEnvironment(),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    public AwkCommandInvocation CreateInputPipeCommand(string commandName, AwkFileDescriptor stdOut) =>
        new(
            commandName,
            Array.Empty<string>(),
            SnapshotCommandEnvironment(),
            currentDirectory,
            stdIn,
            stdOut,
            stdErr);

    private IReadOnlyList<AwkEnvironmentVariable> SnapshotCommandEnvironment() =>
        forwardCommandEnvironment ? snapshotEnvironment() : [];

    public async Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command)
    {
        resources.RecordHostCall();
        IReadOnlyList<AwkExitCode> exitCodes;
        try
        {
            exitCodes = await host.RunCommandsAsync([command], cancellationToken)
                .WaitAsync(_commandTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorCommandTimeout,
                AwkDiagnosticId.RuntimeResourceLimit);
        }
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
        AwkHostPathPolicy.ThrowIfDenied(path, accessiblePathRoot);
        resources.RecordHostCall();
        return await AwaitHostAsync(host.OpenFileAsync(path, mode, cancellationToken)).ConfigureAwait(false);
    }

    public async Task CloseDescriptorAsync(AwkFileDescriptor descriptor)
    {
        resources.RecordHostCall();
        var closed = await AwkHostBoundaryPolicy.AwaitAsync(
                host.CloseDescriptorAsync(descriptor, cancellationToken),
                _closeOperationTimeout,
                cancellationToken)
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
        await _cleanupDeadline.AwaitAsync(
            host.CloseDescriptorAsync(descriptor, CancellationToken.None)).ConfigureAwait(false);
    }

    public AsyncLineReader CreateLineReader(AwkFileDescriptor descriptor, int maxLineCount) =>
        new(
            host,
            descriptor,
            cancellationToken,
            maxLineCount,
            resources,
            countAsRuntimeInput: true,
            hostOperationTimeout: _hostOperationTimeout,
            exposeHostErrorDetails: exposeHostErrorDetails,
            maxHostErrorBytes: maxHostErrorBytes);

    public string ResolveError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, exposeHostErrorDetails, maxHostErrorBytes);

    private Task<T> AwaitHostAsync<T>(Task<T> operation) =>
        AwkHostBoundaryPolicy.AwaitAsync(operation, _hostOperationTimeout, cancellationToken);

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

    private static void ThrowIfAppendFailed(AwkFileDescriptor descriptor, AwkExitCode status)
    {
        if (status.Succeeded)
            return;

        throw new AwkRuntimeException(
            AwkMessages.FormatAppendError(descriptor, status.Value),
            AwkDiagnosticId.HostIoFailure);
    }
}
