using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostStreamingTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_StandardOutputFlushesThroughHostBetweenStatements()
    {
        var observedBeforeAppend = new List<string>();
        var host = new TestOperationVisitor([]);
        host.BeforeAppend = () => observedBeforeAppend.Add(host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\"; print \"two\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(["", "one\n"], observedBeforeAppend);
        Assert.Equal("one\ntwo\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_OutputPipeStreamsThroughBoundedHostPipe()
    {
        var host = new BoundedPipeHost(capacityBytes: 4);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"a\" | \"consumer\"; print \"bc\" | \"consumer\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.True(host.CommandWasRunningDuringAppend);
        Assert.True(host.WriteDescriptorClosed);
        Assert.InRange(host.MaxBufferedBytes, 1, 4);
        var command = Assert.Single(host.Commands);
        Assert.Equal("consumer", command.CommandName);
        Assert.Equal(host.PipeReadDescriptor, command.StdIn);
    }

    private sealed class BoundedPipeHost(int capacityBytes) : IAwkHost
    {
        private readonly TaskCompletionSource<IReadOnlyList<AwkExitCode>> _commandCompletion = new();
        private int _bufferedBytes;

        public AwkFileDescriptor PipeReadDescriptor { get; } = new(100);

        public AwkFileDescriptor PipeWriteDescriptor { get; } = new(101);

        public List<AwkCommandInvocation> Commands { get; } = [];

        public bool CommandWasRunningDuringAppend { get; private set; }

        public bool WriteDescriptorClosed { get; private set; }

        public int MaxBufferedBytes { get; private set; }

        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
            IReadOnlyList<AwkCommandInvocation> commands,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Commands.AddRange(commands);
            return _commandCompletion.Task;
        }

        public Task<AwkExitCode> AppendAsync(
            AwkFileDescriptor fileDescriptor,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fileDescriptor != PipeWriteDescriptor)
                return Task.FromResult(AwkExitCode.Success);

            CommandWasRunningDuringAppend |= !_commandCompletion.Task.IsCompleted;
            if (WriteDescriptorClosed || content.Length > capacityBytes)
                return Task.FromResult(new AwkExitCode(1));

            _bufferedBytes += content.Length;
            MaxBufferedBytes = Math.Max(MaxBufferedBytes, _bufferedBytes);
            _bufferedBytes = 0;
            return Task.FromResult(AwkExitCode.Success);
        }

        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(AwkPipeMode.Pipeline, mode);
            return Task.FromResult(AwkPipeCreationResult.Created(
                new AwkPipeDescriptors(PipeReadDescriptor, PipeWriteDescriptor)));
        }

        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(
            AwkFileDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (descriptor == PipeWriteDescriptor)
            {
                WriteDescriptorClosed = true;
                _commandCompletion.TrySetResult([AwkExitCode.Success]);
            }

            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }

        public Task<AwkReadResult> ReadLinesAsync(
            AwkFileDescriptor fileDescriptor,
            int maxLineCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AwkReadResult.EndOfFile);
        }

        public Task<AwkOpenedFile> OpenFileAsync(
            AwkPath path,
            AwkFileOpenMode mode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

    }
}
