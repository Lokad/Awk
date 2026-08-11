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

    public List<AwkCommandInvocation> AwkCommandInvocations { get; } = [];

    public Action? AfterAppend { get; set; }

    public Action? AfterReadLines { get; set; }

    public Action? AfterCloseDescriptor { get; set; }

    public Func<AwkFileDescriptor, bool, bool>? CloseDescriptorResultOverride { get; set; }

    public Func<AwkFileDescriptor, int, AwkReadResult>? ReadLinesOverride { get; set; }

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

    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commands);
        cancellationToken.ThrowIfCancellationRequested();

        AwkCommandInvocations.AddRange(commands);
        return Task.FromResult<IReadOnlyList<AwkExitCode>>([AwkExitCode.Success]);
    }

    public Task<AwkExitCode> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fileDescriptor.IsDevNull)
        {
            AfterAppend?.Invoke();
            return Task.FromResult(AwkExitCode.Success);
        }

        var result = _descriptors.Append(fileDescriptor, content);
        AfterAppend?.Invoke();
        return Task.FromResult(new AwkExitCode(result));
    }

    public Task<AwkOpenedFile> OpenFileAsync(
        AwkPath path,
        AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (path.Equals(AwkFileDescriptor.DevNullPath))
            return Task.FromResult(AwkOpenedFile.Opened(AwkFileDescriptor.DevNull));

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
                    AwkOpenedFile.Opened(_descriptors.OpenRead(opened.Content)),
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
                    AwkOpenedFile.Opened(_descriptors.OpenWrite(opened.InitialContent, opened.Commit)),
                MockWriteOpenResult.Failure failure => AwkOpenedFile.Failure(failure.Error),
                _ => AwkOpenedFile.MissingFile()
            };
        }
    }

    public Task<AwkOperationStatus> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor.IsDevNull)
            return Task.FromResult(AwkOperationStatus.Success);

        var closed = _descriptors.Close(descriptor);
        AfterCloseDescriptor?.Invoke();
        var closeDescriptorResultOverride = CloseDescriptorResultOverride;
        var succeeded = closeDescriptorResultOverride == null
            ? closed
            : closeDescriptorResultOverride(descriptor, closed);
        return Task.FromResult(
            succeeded ? AwkOperationStatus.Success : AwkOperationStatus.Failure);
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int maxLineCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLineCount);

        var readLinesOverride = ReadLinesOverride;
        var result = readLinesOverride == null
            ? _descriptors.ReadLines(fileDescriptor, maxLineCount)
            : new MockDescriptorReadResult(readLinesOverride(fileDescriptor, maxLineCount), true);
        if (result.NotifyAfterRead)
            AfterReadLines?.Invoke();

        return Task.FromResult(result.Result);
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AwkPipeCreationResult.NotSupported(
            new AwkOperationError("MockFileSystem does not create pipes.")));
    }
}
