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
    [InlineData(0)]
    [InlineData(2)]
    public async Task Awk_HostMustReturnOneResultPerPipeCommand(int resultCount)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(10),
            new AwkFileDescriptor(11)));
        host.EnqueueCommandExitCodes(resultCount == 0 ? [] : [0, 1]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: host returned " + resultCount + " command results for one command\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
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
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Failure);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}"));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: file descriptor 21: close failed\n",
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
