using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class TestOperationVisitor : IAwkHost
{
    private readonly Queue<TestHostOperation> _operations = [];
    private readonly List<(AwkFileDescriptor Descriptor, byte[] Content)> _appends = [];

    public TestOperationVisitor(IReadOnlyList<int> exitCodes)
    {
        if (exitCodes == null)
        {
            throw new ArgumentNullException(nameof(exitCodes));
        }

        foreach (var exitCode in exitCodes)
        {
            _operations.Enqueue(new TestHostOperation.RunCommands([exitCode]));
        }
    }

    public List<string> CommandNames { get; } = [];

    public List<AwkCommandInvocation> CommandInvocations { get; } = [];

    public List<int> ReadLineBatchLineCounts { get; } = [];

    public Action? BeforeAppend { get; set; }

    public string GetAppendedOutput(AwkFileDescriptor descriptor)
    {
        var builder = new StringBuilder();
        foreach (var append in _appends)
        {
            if (append.Descriptor == descriptor)
                builder.Append(Encoding.UTF8.GetString(append.Content));
        }

        return builder.ToString();
    }

    public Task<IReadOnlyList<bool>> FileTestsAsync(IReadOnlyList<AwkFileTestRequest> tests, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.FileTests>("file test results");
        return Task.FromResult(operation.Results);
    }

    public Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.CreatePipe>("pipe descriptors");
        return Task.FromResult(operation.Descriptors);
    }

    public Task<AwkFileMetadata> GetFileMetadataAsync(AwkPath path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("TestOperationVisitor does not provide file metadata.");
    }

    public Task<AwkOperationResult> CreateDirectoryAsync(AwkPath path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("TestOperationVisitor does not create directories.");
    }

    public Task<AwkOperationResult> DeleteDirectoryAsync(AwkPath path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("TestOperationVisitor does not delete directories.");
    }

    public Task<AwkOperationResult> DeleteFileAsync(AwkPath path, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("TestOperationVisitor does not delete files.");
    }

    public Task<AwkOperationResult> CopyFileAsync(
        AwkPath sourcePath,
        AwkPath destinationPath,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("TestOperationVisitor does not copy files.");
    }

    public Task<IReadOnlyList<int>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        foreach (var command in commands)
        {
            CommandInvocations.Add(command);
            CommandNames.Add(command.CommandName);
        }

        var operation = Dequeue<TestHostOperation.RunCommands>("command exit codes");
        return Task.FromResult(operation.ExitCodes);
    }

    public Task<int> AppendAsync(AwkFileDescriptor fileDescriptor, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        BeforeAppend?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();

        var operation = Dequeue<TestHostOperation.Append>("append exit codes");
        _appends.Add((fileDescriptor, content.ToArray()));
        return Task.FromResult(operation.ExitCode);
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.OpenFile>("open file descriptors");
        return Task.FromResult(AwkOpenedFile.Opened(operation.Descriptor));
    }

    public Task<AwkDuplicateDescriptorResult> DuplicateDescriptorAsync(
        AwkFileDescriptor sourceDescriptor,
        AwkFileDescriptor targetDescriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (sourceDescriptor.IsDevNull)
        {
            return Task.FromResult(AwkDuplicateDescriptorResult.Duplicated(AwkFileDescriptor.DevNull));
        }

        var operation = Dequeue<TestHostOperation.DuplicateDescriptor>("duplicated descriptors");
        return Task.FromResult(AwkDuplicateDescriptorResult.Duplicated(operation.Descriptor));
    }

    public Task<bool> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor.IsDevNull)
        {
            return Task.FromResult(true);
        }

        var operation = Dequeue<TestHostOperation.CloseDescriptor>("descriptor close results");
        return Task.FromResult(operation.Closed);
    }

    public Task<IReadOnlyList<AwkDirectoryEntry>> ListDirectoryAsync(AwkPath path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.ListDirectory>("directory entries");
        return Task.FromResult(operation.Entries);
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int maxLineCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadLineBatchLineCounts.Add(maxLineCount);
        if (maxLineCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLineCount));
        }

        if (fileDescriptor.IsDevNull)
        {
            return Task.FromResult(new AwkReadResult(ReadOnlyMemory<byte>.Empty, Array.Empty<int>(), 1, true));
        }

        var operation = Dequeue<TestHostOperation.ReadLines>("read results");
        return Task.FromResult(operation.Result);
    }

    public void EnqueueCommandExitCodes(int[] exitCodes)
    {
        if (exitCodes == null)
        {
            throw new ArgumentNullException(nameof(exitCodes));
        }

        _operations.Enqueue(new TestHostOperation.RunCommands(exitCodes));
    }

    public void EnqueueAwkPipeDescriptors(AwkPipeDescriptors descriptors)
    {
        _operations.Enqueue(new TestHostOperation.CreatePipe(descriptors));
    }

    public void EnqueueAppendExitCode(int exitCode)
    {
        _operations.Enqueue(new TestHostOperation.Append(exitCode));
    }

    public void EnqueueOpenAwkFileDescriptor(int descriptor)
    {
        _operations.Enqueue(new TestHostOperation.OpenFile(new AwkFileDescriptor(descriptor)));
    }

    public void EnqueueDuplicateDescriptor(int descriptor)
    {
        _operations.Enqueue(new TestHostOperation.DuplicateDescriptor(new AwkFileDescriptor(descriptor)));
    }

    public void EnqueueCloseDescriptorResult(bool closed)
    {
        _operations.Enqueue(new TestHostOperation.CloseDescriptor(closed));
    }

    public void EnqueueAwkReadResult(AwkReadResult result)
    {
        _operations.Enqueue(new TestHostOperation.ReadLines(result));
    }

    public void EnqueueDirectoryEntries(AwkDirectoryEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        _operations.Enqueue(new TestHostOperation.ListDirectory(entries));
    }

    public void EnqueueFileTestResults(bool[] results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        _operations.Enqueue(new TestHostOperation.FileTests(results));
    }

    private T Dequeue<T>(string description)
        where T : TestHostOperation
    {
        if (_operations.Count == 0)
        {
            throw new InvalidOperationException("No " + description + " are available for this operation.");
        }

        var operation = _operations.Dequeue();
        if (operation is T typed)
            return typed;

        throw new InvalidOperationException(
            "Expected " + typeof(T).Name + " operation but found " + operation.GetType().Name + ".");
    }

    private abstract record TestHostOperation
    {
        public sealed record RunCommands(IReadOnlyList<int> ExitCodes) : TestHostOperation;

        public sealed record CreatePipe(AwkPipeDescriptors Descriptors) : TestHostOperation;

        public sealed record Append(int ExitCode) : TestHostOperation;

        public sealed record OpenFile(AwkFileDescriptor Descriptor) : TestHostOperation;

        public sealed record DuplicateDescriptor(AwkFileDescriptor Descriptor) : TestHostOperation;

        public sealed record CloseDescriptor(bool Closed) : TestHostOperation;

        public sealed record ReadLines(AwkReadResult Result) : TestHostOperation;

        public sealed record ListDirectory(IReadOnlyList<AwkDirectoryEntry> Entries) : TestHostOperation;

        public sealed record FileTests(IReadOnlyList<bool> Results) : TestHostOperation;
    }
}
