using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkGetlineOwnershipTests
{
    [Theory]
    [InlineData(16)] [InlineData(128)]
    public async Task NestedSourceNamesCannotLoseAdmissionDuringDestinationEvaluation(int megabytes)
    {
        var budget = new CooperativeTestBudget(megabytes << 20);
        var host = new ReadHost();
        var exit = await Execute("""
            function fill(n){
                if(!n){x="";print "held";return 0}
                x=sprintf("%01000000d",0)
                if((getline a[fill(n-1)] < x)<0)failures++
                return 0
            }
            BEGIN{print "base";fill(32);print failures;print "done"}
            """, host, budget);
        var succeeds = megabytes == 128;
        Assert.Equal(succeeds ? 0 : 2, exit.Value);
        Assert.Equal(succeeds ? "base\nheld\n32\ndone\n" : "base\n", host.Output);
        Assert.Equal(succeeds ? "" : "awk: memory limit exceeded\n", host.Error);
        Assert.Equal(succeeds ? 32 : 0, host.Opens);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SourceNameSurvivesPendingDestinationAndCancellation(bool pipe, bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new ReadHost { Held = () => { entered.SetResult(); return completed.Task; } };
        var script = "function wipe(){x=\"\";print \"held\";return 0}" +
            "BEGIN{x=sprintf(\"%01000000d\",0);print (" +
            (pipe ? "x | getline a[wipe()]" : "getline a[wipe()] < x") + ")}";
        var execution = Execute(script, host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 2_000_000);
            Assert.Equal(0, host.Opens + host.Pipes);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 2_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(0, (await execution).Value);
            Assert.Equal(pipe ? "held\n0\n" : "held\n-1\n", host.Output);
        }
        Assert.Equal("", host.Error);
        Assert.Equal(cancel || pipe ? 0 : 1, host.Opens);
        Assert.Equal(!cancel && pipe ? 1 : 0, host.Pipes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task MainDestinationKeySurvivesPhaseMutationAndPendingRead(bool endFile, bool replace, bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new ReadHost { Held = () => { entered.SetResult(); return completed.Task; }, Input = !endFile };
        var phase = endFile ? "ENDFILE" : "BEGINFILE";
        var script = "BEGIN{x=sprintf(\"%01000000d\",0);s=getline a[x];print s,length(a)}" +
            phase + "{" + (replace ? "split(\"z\",a)" : "delete a") + ";x=\"\";print \"held\"}";
        var execution = Execute(script, host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 1_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 1_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(0, (await execution).Value);
            Assert.Equal("held\n" + (endFile ? replace ? "0 1\n" : "0 0\n" : replace ? "1 2\n" : "1 1\n"), host.Output);
        }
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> Execute(string script, IAwkHost host, IMemoryBudget budget, CancellationToken token = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), token);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DestinationFundingLastsThroughActualPendingRead(bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new ReadHost { Input = true, PendingRead = () => { entered.SetResult(); return completed.Task; } };
        var execution = Execute("BEGIN{x=sprintf(\"%01000000d\",0);s=getline a[x];print s,length(a)}" +
            "BEGINFILE{delete a;x=\"\";print \"held\"}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("held\n", host.Output);
            Assert.True(budget.Reserved >= 1_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 1_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(0, (await execution).Value);
            Assert.Equal("held\n1 1\n", host.Output);
        }
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    private sealed class ReadHost : IAwkHost
    {
        private bool _read;
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public int Opens { get; private set; }
        public int Pipes { get; private set; }
        public bool Input { get; init; }
        public Func<Task>? Held { get; init; }
        public Func<Task>? PendingRead { get; init; }
        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            else if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(content.Span);
                if (content.Span.SequenceEqual("held\n"u8) && Held is { } held) await held().ConfigureAwait(false);
            }
            return AwkExitCode.Success;
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        { Opens++; return Task.FromResult(AwkOpenedFile.MissingFile()); }
        public async Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            if (PendingRead is { } pending) await pending().ConfigureAwait(false);
            if (Input && !_read) { _read = true; return AwkReadResult.FromContent("a\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile); }
            return AwkReadResult.EndOfFile;
        }
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkOperationStatus.Success);
        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => Task.FromResult<IReadOnlyList<AwkExitCode>>([AwkExitCode.Success]);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token)
        { Pipes++; return Task.FromResult(AwkPipeCreationResult.Created(new(new(10), new(11)))); }
    }
}
