using System.Collections.Concurrent;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkFlushTests
{
    [Theory]
    [InlineData("none", false)]
    [InlineData("stdout", false)]
    [InlineData("diagnostic", false)]
    [InlineData("both", false)]
    [InlineData("none", true)]
    [InlineData("stdout", true)]
    [InlineData("diagnostic", true)]
    [InlineData("both", true)]
    public async Task StatementFlushGuardKeepsPayloadAndFlowBoundaries(string payload, bool stop)
    {
        var host = new MockFileSystem();
        var appends = 0;
        host.AfterAppend = () => appends++;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(limits, counters);
        var io = Io(host, limits, state);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var seen = false;
        const string warning = "awk: warning: block marker\n";
        var stdout = payload is "stdout" or "both" ? "payload\n" : "";
        var stderr = payload is "diagnostic" or "both" ? warning : "";
        var flow = stop ? AwkStatementFlow.NextRecord : AwkStatementFlow.Continue;
        AwkStatement[] statements =
        [
            new CallbackStatement(() =>
            {
                if (stdout.Length != 0) output.Append(AwkText.FromSourceLiteralString(stdout));
                if (stderr.Length != 0) output.ReportDiagnostic(new(default,warning));
            },flow),
            new CallbackStatement(() =>
            {
                seen = true;
                Assert.Equal(stdout,host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Equal(stderr,host.GetOutput(AwkFileDescriptor.StdErr));
            },AwkStatementFlow.Continue)
        ];
        Assert.Equal(flow, await AwkStatementBlockExecutor.ExecuteAsync(statements, new(state, output, io)));
        Assert.Equal(!stop, seen);
        Assert.Equal(stdout, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(stderr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal((stdout.Length != 0 ? 1 : 0) + (stderr.Length != 0 ? 1 : 0), appends);
        Assert.Equal((payload == "none" ? 1 : 0) + (stop ? 0 : 1), counters.SkippedEmptyStatementFlushes);
        Assert.False(output.HasPendingOutput);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("success")]
    [InlineData("reject")]
    [InlineData("cancel")]
    public async Task StatementOutputWaitsForActualAppendBeforeContinuing(string completion)
    {
        var host = new PendingStatementHost();
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token,
            Options(limits, counters));
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, cancellation.Token, counters, limits, state, static () => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var continued = false;
        AwkStatement[] statements =
        [
            new CallbackStatement(() => output.Append(AwkText.FromSourceLiteralString("held bytes\n")), AwkStatementFlow.Continue),
            new CallbackStatement(() => continued = true, AwkStatementFlow.Continue)
        ];
        var pending = AwkStatementBlockExecutor.ExecuteAsync(statements, new(state, output, io)).AsTask();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(pending.IsCompleted);
            Assert.False(continued);
            Assert.True(output.HasPendingOutput);
            Assert.Equal("held bytes\n", Encoding.UTF8.GetString(host.Content.Span));
            if (completion == "cancel")
            {
                cancellation.Cancel();
                host.Release.TrySetCanceled(cancellation.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
                Assert.True(pending.IsCanceled);
            }
            else
            {
                host.Release.SetResult(completion == "success" ? AwkExitCode.Success : AwkExitCode.FatalError);
                if (completion == "success") Assert.Equal(AwkStatementFlow.Continue, await pending);
                else await Assert.ThrowsAsync<AwkRuntimeException>(async () => await pending);
            }
            Assert.Equal(completion == "success", continued);
            Assert.Equal(completion != "success", output.HasPendingOutput);
            Assert.Equal(completion == "success" ? 1 : 0, counters.SkippedEmptyStatementFlushes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            host.Release.TrySetResult(AwkExitCode.FatalError);
            await io.CloseAsync();
        }
    }

    private sealed class PendingStatementHost : IAwkHost
    {
        private readonly MockFileSystem _files = new();
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<AwkExitCode> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ReadOnlyMemory<byte> Content { get; private set; }
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            Content = content;
            Entered.TrySetResult(true);
            return Release.Task;
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => _files.OpenFileAsync(path, mode, token);
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => _files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => _files.ReadLinesAsync(descriptor, count, token);
        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => _files.RunCommandsAsync(commands, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) => _files.CreatePipeAsync(mode, token);
    }

    private sealed class CallbackStatement(Action run, AwkStatementFlow flow) : AwkStatement
    {
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            run();
            return ValueTask.FromResult(flow);
        }
    }

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
    [InlineData(false)]
    [InlineData(true)]
    public async Task FlushHostFailurePreservesPriorOutputAndCleansUp(bool statement)
    {
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
        var exit = await Run(files, "BEGIN{p=\"one\";print \"before\";print \"x\">p;" + call + ";print \"after\"}");
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal($"awk: file descriptor {failedDescriptor.Id}: close failed\n", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, files.OpenFileCount);
        Assert.True(failures >= 2);
    }

    [Fact]
    public async Task WarningHostExceptionDoesNotRepeatTheWarning()
    {
        var files = new MockFileSystem();
        files.AfterAppend = () =>
        {
            if (files.GetOutput(AwkFileDescriptor.StdErr).Contains("fflush", StringComparison.Ordinal))
            {
                files.AfterAppend = null;
                throw new InvalidOperationException("private host detail");
            }
        };
        var exit = await Run(files, "BEGIN{print \"before\";fflush(\"missing\");print \"after\"}");
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: `missing' is not an open file, pipe or co-process\n" +
            AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorUnexpectedFailure), files.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task WarningMemoryPreflightPreservesErrnoAndReleasesItsAdmission()
    {
        const long maximum = 1 << 20;
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(maximum);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(Options(limits, counters)))).State;
        using var sink = new AwkOutputSink(state.MemoryAccounting, counters);
        var host = new MockFileSystem();
        var io = Io(host, limits, state);
        var execution = new AwkExecutionContext(sink, io);
        state.SetErrno(AwkErrno.FromHostMessage("keep"));
        var path = new string('x', 1024);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = maximum - committed - 1;
        state.MemoryAccounting.Reserve(pressure);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            AwkFflushRuntime.FlushAsync(AwkValue.FromText(path), state, execution).AsTask());
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("keep", state.ResolveSpecialVariableValue(AwkSpecialVariableId.Errno).AsString());
        state.MemoryAccounting.ReleaseReserved(pressure);
        Assert.Equal(-1, await AwkFflushRuntime.FlushAsync(AwkValue.FromText(path), state, execution));
        Assert.Equal("keep", state.ResolveSpecialVariableValue(AwkSpecialVariableId.Errno).AsString());
        sink.Dispose();
        Assert.Equal(committed - AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedPipeLookupSelectsTheLatestOpeningAndFlushDoesNotAwaitCompletion(
bool inputFirst)
    {
        var files = new MockFileSystem();
        var host = new AwkEmptyPipeTestHost(files);
        var openings = inputFirst ? "p|getline s;print \"x\"|p;" : "print \"x\"|p;p|getline s;print \"y\"|p;";
        var script = "BEGIN{p=\"consumer\";ERRNO=\"keep\";" + openings +
            "print fflush(p);print close(p),fflush(p),close(p);print ERRNO}";
        var exit = await Run(host, script);
        Assert.Equal(0, exit.Value);
        Assert.Equal(inputFirst ? "0\n0 -1 0\nkeep\n" : "-1\n0 0 0\nkeep\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: cannot flush: pipe `consumer' opened for reading, not writing\n",
            files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, host.Commands.Count);
        Assert.Equal(inputFirst ? 1 : 2, host.Writes.Count);
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Fact]
    public async Task EmptyPipeTestHostCompletesWithoutTheCallersSynchronizationContext()
    {
        var host = new AwkEmptyPipeTestHost(new MockFileSystem());
        var created = await host.CreatePipeAsync(AwkPipeMode.Pipeline, CancellationToken.None);
        Assert.True(created.Success);
        var descriptors = created.Descriptors;
        var command = new AwkCommandInvocation("consumer", [], [], AwkPath.Root,
            descriptors.Read, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr);
        var context = new QueuedSynchronizationContext();
        var previous = SynchronizationContext.Current;
        Task<IReadOnlyList<AwkExitCode>> completion;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            completion = host.RunCommandsAsync([command], CancellationToken.None);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        try
        {
            Assert.False(completion.IsCompleted);
            await host.CloseDescriptorAsync(descriptors.Write, CancellationToken.None);
            var winner = await Task.WhenAny(completion, context.Posted.Task)
                .WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Same(completion, winner);
            Assert.Equal(AwkExitCode.Success, Assert.Single(await completion));
            Assert.Equal(0, host.OpenPipeCount);
        }
        finally { context.Drain(); }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _pending = new();
        public TaskCompletionSource Posted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override void Post(SendOrPostCallback callback, object? state)
        {
            _pending.Enqueue((callback, state));
            Posted.TrySetResult();
        }
        public void Drain()
        {
            while (_pending.TryDequeue(out var work)) work.Callback(work.State);
        }
    }

    private static AwkRedirectedOutput Output(string path, string content,
        AwkOutputRedirectionKind kind = AwkOutputRedirectionKind.Write) =>
        AwkTestHarness.CreateRedirectedOutput(path, kind, content);

    private static AwkRuntimeState State(AwkRuntimeLimits limits, AwkRuntimeCounters? counters = null) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            Options(limits, counters));

    private static AwkIo Io(IAwkHost host, AwkRuntimeLimits limits, AwkRuntimeState state) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, state.Counters, limits, state, static () => []);

    private static AwkRuntimeOptions Options(AwkRuntimeLimits? limits = null,
        AwkRuntimeCounters? counters = null) =>
        new(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters ?? AwkRuntimeCounters.Disabled, limits ?? AwkRuntimeLimits.CreateDefault());

    private static async Task<AwkExitCode> Run(IAwkHost host, string script,
        AwkRuntimeLimits? limits = null, AwkRuntimeCounters? counters = null)
    {
        var invocation = AwkTestHarness.BuildInvocation("awk", script);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var options = Options(limits, counters);
        return await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation), deadline.Token, options);
    }
}
