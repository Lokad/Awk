using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFunctionFlowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RepeatedFlowUnwindsFunctionAndVmLeases(bool useExecutable, bool skipFile)
    {
        var keyword = skipFile ? "nextfile" : "next";
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "function leaf(a){a[1]=\"local\";" + keyword + "} " +
            "function outer(v){return leaf()+v} {x=outer(1);print \"bad\"}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(32768)
            .WithMaxFunctionCallDepth(2).WithMaxVmStackBytes(1024);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility,
            useExecutable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var executable = useExecutable ? AwkExecutableProgram.Compile(program) : null;
        if (executable is { } compiled)
            state.InitializeFunctions(compiled.Functions);
        else
            state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, counters, limits, state, static () => []);
        using var executor = new AwkRuntimeActionExecutor(program, executable, state);
        var record = AwkText.FromString("row");
        var expectedFlow = skipFile ? AwkStatementFlow.NextFile : AwkStatementFlow.Continue;

        Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var stepsPerRecord = counters.ExecutionSteps;
        Assert.True(stepsPerRecord > 0);
        for (var index = 1; index < 256; index++)
        {
            Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
            Assert.False(state.IsInsideFunction);
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal((index + 1) * stepsPerRecord, counters.ExecutionSteps);
        }
        var error = Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
        Assert.Equal("awk: next used outside input action\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsynchronousReadUnwindsArgumentsAndClosesDescriptors(bool useExecutable)
    {
        const string source = "function skip(){getline value < \"/side\";next} " +
            "function consume(a,b){print \"bad\"} {consume(skip(),9);print \"bad\"} END{print NR}";
        var host = new MockFileSystem();
        host.AddFile("/side", "side\n");
        host.SetStandardInput("a\nb\n");
        var reads = 0;
        host.ReadLinesAsyncOverride = ReadAsync;
        async Task<AwkReadResult> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            reads++;
            host.ReadLinesAsyncOverride = null;
            try { return await host.ReadLinesAsync(descriptor, count, token); }
            finally { host.ReadLinesAsyncOverride = ReadAsync; }
        }
        var compiled = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(32768).WithMaxFunctionCallDepth(2),
            AwkRegexEngineKind.DotNetCompatibility,
            useExecutable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, exit.Value);
        Assert.True(reads >= 2);
        Assert.Equal("2\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
