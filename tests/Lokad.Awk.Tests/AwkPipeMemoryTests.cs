using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkPipeMemoryTests
{
    [Theory]
    [InlineData("{print $0|\"sink\"}END{close(\"sink\")}", false)]
    [InlineData("BEGIN{print \"start\"|\"sink\"}{print $0}END{close(\"sink\")}", false)]
    [InlineData("BEGIN{while((\"source\"|getline)>0)print;close(\"source\")}", true)]
    public async Task LongLivedCommandDoesNotAccumulateConsumedRecords(string script, bool inputPipe)
    {
        var shortPeak = await RunAsync(64);
        var longPeak = await RunAsync(4096);
        Assert.True(longPeak <= shortPeak + 4096, $"Peaks grew from {shortPeak} to {longPeak}.");

        async Task<long> RunAsync(int rows)
        {
            var budget = new CooperativeTestBudget(1 << 20);
            var host = new StreamingHost(rows, inputPipe);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var exit = await ExecuteAsync(script, host, budget, cancellation.Token);
            Assert.True(exit.Value == 0, host.Error);
            Assert.Equal(rows, host.RecordsRead);
            Assert.Equal(rows + (script.Contains("start", StringComparison.Ordinal) ? 1 : 0), host.Writes);
            Assert.Equal("", host.Error);
            Assert.Equal(0, host.OpenDescriptors);
            Assert.Equal(0, budget.Reserved);
            return budget.Peak;
        }
    }

    [Fact]
    public async Task ClosingCommandsReturnsTheirSnapshotFundingForReuse()
    {
        var budget = new CooperativeTestBudget(64 << 10);
        var host = new StreamingHost(0, false);
        var exit = await ExecuteAsync("BEGIN{for(i=0;i<512;i++){print \"x\"|\"sink\";close(\"sink\")}}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal(512, host.Writes);
        Assert.Equal("", host.Error);
        Assert.Equal(0, host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task CommandSnapshotStaysFundedAfterParentEnvironmentMutation()
    {
        var budget = new CooperativeTestBudget(2 << 20);
        var host = new StreamingHost(0, false);
        var checkedSnapshot = false;
        host.OnAppend = descriptor =>
        {
            if (descriptor != AwkFileDescriptor.StdOut) return;
            var command = Assert.IsType<AwkCommandInvocation>(host.Command);
            Assert.Equal(80_000, Assert.Single(command.Environment, item => item.Name == "TOKEN").Value.Length);
            Assert.True(budget.Reserved >= 160_000);
            checkedSnapshot = true;
        };
        var exit = await ExecuteAsync("BEGIN{ENVIRON[\"TOKEN\"]=sprintf(\"%80000s\",\"x\");" +
            "print \"open\"|\"child\";delete ENVIRON[\"TOKEN\"];print \"check\";close(\"child\")}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.True(checkedSnapshot);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedCommandSnapshotDoesNotStartCommandAndCleansBothEndpoints()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new StreamingHost(0, false)
        {
            OnCreate = () => budget.RefuseNextAdmissionMinimumGrowth = 1
        };
        var exit = await ExecuteAsync("BEGIN{print \"x\"|\"sink\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Null(host.Command);
        Assert.Equal(0, host.Writes);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(0, host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> ExecuteAsync(string script, IAwkHost host, IMemoryBudget budget,
        CancellationToken token = default)
    {
        var invocation = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []);
        Assert.True(Awk.TryParse(invocation, out var awk));
        return awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), token);
    }

    // Consumes writes immediately; input is one shared record, not a growing test-host buffer.
    private sealed class StreamingHost(int rows, bool inputPipe) : IAwkHost
    {
        private static readonly byte[] Line = Encoding.UTF8.GetBytes(new string('x', 1023) + "\n");
        private TaskCompletionSource<AwkExitCode> _command = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly StringBuilder _error = new();
        private readonly HashSet<AwkFileDescriptor> _open = [];
        public int RecordsRead { get; private set; }
        public int Writes { get; private set; }
        public string Error => _error.ToString();
        public int OpenDescriptors => _open.Count;
        public AwkCommandInvocation? Command { get; private set; }
        public Action<AwkFileDescriptor>? OnAppend { get; set; }
        public Action? OnCreate { get; init; }

        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (inputPipe && descriptor == AwkFileDescriptor.StdIn)
                return Task.FromResult(AwkReadResult.EndOfFile);
            Assert.Equal(inputPipe ? new AwkFileDescriptor(10) : AwkFileDescriptor.StdIn, descriptor);
            if (RecordsRead == rows) return Task.FromResult(AwkReadResult.EndOfFile);
            RecordsRead++;
            return Task.FromResult(AwkReadResult.FromContent(Line, [],
                RecordsRead == rows ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData));
        }

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (descriptor == AwkFileDescriptor.StdErr) _error.Append(Encoding.UTF8.GetString(content.Span));
            else { OnAppend?.Invoke(descriptor); Writes++; }
            return Task.FromResult(AwkExitCode.Success);
        }

        public async Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands,
            CancellationToken token)
        {
            Command = Assert.Single(commands);
            return [await _command.Task.WaitAsync(token)];
        }

        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token)
        {
            _command = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var read = new AwkFileDescriptor(10);
            var write = new AwkFileDescriptor(11);
            _open.Add(read);
            _open.Add(write);
            OnCreate?.Invoke();
            return Task.FromResult(AwkPipeCreationResult.Created(new(read, write)));
        }

        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            _open.Remove(descriptor);
            if (descriptor == new AwkFileDescriptor(11))
            {
                _open.Remove(new(10));
                _command.TrySetResult(AwkExitCode.Success);
            }
            return Task.FromResult(AwkOperationStatus.Success);
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) =>
            Task.FromResult(AwkOpenedFile.Failure(new AwkOperationError("No files in this host.")));
    }
}
