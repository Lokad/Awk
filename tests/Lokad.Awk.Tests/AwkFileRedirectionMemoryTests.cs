using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkFileRedirectionMemoryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefusedSecondSpellingDoesNotOpenOrTruncateTheFirstStream(bool input)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new FileHost { OnOutput = () => budget.RefuseNextAdmissionMinimumGrowth = 1000 };
        var first = input ? "getline x<(\"./\" p)" : "print \"x\">(\"./\" p)";
        var second = input ? "getline x<p" : "print \"y\">p";
        var exit = await ExecuteAsync("BEGIN{p=sprintf(\"%04096d\",0);" + first +
            ";print \"opened\";" + second + ";print \"unreachable\"}", host, budget);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("opened\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, host.Opens);
        Assert.Equal(1, host.Closes);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefusedPathAdmissionDoesNotOpenOrTruncateTheTarget(bool input)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new FileHost { OnOutput = () => budget.RefuseNextAdmissionMinimumGrowth = 1000 };
        var operation = input ? "getline x < p" : "print \"x\" > p";
        var exit = await ExecuteAsync("BEGIN{p=sprintf(\"%04096d\",0);print \"before\";" + operation + "}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(0, host.Opens);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PathFundingSurvivesCancellationUntilThePendingCloseFinishes(bool input)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(256 << 10);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new FileHost
        {
            BeforeClose = async () => { entered.SetResult(); await completed.Task; }
        };
        var operation = input ? "getline x < p" : "print \"x\" > p";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{p=sprintf(\"%04096d\",0);" + operation + ";close(p);print \"after\"}"], []));
        var execution = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 8192);
            cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 8192);
        }
        finally { completed.TrySetResult(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal("", host.Error);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(1, host.Closes);
        Assert.Equal(0, budget.Reserved);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RetainedLongPathsCannotAccumulateOutsideTheAllowance(bool input)
    {
        var budget = new CooperativeTestBudget(256 << 10);
        var host = new FileHost();
        var operation = input ? "getline x < (p i)" : "print \"x\" > (p i)";
        var exit = await ExecuteAsync("BEGIN{p=sprintf(\"%04096d\",0);print \"ready\";" +
            "for(i=0;i<512;i++){" + operation + "};print \"done\"}", host, budget);

        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.InRange(host.Opens, 1, 511);
        Assert.Equal(host.Opens, host.Closes);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClosingPathsReturnsTheirFundingForTheNextStream(bool input)
    {
        var budget = new CooperativeTestBudget(128 << 10);
        var host = new FileHost();
        var operation = input ? "getline x < (p i)" : "print \"x\" > (p i)";
        var exit = await ExecuteAsync("BEGIN{p=sprintf(\"%04096d\",0);" +
            "for(i=0;i<512;i++){" + operation + ";close(p i)};print \"done\"}", host, budget);

        Assert.Equal(0, exit.Value);
        Assert.Equal("done\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(512, host.Opens);
        Assert.Equal(host.Opens, host.Closes);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task FlushRetainsTheExactLogicalNameAndReopensInAppendMode()
    {
        var budget = new CooperativeTestBudget(256 << 10);
        var snapshots = new List<long>();
        var modes = new List<AwkFileOpenMode>();
        var host = new FileHost
        {
            OnOutput = () => snapshots.Add(budget.Reserved),
            OnOpen = (_, mode) => modes.Add(mode)
        };
        var exit = await ExecuteAsync("""
            BEGIN {
                p=sprintf("%04096d",0)
                print "base"
                print "x" > ("./" p); print "open"
                fflush("./" p); print "flushed"
                print "y" > ("./" p)
                close("./" p); print "closed"
                print "z" > ("./" p)
            }
            """, host, budget);

        Assert.Equal(0, exit.Value);
        Assert.Equal("base\nopen\nflushed\nclosed\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(new[] { AwkFileOpenMode.WriteTruncate, AwkFileOpenMode.WriteAppend,
            AwkFileOpenMode.WriteTruncate }, modes);
        Assert.True(snapshots[1] >= snapshots[0] + 8192);
        Assert.True(snapshots[2] >= snapshots[0] + 8192);
        Assert.True(snapshots[3] <= snapshots[2] - 8192);
        Assert.Equal(host.Opens, host.Closes);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> ExecuteAsync(string script, IAwkHost host, IMemoryBudget budget)
    {
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        return tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None);
    }

    // No retained paths or file contents: the runtime must fund its own stream metadata.
    private sealed class FileHost : IAwkHost
    {
        private readonly StringBuilder _output = new();
        private readonly StringBuilder _error = new();
        public HashSet<AwkFileDescriptor> OpenDescriptors { get; } = [];
        public int Opens { get; private set; }
        public int Closes { get; private set; }
        public string Output => _output.ToString();
        public string Error => _error.ToString();
        public Action? OnOutput { get; init; }
        public Action<AwkPathRequest, AwkFileOpenMode>? OnOpen { get; init; }
        public Func<Task>? BeforeClose { get; init; }

        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                _output.Append(Encoding.UTF8.GetString(content.Span));
                OnOutput?.Invoke();
            }
            else if (descriptor == AwkFileDescriptor.StdErr) _error.Append(Encoding.UTF8.GetString(content.Span));
            else Assert.Contains(descriptor, OpenDescriptors);
            return Task.FromResult(AwkAppendResult.Accepted());
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            OnOpen?.Invoke(path, mode);
            var descriptor = new AwkFileDescriptor(10 + Opens++);
            Assert.True(OpenDescriptors.Add(descriptor));
            return Task.FromResult(AwkOpenedFile.Opened(descriptor, AwkRawPath.Parse(path.Path).ToAbsolute(path.CurrentDirectory)));
        }

        public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            if (BeforeClose is { } beforeClose) await beforeClose();
            Closes++;
            Assert.True(OpenDescriptors.Remove(descriptor));
            return AwkDescriptorCloseResult.Closed();
        }

        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Task.FromResult(AwkExitCode.Success);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) =>
            Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
