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
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\"; print \"two\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

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

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

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
        private readonly TaskCompletionSource<AwkExitCode> _commandCompletion = new();
        private int _bufferedBytes;

        public AwkFileDescriptor PipeReadDescriptor { get; } = new(100);

        public AwkFileDescriptor PipeWriteDescriptor { get; } = new(101);

        public List<AwkCommandInvocation> Commands { get; } = [];

        public bool CommandWasRunningDuringAppend { get; private set; }

        public bool WriteDescriptorClosed { get; private set; }

        public int MaxBufferedBytes { get; private set; }

        public Task<AwkExitCode> RunCommandAsync(
            AwkCommandInvocation command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Commands.Add(command);
            return _commandCompletion.Task;
        }

        public Task<AwkAppendResult> AppendAsync(
            AwkFileDescriptor fileDescriptor,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fileDescriptor != PipeWriteDescriptor)
                return Task.FromResult(AwkAppendResult.Accepted());

            CommandWasRunningDuringAppend |= !_commandCompletion.Task.IsCompleted;
            if (WriteDescriptorClosed || content.Length > capacityBytes)
                return Task.FromResult(AwkAppendResult.DownstreamClosed());

            _bufferedBytes += content.Length;
            MaxBufferedBytes = Math.Max(MaxBufferedBytes, _bufferedBytes);
            _bufferedBytes = 0;
            return Task.FromResult(AwkAppendResult.Accepted());
        }

        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                _commandCompletion.TrySetResult(AwkExitCode.Success);
            }

            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }

        public ValueTask<AwkReadBuffer> ReadAsync(
            AwkFileDescriptor fileDescriptor,
            int maxBytes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        }

        public Task<AwkOpenedFile> OpenFileAsync(
            AwkPathRequest path,
            AwkFileOpenMode mode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

    }
}
