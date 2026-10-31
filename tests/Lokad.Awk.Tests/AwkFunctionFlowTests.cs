using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFunctionFlowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedFlowUnwindsFunctionLeases(bool skipFile)
    {
        var keyword = skipFile ? "nextfile" : "next";
        var binding = AwkBinder.Bind(AwkParser.Parse("function leaf(a){a[1]=\"local\";" + keyword + "} " +
            "function outer(v){return leaf()+v} {x=outer(1);print \"bad\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxFunctionCallDepth(2);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None, counters, limits, state, static _ => []);
        using var executor = new AwkRuntimeActionExecutor(program, state);
        var record = AwkInputRecordResult.Record(AwkText.FromString("row"), AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
        var expectedFlow = skipFile ? AwkStatementFlow.NextFile : AwkStatementFlow.Continue;

        Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        for (var index = 1; index < 256; index++)
        {
            Assert.Equal(expectedFlow, await executor.ProcessRecordAsync(state, io, record));
            Assert.False(state.IsInsideFunction);
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        }
        var error = Assert.Throws<AwkRuntimeException>(() => state.ValidateFunctionFlow(AwkStatementFlow.NextRecord));
        Assert.Equal("awk: next used outside input action\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Fact]
    public async Task AsynchronousReadUnwindsArgumentsAndClosesDescriptors()
    {
        const string source = "function skip(){getline value < \"/side\";next} " +
            "function consume(a,b){print \"bad\"} {consume(skip(),9);print \"bad\"} END{print NR}";
        var host = new MockFileSystem();
        host.AddFile("/side", "side\n");
        host.SetStandardInput("a\nb\n");
        var reads = 0;
        host.ReadAsyncOverride = ReadAsync;
        async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            reads++;
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, count, token); }
            finally { host.ReadAsyncOverride = ReadAsync; }
        }
        var compiled = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2), regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, exit.Value);
        Assert.True(reads >= 2);
        Assert.Equal("2\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
