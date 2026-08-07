using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkPipeRuntimeTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_PipeCommandReceivesCurrentEnvironmentAndInvocationDirectory()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(8),
            new AwkFileDescriptor(9)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(true);
        var command = new AwkCommandInvocation(
            "awk",
            [
                "BEGIN{ENVIRON[\"TOKEN\"]=\"child\"; ENVIRON[\"NEW\"]=\"created\"; " +
                "delete ENVIRON[\"DROP\"]; ENVIRON[\"PWD\"]=\"/spoof\"; " +
                "print \"payload\" | \"consumer\"; exit}"
            ],
            [
                new AwkEnvironmentVariable("PWD", "/work"),
                new AwkEnvironmentVariable("TOKEN", "parent"),
                new AwkEnvironmentVariable("DROP", "remove")
            ]);
        var tool = Assert.IsType<Awk>(Awk.TryParse(command));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        var child = Assert.Single(host.CommandInvocations);
        Assert.Equal("/work", child.CurrentDirectory.Path);
        Assert.Collection(
            child.Environment,
            variable => Assert.Equal(new AwkEnvironmentVariable("NEW", "created"), variable),
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
        host.EnqueueCloseDescriptorResult(true);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"one\" | \"cat -n\"; print \"two\" | \"cat -n\"}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
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
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"worker\"; print close(\"worker\")}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
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
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueAppendExitCode(0);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"closed-reader\"; print \"after\"}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(AwkUtf8Codec.Encode("hello\n"), [], reachedEof: true));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueCloseDescriptorResult(true);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print line}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
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
            reachedEof: true));
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueCloseDescriptorResult(true);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{RS=\"|\"; \"producer\" | getline line; print line \"[\" RT \"]\"}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("first[|]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_InputPipeCloseFromReturnsCommandExitStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(50), new AwkFileDescriptor(51)));
        host.EnqueueCommandExitCodes([17]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(AwkUtf8Codec.Encode("payload\n"), [], reachedEof: true));
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{\"producer\" | getline line; print close(\"producer\", \"from\")}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("17\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }
}
