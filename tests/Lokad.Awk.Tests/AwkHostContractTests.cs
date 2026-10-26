using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostContractTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_ExitFromBeginDoesNotOpenMainInput()
    {
        var host = new TestOperationVisitor([]);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{exit}",
            "/unopened.txt"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
    }

    [Fact]
    public async Task Awk_HelpAppendFailureReturnsFatalStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(7);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "--help"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CommandThrowBeforeHandoffRetainsBothEndpointsForCleanup(bool input, bool governed)
    {
        var host = new TestOperationVisitor([]);
        var read = new AwkFileDescriptor(80);
        var write = new AwkFileDescriptor(81);
        host.EnqueueAwkPipeDescriptors(new(read, write));
        host.RunCommandOverride = static (_, _) => throw new InvalidOperationException("Private host detail.");
        var closed = new List<AwkFileDescriptor>();
        host.CloseDescriptorOverride = (descriptor, token) =>
        {
            Assert.False(token.CanBeCanceled);
            closed.Add(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        };
        host.EnqueueAppendExitCode(0);
        var script = input ? "BEGIN{\"worker\"|getline x;print \"after\"}" :
            "BEGIN{print \"payload\"|\"worker\";print \"after\"}";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", script));
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(new[] { read, write }, closed);
        var command = Assert.Single(host.CommandInvocations);
        Assert.Equal("worker", command.CommandName);
        Assert.Equal(input ? write : AwkFileDescriptor.StdOut, command.StdOut);
        Assert.Equal(input ? AwkFileDescriptor.StdIn : read, command.StdIn);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, "awk: pipe creation failed: Pipe allocation failed.\n")]
    [InlineData(true, "awk: pipe creation is unsupported: Pipes are disabled.\n")]
    public async Task Awk_PipeCreationFailureIsAnAwkDiagnostic(bool unsupported, string expectedError)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeCreationResult(
            unsupported
                ? AwkPipeCreationResult.NotSupported(new AwkOperationError("Pipes are disabled."))
                : AwkPipeCreationResult.Failure(new AwkOperationError("Pipe allocation failed.")));
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer a:b\\\\*.txt\"}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(expectedError, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.CommandInvocations);
    }

    [Fact]
    public async Task Awk_FailedPipeDescriptorCloseIsAnAwkDiagnostic()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(20),
            new AwkFileDescriptor(21)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: consumer: close failed\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_FailedSourceDescriptorCloseIsAPathDiagnostic()
    {
        var host = new MockFileSystem
        {
            CloseDescriptorResultOverride = static (_, _) => false
        };
        host.AddFile("/script.awk", "BEGIN{print 1}");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/script.awk"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(
            "awk: /script.awk: Close failed.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }
}
