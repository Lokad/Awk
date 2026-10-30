using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPipeRuntimeTests : AwkCurrentSurfaceTestBase
{
    public static IEnumerable<object[]> EmptyEnvironmentKeyCases()
    {
        foreach (var mutation in new[] { "read", "set", "delete" })
        foreach (var input in new[] { false, true })
        foreach (var forward in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [mutation, input, forward, governed];
    }

    [Theory]
    [MemberData(nameof(EmptyEnvironmentKeyCases))]
    public async Task EmptyEnvironmentKeysStayInAwkWithoutBreakingPipes(string mutation, bool input, bool forward, bool governed)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new(new(10), new(11)));
        host.EnqueueCommandExitCode(17);
        if (input)
        {
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent("PIPE\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { }));
            host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        }
        else host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var change = mutation switch
        {
            "read" => "x=ENVIRON[\"\"]",
            "set" => "ENVIRON[\"\"]=\"value\"",
            _ => "ENVIRON[\"\"]=\"value\";delete ENVIRON[\"\"]"
        };
        var operation = input ? "\"worker\"|getline value" : "print \"payload\"|\"worker\"";
        var script = "BEGIN{" + change + ";ENVIRON[\"NUMBER\"]=1.234;CONVFMT=\"%.2f\";" + operation +
            ";r=close(\"worker\");print r,(\"\" in ENVIRON)" + (input ? ",value" : "") + ";exit}";
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []);
        Assert.True(Awk.TryParse(command, out var tool));
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithCommandEnvironment(forward);
        if (governed) policy = policy.WithMemoryBudget(budget);

        var result = await tool.ExecuteAsync(host, policy, CancellationToken.None);
        Assert.True(result.Succeeded, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        var expected = "17 " + (mutation == "delete" ? "0" : "1") + (input ? " PIPE\n" : "\n");
        Assert.Equal(expected, host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        var child = Assert.Single(host.CommandInvocations);
        if (forward) Assert.Equal(new AwkEnvironmentVariable("NUMBER", "1.23"), Assert.Single(child.Environment));
        else Assert.Empty(child.Environment);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task Awk_RestrictedPolicyDoesNotForwardMutableEnvironmentToCommands()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(90),
            new AwkFileDescriptor(91)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{ENVIRON[\"TOKEN\"]=\"secret\"; print \"payload\" | \"consumer\"; exit}"));
        var policy = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithCommandPipes(true);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Empty(Assert.Single(host.CommandInvocations).Environment);
    }

    [Fact]
    public async Task CleanupAwaitsEveryDescriptorWithAnUncancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var completions = new[] {
            new TaskCompletionSource<AwkDescriptorCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<AwkDescriptorCloseResult>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var tokens = new List<CancellationToken>();
        var closed = new List<AwkFileDescriptor>();
        var host = new TestOperationVisitor([])
        {
            CloseDescriptorOverride = (descriptor, token) =>
            {
                tokens.Add(token);
                closed.Add(descriptor);
                return completions[descriptor.Id - 82].Task;
            }
        };
        var limits = AwkRuntimeLimits.CreateDefault();
        var adapter = new AwkIoHostAdapter(host, AwkPath.Root, AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, cancellation.Token, static _ => [],
            new(limits, AwkRuntimeCounters.Disabled), AwkMemoryAccounting.Disabled);
        for (var index = 0; index < completions.Length; index++)
        {
            var closing = adapter.CloseDescriptorForCleanupAsync(new(82 + index));
            try
            {
                Assert.False(closing.IsCompleted);
                Assert.False(tokens[index].CanBeCanceled);
            }
            finally
            {
                completions[index].TrySetResult(AwkDescriptorCloseResult.Closed());
                await closing;
            }
        }
        Assert.Equal(new[] { new AwkFileDescriptor(82), new AwkFileDescriptor(83) }, closed);
    }

    [Fact]
    public async Task Awk_PublicPipeLimitFailsBeforeCreatingAnotherPipe()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(70),
            new AwkFileDescriptor(71)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" | \"first\"; print \"two\" | \"second\"}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxPipes(1);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: pipe limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("first", Assert.Single(host.CommandInvocations).CommandName);
    }

    [Fact]
    public async Task Awk_PipeCommandReceivesCurrentEnvironmentAndInvocationDirectory()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(8),
            new AwkFileDescriptor(9)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var command = AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk",
            [
                "BEGIN{ENVIRON[\"TOKEN\"]=\"child\"; ENVIRON[\"NEW\"]=\"created\"; " +
                "CONVFMT=\"%.2f\"; ENVIRON[\"NUMBER\"]=1.234567; " +
                "delete ENVIRON[\"DROP\"]; ENVIRON[\"PWD\"]=\"/spoof\"; " +
                "print \"payload\" | \"consumer\"; exit}"
            ],
            [
                new AwkEnvironmentVariable("PWD", "/work"),
                new AwkEnvironmentVariable("TOKEN", "parent"),
                new AwkEnvironmentVariable("DROP", "remove")
            ]);
        var tool = AwkTestHarness.ParseAwk(command);

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        var child = Assert.Single(host.CommandInvocations);
        Assert.Equal("/work", child.CurrentDirectory.Path);
        Assert.Collection(
            child.Environment,
            variable => Assert.Equal(new AwkEnvironmentVariable("NEW", "created"), variable),
            variable => Assert.Equal(new AwkEnvironmentVariable("NUMBER", "1.23"), variable),
            variable => Assert.Equal(new AwkEnvironmentVariable("PWD", "/spoof"), variable),
            variable => Assert.Equal(new AwkEnvironmentVariable("TOKEN", "child"), variable));
    }

    [Fact]
    public async Task Awk_OutputPipeStreamsWritesToHostPipeDescriptor()
    {
        var pipeRead = new AwkFileDescriptor(10);
        var pipeWrite = new AwkFileDescriptor(11);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(pipeRead, pipeWrite));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" | \"cat -n\"; print \"two\" | \"cat -n\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        var invocation = Assert.Single(host.CommandInvocations);
        Assert.Equal("cat -n", invocation.CommandName);
        Assert.Equal(pipeRead, invocation.StdIn);
        Assert.Equal(AwkFileDescriptor.StdOut, invocation.StdOut);
        Assert.Equal(AwkFileDescriptor.StdErr, invocation.StdErr);
        Assert.Equal("one\ntwo\n", host.GetAppendedOutput(pipeWrite));
    }

    [Fact]
    public async Task Awk_OutputPipeCloseReturnsCommandExitStatus()
    {
        var pipeWrite = new AwkFileDescriptor(21);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(20), pipeWrite));
        host.EnqueueCommandExitCode(17);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"worker\"; print close(\"worker\")}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("payload\n", host.GetAppendedOutput(pipeWrite));
        Assert.Equal("17\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_OutputPipeAppendFailureFailsExecution()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(30), new AwkFileDescriptor(31)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendResult(AwkAppendResult.Failure(new(AwkOperationErrorKind.IoFailure, "destination failed")));
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"closed-reader\"; print \"after\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: file descriptor 31: write failed: destination failed\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_InputPipeGetlineReadsFromHostPipeDescriptor()
    {
        var pipeRead = new AwkFileDescriptor(40);
        var pipeWrite = new AwkFileDescriptor(41);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(pipeRead, pipeWrite));
        host.EnqueueCommandExitCode(0);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
            AwkUtf8Codec.Encode("hello\n"),
            AwkReadCompletion.EndOfFile, static _ => { }));
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print line}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        var invocation = Assert.Single(host.CommandInvocations);
        Assert.Equal("producer", invocation.CommandName);
        Assert.Equal(AwkFileDescriptor.StdIn, invocation.StdIn);
        Assert.Equal(pipeWrite, invocation.StdOut);
        Assert.Equal(AwkFileDescriptor.StdErr, invocation.StdErr);
        Assert.Equal("hello\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_InputPipeGetlineHonorsCustomRecordSeparator()
    {
        var pipeRead = new AwkFileDescriptor(42);
        var pipeWrite = new AwkFileDescriptor(43);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(pipeRead, pipeWrite));
        host.EnqueueCommandExitCode(0);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
            AwkUtf8Codec.Encode("first|second|"),
            AwkReadCompletion.EndOfFile, static _ => { }));
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{RS=\"|\"; \"producer\" | getline line; print line \"[\" RT \"]\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("first[|]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_InputPipeCloseFromReturnsCommandExitStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(50), new AwkFileDescriptor(51)));
        host.EnqueueCommandExitCode(17);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
            AwkUtf8Codec.Encode("payload\n"),
            AwkReadCompletion.EndOfFile, static _ => { }));
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print close(\"producer\", \"from\")}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("17\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }
}
