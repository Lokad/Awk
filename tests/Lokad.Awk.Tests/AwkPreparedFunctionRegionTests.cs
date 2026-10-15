namespace Lokad.Awk.Tests;

public sealed class AwkPreparedFunctionRegionTests
{
    private const string Functions = "function inc(x){return x+1} function twice(x){return inc(inc(x))}";

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(5)] [InlineData(10)] [InlineData(20)]
    [InlineData(40)] [InlineData(80)] [InlineData(160)] [InlineData(400)]
    public async Task FuelAndPartialStateMatchOrdinaryCalls(int fuel)
    {
        var source = Functions + " BEGIN{total=5;for(i=0;i<8;i++)total=total+twice(7);print total}";
        var prepared = await RunAsync(source, false, fuel);
        var ordinary = await RunAsync(source, true, fuel);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
        Assert.Equal(0, ordinary.PreparedFunctions);
        if (fuel == 0 || fuel >= 400)
        {
            Assert.Equal("77\n", prepared.Output);
            Assert.Equal(24, prepared.PreparedFunctions);
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(8)]
    public async Task ChainsPrepareOnlyTheirBoundedAcyclicSuffix(int length)
    {
        var source = "";
        for (var index = 0; index < length; index++)
            source += "function f" + index + "(x){return " +
                (index + 1 == length ? "x+1" : "f" + (index + 1) + "(x)") + "}";
        source += " BEGIN{total=f0(7);print total}";
        var prepared = await RunAsync(source, false);
        var ordinary = await RunAsync(source, true);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = 0 });
        Assert.Equal("8\n", prepared.Output);
        Assert.Equal(Math.Min(length, 4), prepared.PreparedFunctions);
    }

    [Fact]
    public async Task MutualRecursionRemainsOrdinary()
    {
        var source = "function f(x){return g(x)} function g(x){return f(x)} BEGIN{total=f(1)}";
        var prepared = await RunAsync(source, false, 80);
        Assert.Equal(await RunAsync(source, true, 80), prepared);
        Assert.Equal(0, prepared.PreparedFunctions);
        Assert.Equal("awk: execution step limit exceeded\n", prepared.Error);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingOperandCompletesInCallerBeforePreparedBody(bool cancel)
    {
        var functions = Bind(Functions + " BEGIN{}").Functions;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(completion.Task);
        var pending = functions[AwkFunctionName.FromText("twice")].InvokeAsync([operand], new(state, output, io)).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(0, counters.SynchronousFunctionRuns);
            state.AssignVariableValue(x, AwkValue.FromNumber(43));
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(7));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            else Assert.Equal(9, (await pending).AsNumber());
            Assert.Equal(1, operand.Entries);
            Assert.Equal(43, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(cancel ? 0 : 3, counters.SynchronousFunctionRuns);
        }
        finally
        {
            completion.TrySetResult(AwkValue.FromNumber(7));
            try { await pending; } catch (OperationCanceledException) when (cancel) { }
            state.PopFunctionFrame();
            await io.CloseAsync();
        }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NestedMemoryRefusalPreservesCallerAndRefundsAdmittedFrame(bool governed)
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = governed ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None) : AwkExecutionMemoryOwner.Disabled;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            owner: owner);
        var functions = Bind(Functions + " BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var frameBytes = AwkRuntimeMemory.EstimateFunctionFrameBytes() +
            AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(AwkValue.FromNumber(7))));
        var fill = state.Limits.MaxExecutionMemoryBytes - state.MemoryAccounting.CurrentCommittedBytes - frameBytes;
        state.MemoryAccounting.ReserveAndCommit(fill);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await
                functions[AwkFunctionName.FromText("twice")].InvokeAsync([AwkExpression.Number("7", 7)], new(state, output, io)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.True(state.IsInsideFunction);
        }
        finally
        {
            state.MemoryAccounting.ReleaseCommitted(fill);
            state.PopFunctionFrame();
            await io.CloseAsync();
            output.Dispose();
            await owner.DisposeAsync();
        }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("executable")] [InlineData("syntax")] [InlineData("lint")] [InlineData("pending")]
    public async Task ConservativeBoundariesSelectOrdinaryExecution(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits,
            engine: boundary == "executable" ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter,
            lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var functions = Bind(Functions + " BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        var value = await functions[AwkFunctionName.FromText("twice")].InvokeAsync(
            [AwkExpression.Number("7", 7)], new(state, output, io));
        Assert.Equal(9, value.AsNumber());
        if (boundary == "pending")
        {
            Assert.Equal("prior", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.InRange(counters.SynchronousFunctionRuns, 0, 2);
        }
        else Assert.Equal(0, counters.SynchronousFunctionRuns);
        Assert.False(state.IsInsideFunction);
        await io.CloseAsync();
    }

    private static AwkProgram Bind(string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        Assert.True(binding.Program.HasProgram);
        return binding.Program.Program;
    }

    private static async Task<Result> RunAsync(string source, bool ordinary, int fuel = 0)
    {
        var program = Bind(source);
        var functions = program.Functions.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (ordinary)
            foreach (var (name, function) in program.Functions)
                if (function.TryGetReturnExpression(out var expression))
                    functions[name] = new(name, function.LocalLayout, [AwkStatement.Return(AwkOptionalExpression.Some(expression))]);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        if (fuel > 0) limits = limits.WithMaxExecutionSteps(fuel);
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var error = "";
        try { foreach (var action in program.BeginActions) await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber(),
            state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), counters.ExecutionSteps,
            counters.MemoryPeakLiveBytes, counters.SynchronousFunctionRuns, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, AwkRuntimeLimits limits,
        CancellationToken token = default, AwkExecutionMemoryOwner? owner = null,
        AwkExecutionEngineKind engine = AwkExecutionEngineKind.Interpreter, AwkLintMode lint = AwkLintMode.Off)
    {
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, limits, AwkRegexEngineKind.DotNetCompatibility, engine)
            { MemoryOwner = owner ?? AwkExecutionMemoryOwner.Disabled };
        return new(AwkFieldSeparatorSpec.Default, token, options,
            new AwkRuntimeResourceMonitor(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint));
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static () => []);

    private sealed class PendingOperand(Task<AwkValue> completion) : AwkExpression
    {
        public int Entries { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Entries++;
            return new(completion);
        }
    }

    private readonly record struct Result(double Total, double Index, long Steps, long Peak,
        long PreparedFunctions, string Output, string Error);
}
