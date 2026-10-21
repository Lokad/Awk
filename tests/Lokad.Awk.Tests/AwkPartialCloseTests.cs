using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkPartialCloseTests
{
    [Theory]
    [InlineData("output", false)] [InlineData("input", false)] [InlineData("pipe", false)]
    [InlineData("output", true)] [InlineData("input", true)] [InlineData("pipe", true)]
    public async Task PartialFailureKeepsOnlyUnclosedEndpointsForCleanup(string kind, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new CloseHost { Cancellation = cancel ? cancellation : null };
        var source = kind switch
        {
            "output" => "BEGIN{print 1>\"one\";print 2>\"two\";print 3>\"three\"}",
            "input" => "BEGIN{getline x<\"one\";getline y<\"two\";getline z<\"three\"}",
            _ => "BEGIN{\"source\"|getline x}"
        };
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []));
        var execution = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        if (cancel)
        {
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal("", host.Error);
        }
        else
        {
            Assert.Equal(2, (await execution).Value);
            Assert.Equal("awk: file descriptor 11: close failed\n", host.Error);
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
        Assert.Equal(2, (await tool.ExecuteAsync(host, CancellationToken.None)).Value);
        Assert.Equal(input ? new[] { 10, 11 } : new[] { 11 }, host.Closes);
        Assert.Equal("awk: host returned 0 command results for one command\n", host.Error);
    }

    private sealed class CloseHost : IAwkHost
    {
        private int _next = 10;
        private readonly StringBuilder _error = new();
        public CancellationTokenSource? Cancellation { get; init; }
        public bool FailCommand { get; init; }
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

        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            Closes.Add(descriptor.Id);
            CloseTokens.Add(token);
            if (!FailCommand && Closes.Count == 2)
            {
                Cancellation?.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(AwkOperationStatus.Failure);
            }
            return Task.FromResult(AwkOperationStatus.Success);
        }

        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            Task.FromResult(AwkReadResult.EndOfFile);

        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<AwkExitCode>>(FailCommand ? [] : [AwkExitCode.Success]);

        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) =>
            Task.FromResult(AwkPipeCreationResult.Created(new(new(10), new(11))));
    }
}
