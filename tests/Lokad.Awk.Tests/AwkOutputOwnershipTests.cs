using System.Security.Cryptography;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkOutputOwnershipTests
{
    [Fact]
    public void OutputTargetsCanBeInspectedWithoutResolvingRedirection()
    {
        Assert.False(AwkOutputTarget.Standard.TryGetRedirection(out _));
        Assert.NotEmpty(AwkOutputTarget.Standard.ToString());

        var target = AwkOutputTarget.Redirected(new(
            AwkExpression.Literal("file"), AwkOutputRedirectionKind.Write));
        Assert.True(target.TryGetRedirection(out var redirection));
        Assert.Equal(AwkOutputRedirectionKind.Write, redirection.Kind);
        Assert.NotEmpty(target.ToString());
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
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellationToken);

    private sealed class OutputHost : IAwkHost, IDisposable
    {
        public MockFileSystem Files { get; } = new();
        public IncrementalHash Digest { get; } = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public long OutputBytes { get; private set; }
        public int Opened { get; private set; }
        public string Error { get; private set; } = "";
        public Func<Task>? PendingHeld { get; init; }

        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
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
            return AwkExitCode.Success;
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        { Opened++; return Files.OpenFileAsync(path, mode, token); }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Files.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
        public void Dispose() => Digest.Dispose();
    }
}
