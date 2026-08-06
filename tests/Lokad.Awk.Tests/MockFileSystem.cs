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

    public Func<AwkFileDescriptor, int, AwkReadResult>? ReadLinesOverride { get; set; }

    public int OpenFileCount => _descriptors.OpenFileCount;

    public void AddDirectory(
        string path,
        DateTimeOffset? modifiedAt = null,
        AwkFileTreePermission permission = AwkFileTreePermission.ReadWrite) =>
        _files.AddDirectory(path, modifiedAt, permission);

    public void AddFile(
        string path,
        string content,
        DateTimeOffset? modifiedAt = null,
        AwkFileTreePermission permission = AwkFileTreePermission.ReadWrite) =>
        _files.AddFile(path, content, modifiedAt, permission);

    public void AddFile(
        string path,
        byte[] content,
        DateTimeOffset? modifiedAt = null,
        AwkFileTreePermission permission = AwkFileTreePermission.ReadWrite) =>
        _files.AddFile(path, content, modifiedAt, permission);

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

    public Task<IReadOnlyList<int>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commands);
        cancellationToken.ThrowIfCancellationRequested();

        AwkCommandInvocations.AddRange(commands);
        return Task.FromResult<IReadOnlyList<int>>([0]);
    }

    public Task<int> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fileDescriptor.IsDevNull)
        {
            AfterAppend?.Invoke();
            return Task.FromResult(0);
        }

        var result = _descriptors.Append(fileDescriptor, content);
        AfterAppend?.Invoke();
        return Task.FromResult(result);
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
            return Task.FromResult(OpenForRead(path));

        if (mode is AwkFileOpenMode.WriteTruncate or AwkFileOpenMode.WriteAppend)
            return Task.FromResult(OpenForWrite(path, mode));

        return Task.FromResult(AwkOpenedFile.MissingFile());
    }

    public Task<AwkDuplicateDescriptorResult> DuplicateDescriptorAsync(
        AwkFileDescriptor sourceDescriptor,
        AwkFileDescriptor targetDescriptor,
        CancellationToken cancellationToken)
    {
        if (sourceDescriptor.IsDevNull)
            return Task.FromResult(AwkDuplicateDescriptorResult.Duplicated(AwkFileDescriptor.DevNull));

        throw new NotSupportedException("MockFileSystem does not duplicate descriptors.");
    }

    public Task<bool> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor.IsDevNull)
            return Task.FromResult(true);

        var closed = _descriptors.Close(descriptor);
        AfterCloseDescriptor?.Invoke();
        return Task.FromResult(closed);
    }

    public Task<IReadOnlyList<AwkDirectoryEntry>> ListDirectoryAsync(
        AwkPath path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.ListDirectory(path));
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int maxLineCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (maxLineCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLineCount));

        var readLinesOverride = ReadLinesOverride;
        var result = readLinesOverride == null
            ? _descriptors.ReadLines(fileDescriptor, maxLineCount)
            : new MockDescriptorReadResult(readLinesOverride(fileDescriptor, maxLineCount), true);
        if (result.NotifyAfterRead)
            AfterReadLines?.Invoke();

        return Task.FromResult(result.Result);
    }

    public Task<IReadOnlyList<bool>> FileTestsAsync(
        IReadOnlyList<AwkFileTestRequest> tests,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tests);

        var results = new bool[tests.Count];
        for (var index = 0; index < tests.Count; index++)
        {
            results[index] = _files.EvaluateTest(tests[index]);
        }

        return Task.FromResult<IReadOnlyList<bool>>(results);
    }

    public Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("MockFileSystem does not create pipes.");
    }

    public Task<AwkFileMetadata> GetFileMetadataAsync(AwkPath path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.GetMetadata(path));
    }

    public Task<AwkOperationResult> CreateDirectoryAsync(AwkPath path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.CreateDirectory(path));
    }

    public Task<AwkOperationResult> DeleteDirectoryAsync(AwkPath path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.DeleteDirectory(path));
    }

    public Task<AwkOperationResult> DeleteFileAsync(AwkPath path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.DeleteFile(path));
    }

    public Task<AwkOperationResult> CopyFileAsync(
        AwkPath sourcePath,
        AwkPath destinationPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_files.CopyFile(sourcePath, destinationPath));
    }

    private AwkOpenedFile OpenForRead(AwkPath path)
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

    private AwkOpenedFile OpenForWrite(AwkPath path, AwkFileOpenMode mode)
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
