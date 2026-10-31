using System.Runtime.CompilerServices;
using System.Text;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordOwnershipTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task FunctionFieldSnapshotStaysFundedAcrossReplacementAndPendingOutput(bool cancel, bool forceStackBoundary)
    {
        var budget = new CooperativeTestBudget(64 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new RecordHost
        {
            Pending = () => { entered.SetResult(); return release.Task; },
            Observe = label =>
            {
                if (label == "held") Assert.True(budget.Reserved >= 1_000_000);
                if (label == "done") Assert.InRange(budget.Reserved, 1, 100_000);
            }
        };
        const string script = "function keep(x,y,t){t=x;print \"held\";return t} " +
            "BEGIN{FS=\":\";$0=sprintf(\"%1000000s\",\"x\")\":tail\";" +
            "v=keep($1,($0=\"new\"));print length(v),substr(v,length(v),1);v=\"\";print \"done\"}";
        var execution = Execute(script, host, budget, cancellation.Token, forceStackBoundary);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 1_000_000);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(cancel ? "held\n" : "held\n1000000 x\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("saved=$1", "print saved,saved+0", "0007 7\n")]
    [InlineData("a[1]=$1", "print a[1],a[1]+0", "0007 7\n")]
    [InlineData("a[$1]=1", "print a[\"0007\"],length(a)", "1 1\n")]
    [InlineData("n=length(a[$1])", "print (\"0007\" in a),length(a)", "1 1\n")]
    [InlineData("a[$1][\"x\"]=1", "print a[\"0007\"][\"x\"],length(a)", "1 1\n")]
    public async Task PersistentFieldEscapesOwnOnlyTheirText(string assignment, string inspect, string expected)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new RecordHost { Observe = label =>
        {
            if (label == "held") Assert.InRange(budget.Reserved, 1, 100_000);
        }};
        var script = "BEGIN{FS=\":\";$0=\"0007:\"sprintf(\"%1000000s\",\"x\");" +
            assignment + ";$0=\"\";print \"held\";" + inspect + "}";
        Assert.Equal(0, (await Execute(script, host, budget)).Value);
        Assert.Equal("held\n" + expected, host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RefusedFieldEscapeOrRecompositionStopsBeforePublication(bool recompose)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new RecordHost { Observe = label =>
        {
            if (label == "ready") budget.RefuseNextAdmissionMinimumGrowth = 1;
        }};
        var script = "function size(x){return length(x)} BEGIN{$0=\"0007 old\";n=size($1);" +
            (recompose ? "$2=\"new\";" : "") + "print \"ready\";" +
            (recompose ? "saved=$0;print saved" : "saved=$1") + ";print \"after\"}";
        var exit = await Execute(script, host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedDirtyRecordOutputStopsBeforePublishingAnyRecordBytes()
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new RecordHost { Observe = label =>
        {
            if (label == "ready") budget.RefuseNextAdmissionMinimumGrowth = 1;
        }};
        var exit = await Execute("BEGIN{FS=\":\";$0=sprintf(\"%20000s\",\"x\")\":old\";$2=\"É\";" +
            "print \"ready\";print;print \"after\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DirectRecordOutputStaysFundedThroughActualPendingCompletion(bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new string(' ', 999_999) + "x held\n";
        var host = new RecordHost
        {
            PendingText = expected,
            Pending = () => { entered.SetResult(); return release.Task; }
        };
        var execution = Execute("BEGIN{FS=\":\";$0=sprintf(\"%1000000s\",\"x\")\":old\";$2=\"held\";" +
            "print;$0=\"new\";print \"after\"}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 2_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 2_000_000);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(cancel ? expected : expected + "after\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(1, "$1=$1")] [InlineData(2, "$1=$1")]
    [InlineData(1, "$1=\"new\"")] [InlineData(2, "$1=\"new\"")]
    [InlineData(1, "$1=7")] [InlineData(2, "$1=7")]
    [InlineData(1, "NF=NF")] [InlineData(2, "NF=NF")]
    public async Task CapturedSeparatorIsFundedUntilTheRecordRelinquishesIt(int fields, string mutation)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new RecordHost { Observe = label =>
        {
            if (label == "held")
                Assert.InRange(budget.Reserved, fields > 1 ? 3_000_000 : 1, fields > 1 ? budget.Limit : 100_000);
            if (label == "done") Assert.InRange(budget.Reserved, 1, 100_000);
        }};
        var record = fields == 1 ? "left" : "left right";
        var script = "BEGIN{$0=\"" + record + "\";OFS=sprintf(\"%1000000s\",\"x\");" + mutation +
            ";OFS=\"\";print \"held\";$0=\"\";print \"done\"}";
        Assert.Equal(0, (await Execute(script, host, budget)).Value);
        Assert.Equal("held\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingOutputKeepsTheDirtyRecordFundedUntilActualCompletion(bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new RecordHost { Pending = () => { entered.SetResult(); return release.Task; } };
        var execution = Execute("BEGIN{$0=\"left right\";OFS=sprintf(\"%1000000s\",\"x\");" +
            "$1=$1;OFS=\"\";print \"held\";$0=\"\"}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 3_000_000);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 3_000_000);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal("held\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("$1=$1")] [InlineData("$1=\"new\"")] [InlineData("NF=NF")]
    public async Task RefusedSeparatorAdmissionStopsBeforeLaterEffects(string mutation)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new RecordHost { Observe = label =>
        {
            if (label == "ready") budget.RefuseNextAdmissionMinimumGrowth = 1;
        }};
        var exit = await Execute("BEGIN{$0=\"left right\";n=NF;OFS=sprintf(\"%1000000s\",\"x\");" +
            "print \"ready\";" + mutation + ";print \"after\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    public async Task UnusedSeparatorIsNotRetained(bool governed, int fields)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        var frame = new AwkFieldFrame(AwkRuntimeCounters.Disabled, accounting, value => value.ByteText);
        frame.SetRecordText(AwkText.FromString("left right"));
        Assert.Equal(2, frame.GetFieldCount(_ => [AwkFieldText.FromRecordSlice(0, 4), AwkFieldText.FromRecordSlice(5, 5)]));
        var separator = Mutate(frame, fields);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(IsAlive(separator));
        Assert.Equal(fields == 0 ? "" : "left", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
        GC.KeepAlive(frame);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> Mutate(AwkFieldFrame frame, int fields)
    {
        var separator = AwkText.FromString(new string('x', 1_000_000));
        frame.AssignFieldCount(new(fields), separator, _ => throw new InvalidOperationException("Unexpected split"));
        return new(separator);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAlive(WeakReference<AwkText> text) => text.TryGetTarget(out _);

    private static Task<AwkExitCode> Execute(string script, IAwkHost host, IMemoryBudget budget,
        CancellationToken token = default, bool forceStackBoundary = false)
    {
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget);
        if (forceStackBoundary)
            policy = policy.WithMaxSyntaxNestingDepth(AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth + 1);
        return AwkTestHarness.ParseAwk(
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, policy, token);
    }

    private sealed class RecordHost : IAwkHost
    {
        public Action<string>? Observe { get; init; }
        public Func<Task>? Pending { get; init; }
        public string PendingText { get; init; } = "held\n";
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            var text = Encoding.UTF8.GetString(content.Span);
            if (descriptor == AwkFileDescriptor.StdErr) Error += text;
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += text;
                Observe?.Invoke(text.TrimEnd('\n'));
                if (text == PendingText && Pending is { } pending) await pending().ConfigureAwait(false);
            }
            return AwkAppendResult.Accepted();
        }
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
    }
}
