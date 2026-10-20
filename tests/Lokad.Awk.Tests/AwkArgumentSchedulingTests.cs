using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk.Tests;

public sealed class AwkArgumentSchedulingTests
{
    [Theory]
    [InlineData("BEGIN{ARGV[ARGC++]=\"/b.txt\"}{print $0}", "/a.txt", "a\n", 1)]
    [InlineData("BEGIN{ARGV[ARGC++]=ARGV[1]}{print $0}", "/a.txt", "a\n", 1)]
    [InlineData("BEGIN{ARGV[1]=\"/b.txt\"}{print $0}", "/a.txt", "", 0)]
    [InlineData("BEGIN{ARGV[1]=\"/a.txt\"}{print $0}", "x=7", "", 0)]
    [InlineData("BEGIN{ARGV[ARGC++]=\"-\"}{print $0}", "", "", 0)]
    public async Task FrozenInputCapabilitiesDenyNewOrReclassifiedPathsBeforeOpening(
        string program, string operand, string expectedOutput, int expectedOpens)
    {
        {
            var host = Files();
            var arguments = operand.Length == 0 ? new[] { program } : new[] { program, operand };
            var compiled = await AwkExecutor.CompileAsync(host,
                AwkTestHarness.BuildInvocation("awk", arguments), CancellationToken.None);
            var options = Options(allowDynamicPaths: false);

            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);

            Assert.Equal(AwkExitCode.FatalError, exit);
            Assert.Equal(expectedOutput, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: dynamic main-input paths are disabled by execution policy\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(expectedOpens, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData("BEGIN{ARGV[1]=\"x=9\";ARGV[ARGC++]=\"y=8\"}{print x,y,$0}", "9 8 stdin\n", 0)]
    [InlineData("BEGIN{delete ARGV[1]}{print $0}", "stdin\n", 0)]
    [InlineData("{print $0}", "a\n", 1)]
    public async Task FrozenInputCapabilitiesPermitAssignmentsRemovalAndOriginalPaths(
        string program, string expectedOutput, int expectedOpens)
    {
        {
            var host = Files();
            var compiled = await AwkExecutor.CompileAsync(host,
                AwkTestHarness.BuildInvocation("awk", program, "/a.txt"), CancellationToken.None);

            var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
                Options(allowDynamicPaths: false));

            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(expectedOutput, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(expectedOpens, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData("b.txt", "awk: path is outside the execution policy root\n")]
    [InlineData("../b.txt", "awk: invalid path\n")]
    public async Task AddedPathsStillHonorPathValidationBeforeOpening(string path, string diagnostic)
    {
        {
            var host = Files();
            var compiled = await AwkExecutor.CompileAsync(host,
                AwkTestHarness.BuildInvocation("awk", "BEGIN{ARGV[ARGC++]=\"" + path + "\"}{print $0}"),
                CancellationToken.None);

            var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
                Options(accessibleRoot: new AwkPath("/jail")));

            Assert.Equal(AwkExitCode.FatalError, exit);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(diagnostic, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Fact]
    public async Task CancellationAfterClosingAFileStopsSparseOperandAdmission()
    {
        {
            using var cancellation = new CancellationTokenSource();
            var host = Files();
            host.AfterCloseDescriptor = cancellation.Cancel;
            var compiled = await AwkExecutor.CompileAsync(host,
                AwkTestHarness.BuildInvocation("awk", "{print $0}ENDFILE{ARGC=1e12}", "/a.txt"),
                CancellationToken.None);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                compiled.ExecuteAsync(host, cancellation.Token, Options()));

            Assert.Equal("a\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(1, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData("/a.txt")]
    [InlineData("/a=b.txt")]
    [InlineData("/données= space.txt")]
    public async Task OriginalPathsStayByteNativeAndDynamicPathsUseTheCompatibilityBoundary(string originalPath)
    {
        foreach (var dynamic in new[] { false, true })
        {
            var host = Files();
            var program = dynamic ? "BEGIN{ARGV[1]=\"/b.txt\"}{print $0}" : "{print $0}";
            var compiled = await AwkExecutor.CompileAsync(host,
                AwkTestHarness.BuildInvocation("awk", program, originalPath), CancellationToken.None);
            var counters = AwkRuntimeCounters.CreateEnabled();

            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, Options(counters));

            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(dynamic ? "b\n" : "a\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(dynamic ? 1 : 0, counters.StringInteropFallbacks);
            Assert.Equal(dynamic ? 1 : 0, counters.StringDecodes);
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    private static MockFileSystem Files()
    {
        var host = new MockFileSystem();
        host.AddFile("/a.txt", "a\n");
        host.AddFile("/a=b.txt", "a\n");
        host.AddFile("/données= space.txt", "a\n");
        host.AddFile("/b.txt", "b\n");
        host.SetStandardInput("stdin\n");
        return host;
    }

    private static AwkRuntimeOptions Options(
        AwkRuntimeCounters? counters = null,
        AwkRuntimeLimits? limits = null,
        bool allowDynamicPaths = true,
        AwkPath? accessibleRoot = null) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters ?? AwkRuntimeCounters.Disabled, limits ?? AwkRuntimeLimits.CreateDefault(),
accessiblePathRoot: accessibleRoot,
            allowDynamicMainInputPaths: allowDynamicPaths);
}
