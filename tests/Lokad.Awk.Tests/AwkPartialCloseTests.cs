using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkPartialCloseTests
{
    [Theory]
    [InlineData("output", false)] [InlineData("input", false)] [InlineData("pipe", false)]
    [InlineData("output", true)] [InlineData("input", true)] [InlineData("pipe", true)]
    [InlineData("output-alias", false)] [InlineData("input-alias", false)]
    [InlineData("output-alias", true)] [InlineData("input-alias", true)]
    public async Task PartialFailureKeepsOnlyUnclosedEndpointsForCleanup(string kind, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new CloseHost { Cancellation = cancel ? cancellation : null, SuspendCloses = true };
        var source = kind switch
        {
            "output" => "BEGIN{print 1>\"one\";print 2>\"two\";print 3>\"three\"}",
            "input" => "BEGIN{getline x<\"one\";getline y<\"two\";getline z<\"three\"}",
            "output-alias" => "BEGIN{print 1>\"one\";print 2>\"./one\";print 3>\"d/../one\"}",
            "input-alias" => "BEGIN{getline x<\"one\";getline y<\"./one\";getline z<\"d/../one\"}",
            _ => "BEGIN{\"source\"|getline x}"
        };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []));
        var execution = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token);
        if (cancel)
        {
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal("", host.Error);
        }
        else
        {
            Assert.Equal(2, (await execution).Value);
            var name = kind == "pipe" ? "source" : kind.EndsWith("-alias", StringComparison.Ordinal) ? "./one" : "two";
            Assert.Equal("awk: " + name + ": close failed\n", host.Error);
        }
        Assert.Equal(kind == "pipe" ? new[] { 10, 11, 11 } : new[] { 10, 11, 11, 12 }, host.Closes);
        Assert.All(host.CloseTokens.Skip(2), token => Assert.False(token.CanBeCanceled));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommandFailureAfterSuccessfulPipeCloseDoesNotReplayClosedEndpoints(bool input)
    {
        var host = new CloseHost { FailCommand = true };
        var source = input ? "BEGIN{\"source\"|getline x;close(\"source\")}" :
            "BEGIN{print 1|\"sink\";close(\"sink\")}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []));
        Assert.Equal(2, (await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None)).Value);
        Assert.Equal(input ? new[] { 10, 11 } : new[] { 11 }, host.Closes);
        Assert.Equal("awk: execution failed without exposing host exception details\n", host.Error);
    }

    [Theory]
    [InlineData("output")] [InlineData("input")] [InlineData("output-pipe")] [InlineData("input-pipe")]
    public async Task SuspendedRetirementVisitsEveryStreamWithoutReplayingSuccessfulCloses(string kind)
    {
        var operation = kind switch
        {
            "output" => "print i > (\"file\" i)",
            "input" => "getline x < (\"file\" i)",
            "output-pipe" => "print i | (\"sink\" i)",
            _ => "(\"source\" i) | getline x"
        };
        var host = new CloseHost { SucceedCloses = true, SuspendCloses = true };
        var budget = new CooperativeTestBudget(1 << 20);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(operation));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None));
        Assert.Equal(kind == "input-pipe" ? 16 : 8, host.Closes.Count);
        Assert.Equal(host.Closes.Count, host.Closes.Distinct().Count());
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);

        static AwkCommandInvocation BuildInvocation(string operation) =>
            AwkCommandInvocation.CreateWithStandardDescriptors("awk",
                ["BEGIN{for(i=0;i<8;i++){" + operation + "};fflush();fflush()}"], []);
    }

    private sealed class CloseHost : IAwkHost
    {
        private int _next = 10;
        private readonly StringBuilder _error = new();
        public CancellationTokenSource? Cancellation { get; init; }
        public bool FailCommand { get; init; }
        public bool SucceedCloses { get; init; }
        public bool SuspendCloses { get; init; }
        public List<int> Closes { get; } = [];
        public List<CancellationToken> CloseTokens { get; } = [];
        public string Error => _error.ToString();

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (descriptor == AwkFileDescriptor.StdErr) _error.Append(Encoding.UTF8.GetString(content.Span));
            return Task.FromResult(AwkExitCode.Success);
        }

        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) =>
            Task.FromResult(AwkOpenedFile.Opened(new(_next++)));

        public async Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            if (SuspendCloses) await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            Closes.Add(descriptor.Id);
            CloseTokens.Add(token);
            if (!SucceedCloses && !FailCommand && Closes.Count == 2)
            {
                Cancellation?.Cancel();
                token.ThrowIfCancellationRequested();
                return AwkDescriptorCloseResult.Failure(default, descriptorRetired: false);
            }
            return AwkDescriptorCloseResult.Closed();
        }

        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            Task.FromResult(AwkReadResult.EndOfFile);

        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            FailCommand
                ? Task.FromException<AwkExitCode>(new InvalidOperationException("Injected command failure."))
                : Task.FromResult(AwkExitCode.Success);

        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) =>
            Task.FromResult(AwkPipeCreationResult.Created(new(new(_next++), new(_next++))));
    }
}
