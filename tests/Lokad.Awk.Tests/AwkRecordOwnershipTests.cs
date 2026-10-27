using System.Runtime.CompilerServices;
using System.Text;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordOwnershipTests
{
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
        CancellationToken token = default) => AwkTestHarness.ParseAwk(
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), token);

    private sealed class RecordHost : IAwkHost
    {
        public Action<string>? Observe { get; init; }
        public Func<Task>? Pending { get; init; }
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            var text = Encoding.UTF8.GetString(content.Span);
            if (descriptor == AwkFileDescriptor.StdErr) Error += text;
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += text;
                Observe?.Invoke(text.TrimEnd('\n'));
                if (text == "held\n" && Pending is { } pending) await pending().ConfigureAwait(false);
            }
            return AwkExitCode.Success;
        }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Task.FromResult(AwkReadResult.EndOfFile);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.MissingFile());
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
    }
}
