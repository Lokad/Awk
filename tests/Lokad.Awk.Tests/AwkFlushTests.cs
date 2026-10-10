using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkFlushTests
{
    [Fact]
    public async Task FlushingRetainsTheFileLimitAndReopensTheSameFileForAppend()
    {
        var host = new MockFileSystem();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(1);
        var state = State(limits);
        var io = Io(host, limits, state);
        await io.WriteAsync(Output("one", "first\n"));
        Assert.Equal(AwkFlushStatus.Success, await io.FlushPathAsync("one"));
        Assert.Equal(AwkFlushStatus.Success, await io.FlushPathAsync("one"));
        Assert.Equal(0, host.OpenFileCount);
        await io.WriteAsync(Output("one", "second\n"));
        await io.FlushAllAsync();
        Assert.Equal("first\nsecond\n", host.ReadFile("/one"));
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => io.WriteAsync(Output("two", "rejected\n")));
        Assert.Equal("awk: output file limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(host.FileExists("/two"));
        Assert.Equal(0, (await io.CloseAsync("one")).StatusCode);
        Assert.Equal(-1, (await io.CloseAsync("one")).StatusCode);
        await io.WriteAsync(Output("two", "accepted\n"));
        await io.CloseAsync();
        Assert.Equal("accepted\n", host.ReadFile("/two"));
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FilesAndPipesShareTheOutputLimit(bool pipeFirst)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxOutputFiles(1);
        var state = State(limits);
        var io = Io(host, limits, state);
        var pipe = Output("consumer", "payload\n", AwkOutputRedirectionKind.Pipe);
        var file = Output("one", "file\n");
        await io.WriteAsync(pipeFirst ? pipe : file);
        await io.FlushAllAsync();
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => io.WriteAsync(pipeFirst ? file : pipe));
        Assert.Equal("awk: output file limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        if (pipeFirst) Assert.False(files.FileExists("/one"));
        else Assert.Empty(host.Commands);
        await io.CloseAsync(pipeFirst ? "consumer" : "one");
        await io.WriteAsync(pipeFirst ? file : pipe);
        await io.CloseAsync();
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Theory]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, false)]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, true)]
    [InlineData((int)AwkExecutionEngineKind.Executable, false)]
    [InlineData((int)AwkExecutionEngineKind.Executable, true)]
    public async Task FlushHostFailurePreservesPriorOutputAndCleansUp(int engineKind, bool statement)
    {
        var engine = (AwkExecutionEngineKind)engineKind;
        var files = new MockFileSystem();
        var failedDescriptor = new AwkFileDescriptor(0);
        var failures = 0;
        files.CloseDescriptorResultOverride = (descriptor, closed) =>
        {
            if (failures++ == 0)
            {
                failedDescriptor = descriptor;
                return false;
            }
            return closed;
        };
        var call = statement ? "fflush(p)" : "print fflush(p)";
        var exit = await Run(files, engine, "BEGIN{p=\"one\";print \"before\";print \"x\">p;" + call + ";print \"after\"}");
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal($"awk: file descriptor {failedDescriptor.Id}: close failed\n", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, files.OpenFileCount);
        Assert.True(failures >= 2);
    }

    [Theory]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, false, false)]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, false, true)]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, true, false)]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, true, true)]
    [InlineData((int)AwkExecutionEngineKind.Executable, false, false)]
    [InlineData((int)AwkExecutionEngineKind.Executable, false, true)]
    [InlineData((int)AwkExecutionEngineKind.Executable, true, false)]
    [InlineData((int)AwkExecutionEngineKind.Executable, true, true)]
    public async Task WarningOutputLimitFailsBeforeTheFlushResultIsPrinted(
        int engineKind, bool statement, bool countersEnabled)
    {
        var engine = (AwkExecutionEngineKind)engineKind;
        const string warning = "awk: warning: fflush: `missing' is not an open file, pipe or co-process\n";
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxOutputBytes(7 + Encoding.UTF8.GetByteCount(warning) - 1);
        var counters = countersEnabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var files = new MockFileSystem();
        var call = statement ? "fflush(\"missing\")" : "print fflush(\"missing\")";
        var exit = await Run(files, engine, "BEGIN{print \"before\";" + call + ";print \"after\"}", limits, counters);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(counters.MemoryReservedBytes, counters.MemoryCommittedBytes + counters.MemoryReleasedReservedBytes);
    }

    [Theory]
    [InlineData((int)AwkExecutionEngineKind.Interpreter)]
    [InlineData((int)AwkExecutionEngineKind.Executable)]
    public async Task WarningHostExceptionDoesNotRepeatTheWarning(int engineKind)
    {
        var engine = (AwkExecutionEngineKind)engineKind;
        var files = new MockFileSystem();
        files.AfterAppend = () =>
        {
            if (files.GetOutput(AwkFileDescriptor.StdErr).Contains("fflush", StringComparison.Ordinal))
            {
                files.AfterAppend = null;
                throw new InvalidOperationException("private host detail");
            }
        };
        var exit = await Run(files, engine, "BEGIN{print \"before\";fflush(\"missing\");print \"after\"}");
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n" +
            AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorUnexpectedFailure), files.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task WarningMemoryPreflightPreservesErrnoAndReleasesItsAdmission()
    {
        const long maximum = 1 << 20;
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(maximum);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(limits, counters);
        using var sink = new AwkOutputSink(limits, state.MemoryGovernor, counters);
        var host = new MockFileSystem();
        var io = Io(host, limits, state);
        var execution = new AwkExecutionContext(sink, io);
        state.SetErrno(AwkErrno.FromHostMessage("keep"));
        var path = new string('x', 1024);
        var committed = state.MemoryGovernor.CurrentCommittedBytes;
        var pressure = maximum - committed - 1;
        state.MemoryGovernor.Reserve(pressure);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            AwkFflushRuntime.FlushAsync(AwkValue.FromText(path), state, execution).AsTask());
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(committed, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("keep", state.ResolveSpecialVariableValue(AwkSpecialVariableId.Errno).AsString());
        state.MemoryGovernor.ReleaseReserved(pressure);
        Assert.Equal(-1, await AwkFflushRuntime.FlushAsync(AwkValue.FromText(path), state, execution));
        Assert.Equal("keep", state.ResolveSpecialVariableValue(AwkSpecialVariableId.Errno).AsString());
        sink.Dispose();
        Assert.Equal(committed - AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, false)]
    [InlineData((int)AwkExecutionEngineKind.Interpreter, true)]
    [InlineData((int)AwkExecutionEngineKind.Executable, false)]
    [InlineData((int)AwkExecutionEngineKind.Executable, true)]
    public async Task MixedPipeLookupSelectsTheLatestOpeningAndFlushDoesNotAwaitCompletion(
        int engineKind, bool inputFirst)
    {
        var engine = (AwkExecutionEngineKind)engineKind;
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var openings = inputFirst ? "p|getline s;print \"x\"|p;" : "print \"x\"|p;p|getline s;print \"y\"|p;";
        var script = "BEGIN{p=\"consumer\";ERRNO=\"keep\";" + openings +
            "print fflush(p);print close(p),fflush(p),close(p);print ERRNO}";
        var exit = await Run(host, engine, script);
        Assert.Equal(0, exit.Value);
        Assert.Equal(inputFirst ? "0\n0 -1 0\nkeep\n" : "-1\n0 0 0\nkeep\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: cannot flush: pipe `consumer' opened for reading, not writing\n",
            files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, host.Commands.Count);
        Assert.Equal(inputFirst ? 1 : 2, host.Writes.Count);
        Assert.Equal(0, host.OpenPipeCount);
    }

    private static AwkRedirectedOutput Output(string path, string content,
        AwkOutputRedirectionKind kind = AwkOutputRedirectionKind.Write) =>
        AwkTestHarness.CreateRedirectedOutput(path, kind, content);

    private static AwkRuntimeState State(AwkRuntimeLimits limits, AwkRuntimeCounters? counters = null) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            Options(AwkExecutionEngineKind.Interpreter, limits, counters));

    private static AwkIo Io(IAwkHost host, AwkRuntimeLimits limits, AwkRuntimeState state) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, state.Counters, limits, state, static () => []);

    private static AwkRuntimeOptions Options(AwkExecutionEngineKind engine, AwkRuntimeLimits? limits = null,
        AwkRuntimeCounters? counters = null) =>
        new(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters ?? AwkRuntimeCounters.Disabled, limits ?? AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility, engine);

    private static async Task<AwkExitCode> Run(IAwkHost host, AwkExecutionEngineKind engine, string script,
        AwkRuntimeLimits? limits = null, AwkRuntimeCounters? counters = null)
    {
        var invocation = AwkTestHarness.BuildInvocation("awk", script);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var options = Options(engine, limits, counters);
        if (engine == AwkExecutionEngineKind.Executable)
        {
            var program = await AwkExecutor.CompileAsync(host, invocation, deadline.Token);
            return await program.ExecuteAsync(host, deadline.Token, options);
        }
        return await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation), deadline.Token, options);
    }
}
