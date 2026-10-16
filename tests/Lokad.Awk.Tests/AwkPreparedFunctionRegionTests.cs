namespace Lokad.Awk.Tests;

public sealed class AwkPreparedFunctionRegionTests
{
    private const string Functions = "function inc(x){return x+1} function twice(x){return inc(inc(x))}";
    private const string PairFunctions = "function inc(x,a){return x+a} function twice(x,a){return inc(inc(x,a),a)}";

    [Theory]
    [InlineData(Functions + " BEGIN{total=0;for(i=0;i<3;i++)total+=twice(7);print total}")]
    [InlineData("function id(x){return x} function left(x){return 3-x} BEGIN{total=0;for(i=id(0);left(i);i=id(i+1))total+=id(i);print total,i}")]
    [InlineData("function add(x){return x+1} BEGIN{a[1]=1;total=0;for(i=0;i<2;i++)total+=add(a);print total}")]
    [InlineData("function add(x){return x+1} BEGIN{x=40;total=0;for(i=0;i<2;i++)total+=add(x);print total,x}")]
    [InlineData(Functions + " BEGIN{i=0;total=twice(7);i=inc(total);total+=twice(i)}")]
    [InlineData("function id(x){return x} BEGIN{i=40;total=id(i);i=id(i+1);total+=id(i)}")]
    [InlineData("function add(x){return x+1} BEGIN{a[1]=1} BEGIN{i=0;total=add(a)}")]
    [InlineData("function add(x){return x+1} BEGIN{i=40;total=add(i);i=add(i);total+=add(i)}")]
    [InlineData("function step(x,a){return (x*3+a)%101} BEGIN{x=7;for(i=0;i<8;i++)x=step(x,i);total=x}")]
    [InlineData("function pack(x,y){return x*10+y} function inc(x){return x+1} BEGIN{x=3;total=pack(inc(x),pack(x,inc(x)))}")]
    [InlineData("function second(a,x){return x+1} function forward(a,x){return second(a,x)} BEGIN{a[1]=7} BEGIN{total=forward(a,41)}")]
    [InlineData("function zero(){return 1} function pack(a,b,c,d){return 1000*a+100*b+10*c+d} BEGIN{total=pack(zero(),2,3,4)}")]
    [InlineData("function defaults(x,y){return x+y} BEGIN{total=defaults(7);total+=defaults(1,2,3)}")]
    public async Task PreparedCallsMatchOrdinaryExecutionAtEveryFuelBoundary(string source)
    {
        for (var fuel = 0; fuel <= 260; fuel++)
        {
            var prepared = await RunAsync(source, false, fuel);
            var ordinary = await RunAsync(source, true, fuel);
            Assert.Equal(ordinary, prepared with { PreparedFunctions = 0 });
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task CallLoopDepthRefusalLeavesTheSameCallerState(int depth)
    {
        var source = Functions + " BEGIN{total=0;for(i=0;i<2;i++)total+=twice(7);print total}";
        var prepared = await RunAsync(source, false, functionDepth: depth);
        var ordinary = await RunAsync(source, true, functionDepth: depth);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = 0 });
        Assert.Equal(depth == 1 ? "awk: function recursion limit exceeded\n" : "", prepared.Error);
    }

    [Theory]
    [InlineData("default", true)]
    [InlineData("syntax", false)]
    [InlineData("vm", false)]
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    [InlineData("default", true, true)]
    [InlineData("syntax", false, true)]
    [InlineData("vm", false, true)]
    [InlineData("lint", false, true)]
    [InlineData("pending", false, true)]
    public async Task CallLoopsUseOriginalExecutionAtPolicyBoundaries(string boundary, bool prepared, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " BEGIN{total=0;for(i=0;i<3;i++)total+=" + (multiple ? "twice(7,1)" : "twice(7)") + ";print total}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, engine: boundary == "vm" ? AwkExecutionEngineKind.Executable
            : AwkExecutionEngineKind.Interpreter, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(limits, state.MemoryAccounting, counters);
        var context = new AwkEvaluationContext(state, output, io);
        var statements = program.BeginActions[0].Statements;
        await statements[0].ExecuteAsync(context);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior\n"));
        await statements[1].ExecuteAsync(context);
        await statements[2].ExecuteAsync(context);
        await output.FlushAsync(io);
        Assert.Equal((boundary == "pending" ? "prior\n" : "") + "27\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousLoopRuns);
        Assert.Equal(multiple && !prepared, counters.FunctionArgumentBufferRents > 0);
        if (boundary == "syntax") Assert.True(counters.FunctionStackSuspensions > 0);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("default", true)]
    [InlineData("syntax", false)]
    [InlineData("vm", false)]
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    [InlineData("default", true, true)]
    [InlineData("syntax", false, true)]
    [InlineData("vm", false, true)]
    [InlineData("lint", false, true)]
    [InlineData("pending", false, true)]
    public async Task ScalarCallActionsUseOriginalExecutionAtPolicyBoundaries(string boundary, bool prepared, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " BEGIN{total=" + (multiple ? "twice(7,1)" : "twice(7)") + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, engine: boundary == "vm" ? AwkExecutionEngineKind.Executable
            : AwkExecutionEngineKind.Interpreter, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(limits, state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior\n"));
        await program.BeginActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(9, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(boundary == "pending" ? "prior\n" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        Assert.Equal(multiple && !prepared, counters.FunctionArgumentBufferRents > 0);
        if (boundary == "syntax") Assert.True(counters.FunctionStackSuspensions > 0);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(16, true)] [InlineData(17, false)]
    public async Task ScalarCallActionsStopAtTheirStatementBound(int count, bool prepared)
    {
        var program = Bind(Functions + " BEGIN{" + string.Join(';', Enumerable.Repeat("total=inc(7)", count)) + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        await program.BeginActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(8, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        Assert.Equal(count, counters.SynchronousFunctionRuns);
        Assert.False(state.IsInsideFunction);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("function f(){return 7}", "f()", true, false)]
    [InlineData("function f(x){return x}", "f(7)", true, false)]
    [InlineData("function f(x,y){return x+y}", "f(3,4)", true, false)]
    [InlineData("function f(x,y,z){return x+y+z}", "f(1,2,4)", true, false)]
    [InlineData("function f(w,x,y,z){return w+x+y+z}", "f(1,1,1,4)", true, false)]
    [InlineData("function f(v,w,x,y,z){return v+w+x+y+z}", "f(1,1,1,1,3)", false, true)]
    [InlineData("function f(x,y){return x+y}", "f(7)", false, false)]
    [InlineData("function f(x){return x}", "f(7,2)", false, true)]
    public async Task CompleteCallActionsAvoidArgumentRentsWithinTheirArityBound(
        string function, string call, bool prepared, bool rents)
    {
        var program = Bind(function + " BEGIN{total=" + call + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        await program.BeginActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        Assert.Equal(rents ? 1 : 0, counters.FunctionArgumentBufferRents);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task PendingArgumentKeepsTheWholeScalarActionOnItsOriginalPath(bool cancel, bool multiple)
    {
        var program = Bind(Functions + " function pair(x,y){return x+y} BEGIN{i=inc(i);total=twice(7)}");
        var functions = program.Functions.ToDictionary(p => p.Key, p => p.Value);
        var prepared = AwkSynchronousFunction.Prepare(functions, null);
        var reads = new AwkGlobalReadLayout(program.GlobalNames, null);
        var i = AwkVariableName.FromText("i");
        var total = AwkVariableName.FromText("total");
        Assert.True(reads.TryGetIndex(i, out var indexSlot));
        Assert.True(reads.TryGetIndex(total, out var totalSlot));
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        state.InitializeGlobalReadSlots(reads.Count);
        state.InitializeFunctions(functions);
        state.AssignVariableValue(i, AwkValue.FromNumber(5));
        state.AssignVariableValue(total, AwkValue.FromNumber(5));
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(completion.Task);
        var action = new AwkAction(null,
            [AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(i, indexSlot),
                AwkExpression.FunctionCall(AwkFunctionName.FromText("inc"), [AwkExpression.PreparedGlobalVariable(i, indexSlot)])),
             AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(total, totalSlot),
                multiple
                    ? AwkExpression.FunctionCall(AwkFunctionName.FromText("pair"),
                        [AwkExpression.FunctionCall(AwkFunctionName.FromText("inc"), [AwkExpression.Number("7", 7)]), operand])
                    : AwkExpression.FunctionCall(AwkFunctionName.FromText("twice"), [operand]))], prepared);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var pending = action.ExecuteAsync(state, io, output).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(6, state.ResolveVariableValue(i).AsNumber());
            Assert.Equal(5, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(0, counters.SynchronousActionRuns);
            state.AssignVariableValue(x, AwkValue.FromNumber(43));
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(7));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            else Assert.Equal(AwkStatementFlow.Continue, await pending);
            Assert.Equal(6, state.ResolveVariableValue(i).AsNumber());
            Assert.Equal(cancel ? 5 : multiple ? 15 : 9, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(43, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(1, operand.Entries);
            Assert.Equal(0, counters.SynchronousActionRuns);
            Assert.Equal(multiple ? 1 : 0, counters.FunctionArgumentBufferRents);
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
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public async Task CancellationInsideCallRegionsPreservesCallerAndRefunds(bool governed, bool scalarAction, bool multiple)
    {
        var results = new List<(long Steps, long Peak)>();
        foreach (var ordinary in new[] { false, true })
        {
            var budget = new CooperativeTestBudget(2L << 20);
            await using (var owner = governed ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None)
                : AwkExecutionMemoryOwner.Disabled)
            {
                using var cancellation = new CancellationTokenSource();
                var counters = AwkRuntimeCounters.CreateEnabled();
                var state = State(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
                    cancellation.Token, owner);
                var functions = Bind((multiple ? "function inc(x,y){return x+y}" : "function inc(x){return x+1}") +
                    " BEGIN{}").Functions.ToDictionary(p => p.Key, p => p.Value);
                var prepared = AwkSynchronousFunction.Prepare(functions, null);
                state.InitializeFunctions(functions);
                var i = AwkVariableName.FromText("i");
                var total = AwkVariableName.FromText("total");
                var reads = new AwkGlobalReadLayout(new HashSet<AwkVariableName> { i, total }, null);
                Assert.True(reads.TryGetIndex(i, out var indexSlot));
                Assert.True(reads.TryGetIndex(total, out var totalSlot));
                state.InitializeGlobalReadSlots(reads.Count);
                state.AssignVariableValue(total, AwkValue.FromNumber(5));
                if (scalarAction) state.AssignVariableValue(i, AwkValue.FromNumber(2));
                var index = AwkExpression.PreparedGlobalVariable(i, indexSlot);
                var indexTarget = AwkReference.PreparedGlobalScalar(i, indexSlot);
                var evaluations = 0;
                var operand = new CancelingOperand(() => { if (++evaluations == 3) cancellation.Cancel(); });
                var body = AwkStatement.Assignment(AwkReference.PreparedGlobalScalar(total, totalSlot), AwkExpression.Add(
                    AwkExpression.PreparedGlobalVariable(total, totalSlot),
                    AwkExpression.FunctionCall(AwkFunctionName.FromText("inc"), multiple
                        ? [AwkExpression.Number("1", 1), operand] : [operand])));
                var loop = AwkStatement.For(AwkStatement.Assignment(indexTarget, AwkExpression.Number("0", 0)),
                    new([[AwkConditionClause.Comparison(index, AwkExpression.Number("8", 8), AwkComparison.LessThan)]]),
                    AwkStatement.PostIncrement(indexTarget),
                    [body],
                    ordinary ? null : prepared);
                var action = new AwkAction(null, [body, body, body], ordinary ? null : prepared);
                var host = new MockFileSystem();
                var io = Io(state, host, counters);
                using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
                var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await (scalarAction ? action.ExecuteAsync(state, io, output) : loop.ExecuteAsync(new(state, output, io))));
                Assert.Equal(cancellation.Token, error.CancellationToken);
                Assert.Equal(3, evaluations);
                Assert.Equal(21, state.ResolveVariableValue(total).AsNumber());
                Assert.Equal(2, state.ResolveVariableValue(i).AsNumber());
                Assert.Equal(ordinary ? 0 : 1, scalarAction ? counters.SynchronousActionRuns : counters.SynchronousLoopRuns);
                Assert.Equal(ordinary && multiple ? 3 : 0, counters.FunctionArgumentBufferRents);
                Assert.False(state.IsInsideFunction);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
                results.Add((counters.ExecutionSteps, counters.MemoryPeakLiveBytes));
                await io.CloseAsync();
            }
            Assert.Equal(0, budget.Reserved);
        }
        Assert.Equal(results[0], results[1]);
    }

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
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task NestedMemoryRefusalPreservesCallerAndRefundsAdmittedFrame(bool governed, bool multiple)
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = governed ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None) : AwkExecutionMemoryOwner.Disabled;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            owner: owner);
        var functions = Bind((multiple
            ? "function inc(x,y){return x+y} function twice(x,y){return inc(inc(x,y),y)}"
            : Functions) + " BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var frameBytes = AwkRuntimeMemory.EstimateFunctionFrameBytes() +
            (multiple ? 2 : 1) * AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(
                AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(AwkValue.FromNumber(7))));
        var fill = state.Limits.MaxExecutionMemoryBytes - state.MemoryAccounting.CurrentCommittedBytes - frameBytes;
        state.MemoryAccounting.ReserveAndCommit(fill);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await
                functions[AwkFunctionName.FromText("twice")].InvokeAsync(multiple
                    ? [AwkExpression.Number("7", 7), AwkExpression.Number("1", 1)]
                    : [AwkExpression.Number("7", 7)], new(state, output, io)));
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

    private static async Task<Result> RunAsync(string source, bool ordinary, int fuel = 0, int functionDepth = 0)
    {
        var program = Bind(source);
        var beginActions = program.BeginActions;
        if (ordinary)
        {
            // Omit call preparation when lowering the control's loops as well as its functions.
            var reads = new AwkGlobalReadLayout(program.GlobalNames, null);
            var scope = AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, globalReads: reads);
            beginActions = AwkParser.Parse(source).Syntax.Rules.Where(rule => rule.Kind == AwkRuleKind.Begin)
                .Select(rule => AwkStatementLowerer.LowerList(rule.ActionStatements, scope))
                .Select(lowered =>
                {
                    Assert.True(lowered.Succeeded);
                    return new AwkAction(null, lowered.Value);
                }).ToArray();
        }
        var functions = program.Functions.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (ordinary)
            foreach (var (name, function) in program.Functions)
                if (function.TryGetReturnExpression(out var expression))
                    functions[name] = new(name, function.LocalLayout, [AwkStatement.Return(AwkOptionalExpression.Some(expression))]);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        if (fuel > 0) limits = limits.WithMaxExecutionSteps(fuel);
        if (functionDepth > 0) limits = limits.WithMaxFunctionCallDepth(functionDepth);
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var error = "";
        try { foreach (var action in beginActions) await action.ExecuteAsync(state, io, output); }
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

    private sealed class CancelingOperand(Action evaluate) : AwkSynchronousExpression
    {
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            evaluate();
            return AwkValue.FromNumber(7);
        }
    }

    private readonly record struct Result(double Total, double Index, long Steps, long Peak,
        long PreparedFunctions, string Output, string Error);
}
