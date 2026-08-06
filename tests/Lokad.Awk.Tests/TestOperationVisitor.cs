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
            _operations.Enqueue(new TestHostOperation.RunCommands([new AwkExitCode(exitCode)]));
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

    public Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = Dequeue<TestHostOperation.CreatePipe>("pipe descriptors");
        return Task.FromResult(operation.Descriptors);
    }

    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken)
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

    public Task<AwkExitCode> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
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
            return Task.FromResult(AwkReadResult.EndOfFile);
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

        var typedExitCodes = new AwkExitCode[exitCodes.Length];
        for (var index = 0; index < exitCodes.Length; index++)
            typedExitCodes[index] = new AwkExitCode(exitCodes[index]);

        _operations.Enqueue(new TestHostOperation.RunCommands(typedExitCodes));
    }

    public void EnqueueAwkPipeDescriptors(AwkPipeDescriptors descriptors)
    {
        _operations.Enqueue(new TestHostOperation.CreatePipe(descriptors));
    }

    public void EnqueueAppendExitCode(int exitCode)
    {
        _operations.Enqueue(new TestHostOperation.Append(new AwkExitCode(exitCode)));
    }

    public void EnqueueOpenAwkFileDescriptor(int descriptor)
    {
        _operations.Enqueue(new TestHostOperation.OpenFile(new AwkFileDescriptor(descriptor)));
    }

    public void EnqueueCloseDescriptorResult(bool closed)
    {
        _operations.Enqueue(new TestHostOperation.CloseDescriptor(closed));
    }

    public void EnqueueAwkReadResult(AwkReadResult result)
    {
        _operations.Enqueue(new TestHostOperation.ReadLines(result));
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
        public sealed record RunCommands(IReadOnlyList<AwkExitCode> ExitCodes) : TestHostOperation;

        public sealed record CreatePipe(AwkPipeDescriptors Descriptors) : TestHostOperation;

        public sealed record Append(AwkExitCode ExitCode) : TestHostOperation;

        public sealed record OpenFile(AwkFileDescriptor Descriptor) : TestHostOperation;

        public sealed record CloseDescriptor(bool Closed) : TestHostOperation;

        public sealed record ReadLines(AwkReadResult Result) : TestHostOperation;

    }
}
