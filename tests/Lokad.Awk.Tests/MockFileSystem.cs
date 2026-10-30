using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class MockFileSystem : IAwkHost
{
    private readonly MockFileTree _files = new();
    private readonly MockFileDescriptorTable _descriptors = new();

    public MockFileSystem() => _files.AddDirectory("/dev");

    public List<AwkCommandInvocation> AwkCommandInvocations { get; } = [];

    public Action? AfterAppend { get; set; }

    public Action? AfterRead { get; set; }

    public Action? AfterCloseDescriptor { get; set; }

    public Func<AwkFileDescriptor, bool, bool>? CloseDescriptorResultOverride { get; set; }

    public Func<AwkFileDescriptor, int, AwkReadBuffer>? ReadOverride { get; set; }

    public Func<AwkFileDescriptor, int, CancellationToken, ValueTask<AwkReadBuffer>>?
        ReadAsyncOverride { get; set; }

    public Exception? OpenFileException { get; set; }

    public int OpenFileCount => _descriptors.OpenFileCount;

    public int ReadOpenCount { get; private set; }

    public void AddDirectory(string path) =>
        AddDirectory(path, AwkFileTreePermission.ReadWrite);

    public void AddDirectory(string path, AwkFileTreePermission permission) =>
        _files.AddDirectory(path, permission);

    public void AddFile(string path, string content) =>
        AddFile(path, content, AwkFileTreePermission.ReadWrite);

    public void AddFile(
        string path,
        string content,
        AwkFileTreePermission permission) =>
        _files.AddFile(path, content, permission);

    public void AddFile(string path, byte[] content) =>
        AddFile(path, content, AwkFileTreePermission.ReadWrite);

    public void AddFile(
        string path,
        byte[] content,
        AwkFileTreePermission permission) =>
        _files.AddFile(path, content, permission);

    public void AddReadOpenError(string path, string message) =>
        _files.AddReadOpenError(path, message);

    public bool FileExists(string path) =>
        _files.FileExists(path);

    public byte[] GetOutputBytes(AwkFileDescriptor descriptor) =>
        _descriptors.GetOutputBytes(descriptor);

    public byte[] ReadFileBytes(string path) =>
        _files.ReadFileBytes(path);

    public void SetStandardInput(string content) =>
        SetStandardInputBytes(Encoding.UTF8.GetBytes(content));

    public void SetStandardInputBytes(byte[] content) =>
        _descriptors.SetStandardInput(content);

    public string GetOutput(AwkFileDescriptor descriptor) =>
        _descriptors.GetOutput(descriptor);

    public string ReadFile(string path) =>
        _files.ReadFile(path);

    public Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        AwkCommandInvocations.Add(command);
        return Task.FromResult(AwkExitCode.Success);
    }

    public Task<AwkAppendResult> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fileDescriptor.IsDevNull)
        {
            AfterAppend?.Invoke();
            return Task.FromResult(AwkAppendResult.Accepted());
        }

        var result = _descriptors.Append(fileDescriptor, content);
        AfterAppend?.Invoke();
        return Task.FromResult(result == 0 ? AwkAppendResult.Accepted() :
            AwkAppendResult.Failure(new(AwkOperationErrorKind.IoFailure, "Descriptor is not writable.")));
    }

    public Task<AwkOpenedFile> OpenFileAsync(
        AwkPathRequest request,
        AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (OpenFileException is { } exception)
            throw exception;

        if (!_files.TryResolvePath(request, out var path, out var pathError))
            return Task.FromResult(pathError.Kind == AwkOperationErrorKind.NotFound
                ? AwkOpenedFile.MissingFile() : AwkOpenedFile.Failure(pathError));

        if (path.Equals(AwkFileDescriptor.DevNullPath))
            return Task.FromResult(AwkOpenedFile.Opened(AwkFileDescriptor.DevNull, path));

        if (mode == AwkFileOpenMode.Read)
        {
            ReadOpenCount++;
            return Task.FromResult(OpenForRead(path));
        }

        if (mode is AwkFileOpenMode.WriteTruncate or AwkFileOpenMode.WriteAppend)
            return Task.FromResult(OpenForWrite(path, mode));

        return Task.FromResult(AwkOpenedFile.MissingFile());

        AwkOpenedFile OpenForRead(AwkPath path)
        {
            return _files.OpenForRead(path) switch
            {
                MockReadOpenResult.Opened opened =>
                    AwkOpenedFile.Opened(_descriptors.OpenRead(opened.Content), path),
                MockReadOpenResult.Failure failure => AwkOpenedFile.Failure(failure.Error),
                MockReadOpenResult.MissingFile => AwkOpenedFile.MissingFile(),
                _ => AwkOpenedFile.MissingFile()
            };
        }

        AwkOpenedFile OpenForWrite(AwkPath path, AwkFileOpenMode mode)
        {
            return _files.OpenForWrite(path, mode) switch
            {
                MockWriteOpenResult.Opened opened =>
                    AwkOpenedFile.Opened(_descriptors.OpenWrite(opened.InitialContent, opened.Commit), path),
                MockWriteOpenResult.Failure failure => AwkOpenedFile.Failure(failure.Error),
                _ => AwkOpenedFile.MissingFile()
            };
        }
    }

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor.IsDevNull)
            return Task.FromResult(AwkDescriptorCloseResult.Closed());

        var closed = _descriptors.Close(descriptor);
        AfterCloseDescriptor?.Invoke();
        var closeDescriptorResultOverride = CloseDescriptorResultOverride;
        var succeeded = closeDescriptorResultOverride == null
            ? closed
            : closeDescriptorResultOverride(descriptor, closed);
        return Task.FromResult(
            succeeded ? AwkDescriptorCloseResult.Closed() : AwkDescriptorCloseResult.Failure(default, descriptorRetired: closed));
    }

    public ValueTask<AwkReadBuffer> ReadAsync(
        AwkFileDescriptor fileDescriptor,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        if (ReadAsyncOverride is { } readAsyncOverride)
            return readAsyncOverride(fileDescriptor, maxBytes, cancellationToken);

        var readOverride = ReadOverride;
        var result = readOverride == null
            ? _descriptors.ReadBytes(fileDescriptor, maxBytes)
            : new MockDescriptorReadResult(readOverride(fileDescriptor, maxBytes), true);
        if (result.NotifyAfterRead)
            AfterRead?.Invoke();

        return ValueTask.FromResult(result.Result);
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AwkPipeCreationResult.NotSupported(
            new AwkOperationError(AwkOperationErrorKind.IoFailure, "MockFileSystem does not create pipes.")));
    }
}
