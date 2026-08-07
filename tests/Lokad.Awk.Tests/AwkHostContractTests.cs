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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{exit}",
            "/unopened.txt")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success.Value, exitCode);
    }

    [Fact]
    public async Task Awk_HelpAppendFailureReturnsFatalStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(7);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "--help")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueAppendExitCode(0);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(
            "awk: host returned " + resultCount + " command results for one command\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
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
        host.EnqueueCloseDescriptorResult(false);
        host.EnqueueCloseDescriptorResult(true);
        host.EnqueueAppendExitCode(0);
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "-f", "/script.awk")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(
            "awk: /script.awk: Close failed.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }
}
