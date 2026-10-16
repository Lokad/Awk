namespace Lokad.Awk.Tests;

public sealed class AwkAdmissionRefusalTests
{
    [Fact]
    public async Task SinglePrintAdmissionRefusalStopsLaterAndEndWorkWithoutRecovery()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            if (budget.Refusals == 0 && host.GetOutput(AwkFileDescriptor.StdOut) == "before\n")
                budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        const string source = "BEGIN{print \"before\";OFMT=\"%8192.2f\";print 1.25;print \"after\"} END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(Command(source, budget));
        Assert.Equal(2, (await tool.ExecuteAsync(host, CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailedInitialRefusalDiagnosticStillReturnsFatalStatus(bool throwFromHost)
    {
        var budget = new CooperativeTestBudget(1 << 20) { RefuseNextAdmissionMinimumGrowth = 0 };
        var host = new TestOperationVisitor([]);
        if (throwFromHost) host.BeforeAppend = () => throw new InvalidOperationException("private host failure");
        else host.EnqueueAppendExitCode(2);
        var tool = AwkTestHarness.ParseAwk(Command("BEGIN{print 42}", budget));
        Assert.Equal(2, (await tool.ExecuteAsync(host, CancellationToken.None)).Value);
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(throwFromHost ? "" : "awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData("initial", false)] [InlineData("initial", true)]
    [InlineData("frontend", false)] [InlineData("frontend", true)]
    [InlineData("runtime", false)] [InlineData("runtime", true)]
    public async Task RefusalFailsTheCommandWithoutRecoveryAndLaterCommandsCanReuseTheBudget(string stage, bool commandBudget)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        Assert.True(budget.TryReserve(16, out var retained));
        using (retained)
        {
            var host = new MockFileSystem();
            if (stage != "runtime") budget.RefuseNextAdmissionMinimumGrowth = stage == "initial" ? 0 : 1;
            else host.AfterAppend = () =>
            {
                if (budget.Refusals == 0 && host.GetOutput(AwkFileDescriptor.StdOut) == "before\n")
                    budget.RefuseNextAdmissionMinimumGrowth = 1;
            };
            const string source = "BEGIN{print \"before\";s=sprintf(\"%8192s\",\"x\");print \"after\"} END{print \"end\"}";
            var tool = AwkTestHarness.ParseAwk(Command(source, commandBudget ? budget : null));
            var result = commandBudget ? await tool.ExecuteAsync(host, CancellationToken.None)
                : await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
            Assert.Equal(2, result.Value);
            Assert.Equal(stage == "runtime" ? "before\n" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(16, budget.Reserved);
            Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);

            var next = new MockFileSystem();
            var nextCommand = AwkTestHarness.ParseAwk(Command("BEGIN{print 42}", budget));
            Assert.Equal(0, (await nextCommand.ExecuteAsync(next, CancellationToken.None)).Value);
            Assert.Equal("42\n", next.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", next.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(16, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task InitialExecutionRefusalPreservesTheCompiledArtifactCharge()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using (var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), Command("BEGIN{print 42}", budget), CancellationToken.None))
        {
            var retained = budget.Reserved;
            Assert.True(retained > 0);
            budget.RefuseNextAdmissionMinimumGrowth = 0;
            var host = new MockFileSystem();
            Assert.Equal(2, (await compiled.ExecuteAsync(host, CancellationToken.None)).Value);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(retained, budget.Reserved);
            var next = new MockFileSystem();
            Assert.Equal(0, (await compiled.ExecuteAsync(next, CancellationToken.None)).Value);
            Assert.Equal("42\n", next.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(retained, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task RefusalWaitsForTheStartedChildBeforeRefunding()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var child = new TaskCompletionSource<IReadOnlyList<AwkExitCode>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        budget.OnRefusal = () => refused.TrySetResult();
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(90), new AwkFileDescriptor(91)));
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        host.RunCommandsOverride = (commands, token) =>
        {
            Assert.IsAssignableFrom<IMemoryScope>(Assert.Single(commands).MemoryBudget);
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            return child.Task;
        };
        var tool = AwkTestHarness.ParseAwk(Command("BEGIN{print \"before\";print 1 | \"child\";s=sprintf(\"%8192s\",\"x\");print \"after\"} END{print \"end\"}", budget));
        var execution = tool.ExecuteAsync(host, CancellationToken.None);
        try
        {
            await refused.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal(0, budget.ScopesDisposed);
        }
        finally { child.TrySetResult([AwkExitCode.Success]); }
        Assert.Equal(2, (await execution).Value);
        Assert.Equal("before\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    private static AwkCommandInvocation Command(string source, IMemoryBudget? budget) =>
        new("awk", [source], [], AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, budget);
}
