using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPipeRuntimeTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_RestrictedPolicyDoesNotForwardMutableEnvironmentToCommands()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(90),
            new AwkFileDescriptor(91)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{ENVIRON[\"TOKEN\"]=\"secret\"; print \"payload\" | \"consumer\"; exit}"));
        var policy = AwkExecutionPolicy.CreateRestricted().WithCommandPipes(true);

        var exitCode = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Empty(Assert.Single(host.CommandInvocations).Environment);
    }

    [Fact]
    public async Task Awk_CommandHostCallHasIndependentTimeout()
    {
        var pendingCommand = new TaskCompletionSource<IReadOnlyList<AwkExitCode>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TestOperationVisitor([])
        {
            RunCommandsOverride = (_, _) => pendingCommand.Task
        };
        var limits = AwkRuntimeLimits.CreateDefault();
        var adapter = new AwkIoHostAdapter(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            static () => [],
            AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(limits, AwkRuntimeCounters.Disabled),
            commandTimeout: TimeSpan.FromMilliseconds(10));
        var command = AwkCommandInvocation.CreateWithStandardDescriptors(
            "never-finishes",
            [],
            []);

        var exception = await Assert.ThrowsAsync<AwkRuntimeException>(
            () => adapter.RunCommandAsync(command));

        Assert.Equal("awk: command pipe timed out\n", AwkUtf8Codec.Decode(exception.MessageBytes));
    }

    [Fact]
    public async Task Awk_CleanupDescriptorsShareOneAggregateDeadline()
    {
        var pendingClose = new TaskCompletionSource<AwkOperationStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TestOperationVisitor([])
        {
            CloseDescriptorOverride = (_, _) => pendingClose.Task
        };
        var limits = AwkRuntimeLimits.CreateDefault();
        var adapter = new AwkIoHostAdapter(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            static () => [],
            AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(limits, AwkRuntimeCounters.Disabled),
            cleanupTimeout: TimeSpan.FromMilliseconds(10));

        await adapter.CloseDescriptorForCleanupAsync(new AwkFileDescriptor(82));
        await Task.Delay(20);

        var secondClose = adapter.CloseDescriptorForCleanupAsync(new AwkFileDescriptor(83));

        Assert.True(secondClose.IsCompletedSuccessfully);
        await secondClose;
    }

    [Fact]
    public async Task Awk_PublicPipeLimitFailsBeforeCreatingAnotherPipe()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(70),
            new AwkFileDescriptor(71)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" | \"first\"; print \"two\" | \"second\"}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMaxPipes(1);

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
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
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

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

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
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" | \"cat -n\"; print \"two\" | \"cat -n\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

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
        host.EnqueueCommandExitCodes([17]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"worker\"; print close(\"worker\")}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("payload\n", host.GetAppendedOutput(pipeWrite));
        Assert.Equal("17\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_OutputPipeAppendFailureFailsExecution()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(30), new AwkFileDescriptor(31)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(9);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"closed-reader\"; print \"after\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: file descriptor 31: append failed with status 9\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_InputPipeGetlineReadsFromHostPipeDescriptor()
    {
        var pipeRead = new AwkFileDescriptor(40);
        var pipeWrite = new AwkFileDescriptor(41);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(pipeRead, pipeWrite));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            AwkUtf8Codec.Encode("hello\n"),
            [],
            AwkReadCompletion.EndOfFile));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print line}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

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
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            AwkUtf8Codec.Encode("first|second|"),
            [],
            AwkReadCompletion.EndOfFile));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{RS=\"|\"; \"producer\" | getline line; print line \"[\" RT \"]\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("first[|]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_InputPipeCloseFromReturnsCommandExitStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(50), new AwkFileDescriptor(51)));
        host.EnqueueCommandExitCodes([17]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            AwkUtf8Codec.Encode("payload\n"),
            [],
            AwkReadCompletion.EndOfFile));
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print close(\"producer\", \"from\")}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("17\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }
}
