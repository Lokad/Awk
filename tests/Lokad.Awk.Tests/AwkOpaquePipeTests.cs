using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkOpaquePipeTests
{
    [Fact]
    public async Task ReportedCloseCaseMatchesTheGoldenContract()
    {
        var testCase = AwkGoldenCorpus.OpaquePipeCloseCase;
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { InputRecord = "a:b\n" };
        var command = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        Assert.True(Awk.TryParse(command, out var awk));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var exit = await awk.ExecuteAsync(host, watchdog.Token);
        Assert.Equal(testCase.ExpectedExitCode, exit.Value);
        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(testCase.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("echo a:b", Assert.Single(host.Commands).CommandName);
        Assert.Equal(0, host.OpenPipeCount);
    }

    public static TheoryData<string> Commands => new()
    {
        "echo a:b",
        @"worker C:\data\input.txt",
        "worker *.csv ? [ab]",
        "worker \"quoted argument\" 'another argument'",
        "../../worker"
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task InputClosePreservesOpaqueCommandAndStatus(string command)
    {
        foreach (var status in new[] { 0, 17 })
        {
            var files = new MockFileSystem();
            var host = new AwkEmptyPipeTestHost(files) { InputRecord = "a:b\n", CommandStatus = status };
            var exit = await Run(host, "BEGIN{cmd=" + Literal(command) +
                ";cmd|getline x;print x;print close(cmd);exit}");
            Assert.Equal(0, exit.Value);
            Assert.Equal("a:b\n" + status + "\n", files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(command, Assert.Single(host.Commands).CommandName);
            Assert.Equal(0, host.OpenPipeCount);
            Assert.Equal(0, files.OpenFileCount);
        }
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task OutputFlushDoesNotCloseOrAwaitTheCommand(string command)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { CommandStatus = 17 };
        var exit = await Run(host, "BEGIN{cmd=" + Literal(command) +
            ";print \"one\"|cmd;print fflush(cmd);print \"two\"|cmd;print close(cmd)}");
        Assert.Equal(0, exit.Value);
        Assert.Equal("0\n17\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("one\ntwo\n", Encoding.UTF8.GetString(host.Writes.SelectMany(bytes => bytes).ToArray()));
        Assert.Equal(command, Assert.Single(host.Commands).CommandName);
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task InputFlushKeepsWarningAndErrnoWithoutClosingThePipe(string command)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { InputRecord = "a:b\n", CommandStatus = 17 };
        var exit = await Run(host, "BEGIN{cmd=" + Literal(command) +
            ";cmd|getline x;ERRNO=\"keep\";print fflush(cmd),ERRNO;print close(cmd)}");
        Assert.Equal(0, exit.Value);
        Assert.Equal("-1 keep\n17\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: cannot flush: pipe `" + command + "' opened for reading, not writing\n",
            files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SameNameFileAndPipeKeepOpeningOrder(bool pipeLast, bool inputPipe)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { CommandStatus = 17 };
        var openPipe = inputPipe ? "cmd|getline x;" : "print \"pipe\"|cmd;";
        var openFile = "print \"file\">cmd;";
        var exit = await Run(host, "BEGIN{cmd=\"worker\";" + (pipeLast ? openFile + openPipe : openPipe + openFile) +
            "print close(cmd);print close(cmd)}");
        Assert.Equal(0, exit.Value);
        Assert.Equal(pipeLast ? "17\n0\n" : "0\n17\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("file\n", files.ReadFile("/worker"));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameNameFlushSelectsTheLatestLogicalStream(bool pipeLast)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { CommandStatus = 17 };
        const string openFile = "print \"file\">cmd;";
        const string openPipe = "cmd|getline x;";
        var exit = await Run(host, "BEGIN{cmd=\"worker\";" + (pipeLast ? openFile + openPipe : openPipe + openFile) +
            "print fflush(cmd);print close(cmd);print close(cmd)}");
        Assert.Equal(0, exit.Value);
        Assert.Equal(pipeLast ? "-1\n17\n0\n" : "0\n0\n17\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(pipeLast ? "awk: warning: fflush: cannot flush: pipe `worker' opened for reading, not writing\n" : "",
            files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("file\n", files.ReadFile("/worker"));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("to", true)]
    [InlineData("from", false)]
    public async Task CloseStatementAndDirectionsKeepOpaqueNames(string direction, bool output)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var open = output ? "print \"pipe\"|cmd;" : "cmd|getline x;";
        var close = direction.Length == 0 ? "close(cmd)" : "close(cmd,\"" + direction + "\")";
        var exit = await Run(host, "BEGIN{cmd=\"worker a:b\\\\file*.txt\";" + open + close + ";print \"done\"}");
        Assert.Equal(0, exit.Value);
        Assert.Equal("done\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Theory]
    [InlineData("echo a:b")]
    [InlineData(@"file\name")]
    [InlineData("*.csv")]
    public async Task InvalidFileRedirectsRemainInvalid(string path)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var exit = await Run(host, "BEGIN{print \"before\";print \"rejected\">" + Literal(path) + "}");
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: invalid path\n", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.Commands);
        Assert.Equal(0, files.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpaquePipeReopensAfterCloseUnderAFilePathRoot(bool input)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files) { CommandStatus = 17 };
        var open = input ? "cmd|getline x;" : "print \"pipe\"|cmd;";
        var policy = AwkExecutionPolicy.CreateDefault().WithAccessiblePathRoot(new AwkPath("/allowed"));
        var exit = await Run(host, "BEGIN{cmd=\"../../worker a:b\";" + open + "print close(cmd);" +
            open + "print close(cmd)}", policy);
        Assert.Equal(0, exit.Value);
        Assert.Equal("17\n17\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, host.Commands.Count);
        Assert.All(host.Commands, command => Assert.Equal("../../worker a:b", command.CommandName));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PolicyDenialPrecedesOpaquePipeEffects(bool input, bool sandbox)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var script = "BEGIN{cmd=\"worker a:b\\\\*.txt\";" + (input ? "cmd|getline x" : "print \"pipe\"|cmd") + "}";
        var args = sandbox ? new[] { "--sandbox", script } : new[] { script };
        Assert.True(Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", args, []), out var awk));
        var policy = AwkExecutionPolicy.CreateDefault().WithCommandPipes(sandbox);
        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(AwkUtf8Codec.Decode(sandbox ? Lokad.Awk.AwkMessages.ErrorSandboxRedirection :
            Lokad.Awk.AwkMessages.ErrorCommandPipesDisabled), files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.Commands);
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Fact]
    public async Task CancellationAtOpaquePipeCloseStillCleansUp()
    {
        using var cancellation = new CancellationTokenSource();
        var host = new TestOperationVisitor([]);
        var closed = new List<AwkFileDescriptor>();
        host.EnqueueAwkPipeDescriptors(new(new(80), new(81)));
        host.RunCommandsOverride = async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return [];
        };
        host.CloseDescriptorOverride = (descriptor, _) =>
        {
            closed.Add(descriptor);
            if (descriptor.Id == 81) cancellation.Cancel();
            return Task.FromResult(AwkOperationStatus.Success);
        };
        host.EnqueueAppendExitCode(0);
        Assert.True(Awk.TryParse(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{cmd=\"worker a:b\";print \"pipe\"|cmd;print close(cmd);print \"unreached\"}"), out var awk));
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            awk.ExecuteAsync(host, cancellation.Token).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Contains(new AwkFileDescriptor(81), closed);
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    private static string Literal(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static async Task<AwkExitCode> Run(IAwkHost host, string script, AwkExecutionPolicy? policy = null)
    {
        Assert.True(Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []), out var awk));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return await awk.ExecuteAsync(host, policy ?? AwkExecutionPolicy.CreateDefault(), watchdog.Token).ConfigureAwait(false);
    }
}
