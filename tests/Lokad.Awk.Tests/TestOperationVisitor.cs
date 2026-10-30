using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class TestOperationVisitor : IAwkHost
{
    private readonly Queue<TestHostOperation> _operations = [];
    private readonly List<AppendedContent> _appends = [];

    public TestOperationVisitor(IReadOnlyList<int> exitCodes)
    {
        ArgumentNullException.ThrowIfNull(exitCodes);

        foreach (var exitCode in exitCodes)
        {
            _operations.Enqueue(new TestHostOperation.RunCommand(new AwkExitCode(exitCode)));
        }
    }

    public List<string> CommandNames { get; } = [];

    public List<AwkCommandInvocation> CommandInvocations { get; } = [];

    public List<int> ReadByteCounts { get; } = [];

    public Action? BeforeAppend { get; set; }

    public Func<AwkFileDescriptor, ReadOnlyMemory<byte>, CancellationToken, Task<AwkAppendResult>>?
        AppendOverride { get; set; }

    public Action? AfterOpenFile { get; set; }

    public Action? AfterCreatePipe { get; set; }

    public Func<AwkCommandInvocation, CancellationToken, Task<AwkExitCode>>?
        RunCommandOverride { get; set; }

    public Func<AwkFileDescriptor, CancellationToken, Task<AwkDescriptorCloseResult>>?
        CloseDescriptorOverride { get; set; }

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

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.CreatePipe>("pipe descriptors");
        AfterCreatePipe?.Invoke();
        return Task.FromResult(operation.Result);
    }

    public Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(command);
        CommandInvocations.Add(command);
        CommandNames.Add(command.CommandName);

        if (RunCommandOverride is { } runCommandOverride)
            return runCommandOverride(command, cancellationToken);

        var operation = Dequeue<TestHostOperation.RunCommand>("command exit code");
        return Task.FromResult(operation.ExitCode);
    }

    public Task<AwkAppendResult> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        BeforeAppend?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();

        if (AppendOverride is { } append)
            return append(fileDescriptor, content, cancellationToken);

        var operation = Dequeue<TestHostOperation.Append>("append results");
        if (operation.Result.IsAccepted)
            _appends.Add(new AppendedContent(fileDescriptor, content.ToArray()));
        return Task.FromResult(operation.Result);
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.OpenFile>("open file descriptors");
        AfterOpenFile?.Invoke();
        return Task.FromResult(AwkOpenedFile.Opened(operation.Descriptor));
    }

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor.IsDevNull)
        {
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }

        if (CloseDescriptorOverride is { } closeDescriptorOverride)
            return closeDescriptorOverride(descriptor, cancellationToken);

        var operation = Dequeue<TestHostOperation.CloseDescriptor>("descriptor close results");
        return Task.FromResult(operation.Status);
    }

    public ValueTask<AwkReadBuffer> ReadAsync(
        AwkFileDescriptor fileDescriptor,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadByteCounts.Add(maxBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        if (fileDescriptor.IsDevNull)
        {
            return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        }

        var operation = Dequeue<TestHostOperation.ReadBytes>("read results");
        return ValueTask.FromResult(operation.Result);
    }

    public void EnqueueCommandExitCode(int exitCode) =>
        _operations.Enqueue(new TestHostOperation.RunCommand(new AwkExitCode(exitCode)));

    public void EnqueueAwkPipeDescriptors(AwkPipeDescriptors descriptors)
    {
        _operations.Enqueue(new TestHostOperation.CreatePipe(AwkPipeCreationResult.Created(descriptors)));
    }

    public void EnqueueAwkPipeCreationResult(AwkPipeCreationResult result)
    {
        _operations.Enqueue(new TestHostOperation.CreatePipe(result));
    }

    public void EnqueueAppendResult(AwkAppendResult result)
    {
        _operations.Enqueue(new TestHostOperation.Append(result));
    }

    public void EnqueueOpenAwkFileDescriptor(int descriptor)
    {
        _operations.Enqueue(new TestHostOperation.OpenFile(new AwkFileDescriptor(descriptor)));
    }

    public void EnqueueCloseDescriptorResult(AwkDescriptorCloseResult status)
    {
        _operations.Enqueue(new TestHostOperation.CloseDescriptor(status));
    }

    public void EnqueueReadBuffer(AwkReadBuffer result)
    {
        _operations.Enqueue(new TestHostOperation.ReadBytes(result));
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
        public sealed record RunCommand(AwkExitCode ExitCode) : TestHostOperation;

        public sealed record CreatePipe(AwkPipeCreationResult Result) : TestHostOperation;

        public sealed record Append(AwkAppendResult Result) : TestHostOperation;

        public sealed record OpenFile(AwkFileDescriptor Descriptor) : TestHostOperation;

        public sealed record CloseDescriptor(AwkDescriptorCloseResult Status) : TestHostOperation;

        public sealed record ReadBytes(AwkReadBuffer Result) : TestHostOperation;

    }

    private readonly record struct AppendedContent(
        AwkFileDescriptor Descriptor,
        byte[] Content);
}
