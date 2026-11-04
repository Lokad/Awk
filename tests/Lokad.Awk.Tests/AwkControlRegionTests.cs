namespace Lokad.Awk.Tests;

public sealed class AwkControlRegionTests
{
    [Theory]
    [InlineData("for", false, false)] [InlineData("for", true, false)] [InlineData("for", false, true)]
    [InlineData("while", false, false)] [InlineData("while", true, false)] [InlineData("while", false, true)]
    [InlineData("do", false, false)] [InlineData("do", true, false)] [InlineData("do", false, true)]
    public async Task LoopsPreserveUpdatesOutputAndLint(string kind, bool pendingOutput, bool lint)
    {
        var result = await RunLoop(kind, pendingOutput, lint);
        Assert.Equal(20, result.Value);
        Assert.Equal(8, result.Index);
        Assert.Equal(pendingOutput ? "prior" : "", result.Output);
        Assert.Empty(result.Error);
    }

    [Theory]
    [InlineData("for")] [InlineData("while")] [InlineData("do")]
    public async Task LoopRefusalPreservesTargetAndStopsBeforeIncrement(string kind)
    {
        var result = await RunLoop(kind, failMemory: true);
        Assert.Equal(7, result.Value);
        Assert.Equal(0, result.Index);
        Assert.Empty(result.Output);
        Assert.Equal("awk: memory limit exceeded\n", result.Error);
    }

    [Theory]
    [InlineData("")] [InlineData("pending")] [InlineData("lint")] [InlineData("separator")]
    public async Task ConditionalsPreserveBranchesAndEarlierOutput(string boundary)
    {
        var result = await RunConditional(boundary);
        Assert.Equal(4, result.Value);
        Assert.Equal(23, result.Index);
        Assert.Equal(boundary == "pending" ? "priorpriorpriorprior" : "", result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task ConditionalRefusalPreservesEarlierEffects()
    {
        var result = await RunConditional("", failMemory: true);
        Assert.Equal(0, result.Value);
        Assert.Equal(1, result.Index);
        Assert.Empty(result.Output);
        Assert.Equal("awk: memory limit exceeded\n", result.Error);
    }

    [Theory]
    [InlineData("for", false, false)] [InlineData("for", false, true)]
    [InlineData("for", true, false)] [InlineData("for", true, true)]
    [InlineData("while", false, false)] [InlineData("while", false, true)]
    [InlineData("while", true, false)] [InlineData("while", true, true)]
    [InlineData("do", false, false)] [InlineData("do", false, true)]
    [InlineData("do", true, false)] [InlineData("do", true, true)]
    [InlineData("if", false, false)] [InlineData("if", false, true)]
    [InlineData("if", true, false)] [InlineData("if", true, true)]
    [InlineData("do-condition", false, false)] [InlineData("do-condition", false, true)]
    [InlineData("do-condition", true, false)] [InlineData("do-condition", true, true)]
    public async Task PendingControlPreservesContextPublicationAndCleanup(string kind, bool governed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var value = "((getline incoming<\"/side\")?incoming:0)";
        var body = "entered++;x=" + value;
        var source = kind switch
        {
            "for" => "for(i=0;i<1;i++){" + body + "}",
            "while" => "while(i<1){" + body + ";i++}",
            "do" => "do{" + body + ";i++}while(i<1)",
            "if" => "if(1){" + body + "}",
            _ => "do{x=9;entered++}while(" + value + ")"
        };
        var program = Bind(source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters, cancellation.Token, governed);
        var state = memory.State;
        state.AssignVariableValue(AwkVariableName.FromText("entered"), AwkValue.FromNumber(0));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem();
        host.AddFile("/side", kind == "do-condition" ? "0\n" : "9\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var run = Assert.Single(program.MainActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(state.TryGetExecutionContext(out _));
            Assert.Equal(kind == "do-condition" ? 9 : 7, Number(state, "x"));
            Assert.Equal(1, Number(state, "entered"));
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(run.IsCompleted);
                if (governed) Assert.True(memory.Budget.Reserved > 0);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(!cancel || kind == "do-condition" ? 9 : 7, Number(state, "x"));
            Assert.Equal(1, Number(state, "entered"));
            Assert.Equal(1, reads);
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData("for", false)] [InlineData("for", true)]
    [InlineData("while", false)] [InlineData("while", true)]
    [InlineData("do", false)] [InlineData("do", true)]
    public async Task CancellationBeforeEntryMutatesNothing(string kind, bool governed)
    {
        var program = Bind(Loop(kind, "x=9"));
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters, cancellation.Token, governed);
        var state = memory.State;
        state.AssignVariableValue(AwkVariableName.FromText("i"), AwkValue.FromNumber(42));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        cancellation.Cancel();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await Assert.Single(program.MainActions).ExecuteAsync(state, io, output));
            Assert.Equal(7, Number(state, "x"));
            Assert.Equal(42, Number(state, "i"));
            Assert.False(state.TryGetExecutionContext(out _));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseForCleanupAsync(); }
    }

    [Theory]
    [InlineData(16)] [InlineData(17)] [InlineData(64)]
    public async Task LongDoWhileBodyKeepsMandatoryFirstIteration(int count)
    {
        var program = Bind("do{" + string.Concat(Enumerable.Repeat("executed++;", count)) + "}while(0)");
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters);
        var state = memory.State;
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        Assert.Equal(AwkStatementFlow.Continue, await Assert.Single(program.MainActions).ExecuteAsync(state, io, output));
        Assert.Equal(count, Number(state, "executed"));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    private static async Task<Result> RunLoop(string kind, bool pendingOutput = false, bool lint = false, bool failMemory = false) =>
        await Run(Loop(kind, failMemory ? "x=sprintf(\"%2097152s\",\"\")" : "x=(x*3+1)%101"),
            repetitions: 1, pendingOutput, lint);

    private static Task<Result> RunConditional(string boundary, bool failMemory = false) => Run(
        "if(x<5||i==0){if(x==0)i++;else i+=2;x=" +
        (failMemory ? "sprintf(\"%2097152s\",\"\")" : "x+3") + "}else{i+=10;x--}",
        repetitions: 4, pendingOutput: boundary == "pending", lint: boundary == "lint", initial: 0,
        separator: boundary == "separator");

    private static async Task<Result> Run(string source, int repetitions, bool pendingOutput, bool lint,
        double initial = 7, bool separator = false)
    {
        var program = Bind(source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(program, counters, lint: lint);
        var state = memory.State;
        state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromNumber(initial));
        if (separator)
        {
            state.AssignVariableValue(AwkVariableName.FromText("FS"), AwkValue.FromText("[ ,]"));
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("a b"));
        }
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try
        {
            for (var run = 0; run < repetitions; run++)
            {
                if (pendingOutput) output.Append(AwkText.FromSourceLiteralString("prior"));
                await Assert.Single(program.MainActions).ExecuteAsync(state, io, output);
            }
        }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.False(state.TryGetExecutionContext(out _));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(Number(state, "x"), Number(state, "i"), host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        return result;
    }

    private static string Loop(string kind, string body) => kind switch
    {
        "for" => "for(i=0;i<8;i++){" + body + "}",
        "while" => "while(i<8){" + body + ";i++}",
        _ => "do{" + body + ";i++}while(i<8)"
    };

    private static AwkProgram Bind(string body)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("{" + body + "}"));
        Assert.True(binding.Success);
        return binding.Program.Program;
    }

    private static CooperativeTestMemory State(AwkProgram program, AwkRuntimeCounters counters,
        CancellationToken token = default, bool governed = true, bool lint = false)
    {
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits,
            regexEngine: AwkUtf8RegexEngine.Instance);
        return memory.Initialize(() =>
        {
            var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, token, memory.Configure(options),
                new(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint ? AwkLintMode.Normal : AwkLintMode.Off));
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            state.AssignVariableValue(AwkVariableName.FromText("i"), AwkValue.FromNumber(0));
            state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromNumber(7));
            return state;
        });
    }

    private static double Number(AwkRuntimeState state, string name) => state.ResolveVariableValue(AwkVariableName.FromText(name)).AsNumber();

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static _ => []);

    private readonly record struct Result(double Value, double Index, string Output, string Error);
}
