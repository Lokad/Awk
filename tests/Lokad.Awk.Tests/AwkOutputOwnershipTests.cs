using System.Security.Cryptography;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkOutputOwnershipTests
{
    [Theory]
    [InlineData("FS", false, false)] [InlineData("FS", true, false)]
    [InlineData("FIELDWIDTHS", false, false)] [InlineData("FIELDWIDTHS", true, false)]
    [InlineData("FPAT", false, false)] [InlineData("FPAT", true, false)]
    [InlineData("RS", false, false)] [InlineData("RS", true, false)]
    [InlineData("FS", false, true)] [InlineData("FS", true, true)]
    [InlineData("FIELDWIDTHS", false, true)] [InlineData("FIELDWIDTHS", true, true)]
    [InlineData("FPAT", false, true)] [InlineData("FPAT", true, true)]
    [InlineData("RS", false, true)] [InlineData("RS", true, true)]
    public async Task CsvAssignmentWarningPrecedesFollowingStdout(string name, bool governed, bool numeric)
    {
        var budget = new CooperativeTestBudget(4 << 20);
        var appends = new List<(AwkFileDescriptor Descriptor, string Text)>();
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                appends.Add((descriptor, AwkUtf8Codec.Decode(bytes.Span)));
                return Task.FromResult(AwkAppendResult.Accepted());
            }
        };
        var value = numeric ? "1" : "\"1\"";
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["--csv", "BEGIN{guard=1;" + name + "=" + value + ";print \"after\"}"], []);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(0, (await AwkTestHarness.ParseAwk(command).ExecuteAsync(host, policy, CancellationToken.None)).Value);

        var warning = name == "RS"
            ? "awk: warning: assignment to RS has no effect when using --csv\n"
            : "awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n";
        Assert.Equal(new[]
        {
            (AwkFileDescriptor.StdErr, warning),
            (AwkFileDescriptor.StdOut, "after\n")
        }, appends);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ComputationBetweenPrintsKeepsPendingOutputAndStopsCanceledEffects(bool governed, bool cancel)
    {
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = "";
        var stderr = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdErr) stderr += AwkUtf8Codec.Decode(bytes.Span);
                else
                {
                    stdout += AwkUtf8Codec.Decode(bytes.Span);
                    if (bytes.Span.SequenceEqual("first\n"u8))
                    {
                        entered.TrySetResult();
                        await release.Task.ConfigureAwait(false);
                        Assert.True(bytes.Span.SequenceEqual("first\n"u8));
                    }
                }
                return AwkAppendResult.Accepted();
            }
        };
        var awk = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{a=1;b=2;print \"first\";a+=3;b*=4;print a,b}"], []));
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("first\n", stdout);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(0, (await execution).Value);
            Assert.Equal(cancel ? "first\n" : "first\n4 8\n", stdout);
            Assert.Empty(stderr);
        }
        finally
        {
            release.TrySetResult();
            try { await execution; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 16)] [InlineData(true, 16)]
    [InlineData(false, 128)] [InlineData(true, 128)]
    public async Task NestedOutputCannotHideCapturedArguments(bool printf, int megabytes)
    {
        var budget = new CooperativeTestBudget(megabytes << 20);
        using var host = new OutputHost();
        var output = printf ? "printf \"%s%s\\n\",x,fill(n-1)" : "print x,fill(n-1)";
        var exit = await Execute("function fill(n){if(!n){x=\"\";print \"held\";return 0};" +
            "x=sprintf(\"%01000000d\",0);" + output + ";return 0}" +
            "BEGIN{print \"base\";fill(32);print \"done\"}", host, budget);
        var succeeds = megabytes == 128;
        Assert.Equal(succeeds ? 0 : 2, exit.Value);
        Assert.Equal(succeeds ? "" : "awk: memory limit exceeded\n", host.Error);
        using var expected = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        expected.AppendData("base\n"u8);
        if (succeeds)
        {
            expected.AppendData("held\n"u8);
            var zeros = Encoding.UTF8.GetBytes(new string('0', 1_000_000));
            for (var index = 0; index < 32; index++)
            {
                expected.AppendData(zeros);
                expected.AppendData(printf ? "0\n"u8 : " 0\n"u8);
            }
            expected.AppendData("done\n"u8);
        }
        Assert.Equal(expected.GetHashAndReset(), host.Digest.GetHashAndReset());
        Assert.Equal(succeeds ? 32_000_000 + 15 + 32 * (printf ? 2 : 3) : 5, host.OutputBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task ResolvedOutputNameStaysFundedDuringOperandAndHostSuspension(bool printf, bool cancel)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new OutputHost
        {
            PendingHeld = () => { entered.SetResult(); return completed.Task; }
        };
        var script = "function wipe(){x=\"\";print \"held\";return 0}" +
            "BEGIN{x=sprintf(\"%01000000d\",0);" + (printf ? "printf \"%s\",wipe()>x" : "print wipe()>x") + "}";
        var execution = Execute(script, host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved >= 2_000_000);
            Assert.Equal(0, host.Opened);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 2_000_000);
        }
        finally { completed.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(cancel ? 0 : 1, host.Opened);
        Assert.Equal(0, host.Files.OpenFileCount);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> Execute(string script, IAwkHost host, IMemoryBudget budget,
        CancellationToken cancellationToken = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellationToken);

    private sealed class OutputHost : IAwkHost, IDisposable
    {
        public MockFileSystem Files { get; } = new();
        public IncrementalHash Digest { get; } = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public long OutputBytes { get; private set; }
        public int Opened { get; private set; }
        public string Error { get; private set; } = "";
        public Func<Task>? PendingHeld { get; init; }

        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            else if (descriptor == AwkFileDescriptor.StdOut)
            {
                Digest.AppendData(content.Span);
                OutputBytes += content.Length;
                if (content.Span.SequenceEqual("held\n"u8) && PendingHeld is { } pending)
                    await pending().ConfigureAwait(false);
            }
            else return await Files.AppendAsync(descriptor, content, token).ConfigureAwait(false);
            return AwkAppendResult.Accepted();
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token)
        { Opened++; return Files.OpenFileAsync(path, mode, token); }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Files.CloseDescriptorAsync(descriptor, token);
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Files.ReadAsync(descriptor, count, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
        public void Dispose() => Digest.Dispose();
    }
}
