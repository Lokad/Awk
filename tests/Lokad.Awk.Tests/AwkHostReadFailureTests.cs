using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostReadFailureTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_MainInputReadFailureIsFatalAfterCompletedRecords()
    {
        var readCount = 0;
        var host = new MockFileSystem
        {
            ReadLinesOverride = (_, _) => readCount++ == 0
                ? AwkReadResult.FromContent("one\n"u8.ToArray(), [], AwkReadCompletion.MoreData)
                : AwkReadResult.Failure(new AwkOperationError("Device read failed"))
        };
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "{print}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("one\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: file descriptor 0: read failed: Device read failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_GetlineReadFailureReturnsMinusOneAndSetsErrno()
    {
        var host = new MockFileSystem
        {
            ReadLinesOverride = (_, _) =>
                AwkReadResult.Failure(new AwkOperationError("Device read failed"))
        };
        host.AddFile("/data.txt", "unread");
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "BEGIN{result=(getline value < \"/data.txt\"); print result, ERRNO; exit}")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("-1 Device read failed\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: file descriptor 3: read failed: Device read failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_ScriptReadFailureUsesTheSourcePathDiagnostic()
    {
        var host = new MockFileSystem
        {
            ReadLinesOverride = (_, _) =>
                AwkReadResult.Failure(new AwkOperationError("Device read failed"))
        };
        host.AddFile("/script.awk", "BEGIN{print \"unread\"}");
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "-f", "/script.awk")));

        var exitCode = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            "awk: /script.awk: Device read failed\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_ScriptReadCancellationStillClosesTheSourceDescriptor()
    {
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem
        {
            AfterReadLines = cancellation.Cancel,
            ReadLinesOverride = (_, _) =>
                AwkReadResult.FromContent("BEGIN{print 1}"u8.ToArray(), [], AwkReadCompletion.MoreData)
        };
        host.AddFile("/script.awk", "unread");
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "-f", "/script.awk")));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tool.ExecuteAsync(host, cancellation.Token));

        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public void AwkReadResult_SeparatesEndOfFileFromFailure()
    {
        var endOfFile = AwkReadResult.EndOfFile;
        var failure = AwkReadResult.Failure(new AwkOperationError("Device read failed"));

        Assert.True(endOfFile.Success);
        Assert.True(endOfFile.ReachedEof);
        Assert.False(endOfFile.Failed);
        Assert.True(failure.Failed);
        Assert.False(failure.ReachedEof);
        Assert.Equal("Device read failed", failure.Error.Message);
    }

    [Fact]
    public void AwkReadResult_RejectsMalformedLineOffsets()
    {
        Assert.Throws<ArgumentException>(() =>
            AwkReadResult.FromContent("one\n"u8.ToArray(), [4], AwkReadCompletion.EndOfFile));
        Assert.Throws<ArgumentException>(() =>
            AwkReadResult.FromContent("one\ntwo\n"u8.ToArray(), [4, 4], AwkReadCompletion.EndOfFile));
    }

    [Fact]
    public void AwkReadResult_RejectsSuccessfulReadsThatCannotMakeProgress()
    {
        Assert.Throws<ArgumentException>(() =>
            AwkReadResult.FromContent(ReadOnlyMemory<byte>.Empty, [], AwkReadCompletion.MoreData));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AwkReadResult.FromContent("one\n"u8.ToArray(), [], (AwkReadCompletion)42));
    }

    [Fact]
    public void AwkReadResult_SnapshotsLineOffsets()
    {
        int[] offsets = [4];
        var result = AwkReadResult.FromContent(
            "one\ntwo\n"u8.ToArray(),
            offsets,
            AwkReadCompletion.EndOfFile);

        offsets[0] = 1;

        Assert.Equal([4], result.LineStartOffsets.ToArray());
    }
}
