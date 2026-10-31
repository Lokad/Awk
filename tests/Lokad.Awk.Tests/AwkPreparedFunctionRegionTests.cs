namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    public async Task TopLevelLoopsMatchOriginalActionAndCallerScope(string loop)
    {
        {
            foreach (var caller in new[] { false, true })
            {
                var prepared = await RunPureAction(false, caller, loop: loop);
                var legacy = await RunPureAction(true, caller, loop: loop);
                Assert.Equal(legacy with { Prepared = prepared.Prepared }, prepared);
                Assert.Equal(1, prepared.Prepared);
                Assert.Equal(0, legacy.Prepared);
                {
                    Assert.Equal(3, prepared.Index);
                    Assert.Equal(caller ? 42 : 0, prepared.Caller);
                    if (!caller) Assert.Equal(6, prepared.Total);
                }
            }
        }
    }

    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    public async Task TopLevelLoopMemoryRefusalPreservesEarlierEffects(string loop)
    {
        var prepared = await RunPureAction(false, false, failMemory: true, loop: loop);
        var legacy = await RunPureAction(true, false, failMemory: true, loop: loop);
        Assert.Equal(legacy with { Prepared = prepared.Prepared }, prepared);
        Assert.Equal("ok", prepared.Text);
        Assert.Equal(0, prepared.Total);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
        Assert.Equal(1, prepared.Prepared);
        Assert.Equal(0, legacy.Prepared);
    }

    [Fact]
    public void TopLevelLoopPreparationKeepsNestedLoopsExcluded()
    {
        foreach (var source in new[] { "{for(i=0;i<1;i++)x++}", "{while(i<1)i++}" })
        {
            var loop = Bind(source).MainActions[0].Statements[0];
            Assert.NotNull(AwkStatementBlockExecutor.TryPrepareActions([loop], null));
            Assert.False(loop.TryPrepareSynchronousAction(out _));
            var condition = new AwkCondition(AwkExpression.Number(1));
            Assert.Null(AwkStatementBlockExecutor.TryPrepareActions(
                [AwkStatement.For(AwkStatement.NoOp(), condition, AwkStatement.NoOp(), [loop])], null));
            Assert.Null(AwkStatementBlockExecutor.TryPrepareActions([AwkStatement.While(condition, [loop])], null));
        }
    }

    [Fact]
    public async Task TopLevelLoopActionsKeepExistingPolicyAndCapturedFieldBoundaries()
    {
        foreach (var loop in new[] { "for(i=0;i<3;i++)total+=$1", "i=0;while(i<3){total+=$1;i++}" })
            foreach (var boundary in new[] { "default", "syntax", "lint", "pending", "captured-regex" })
            {
                var program = Bind("{" + loop + "}");
                var counters = AwkRuntimeCounters.CreateEnabled();
                var limits = AwkRuntimeLimits.CreateDefault();
                if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
                var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
                state.InitializeGlobalReadSlots(program.GlobalReadCount);
                var fs = AwkVariableName.FromText("FS");
                if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("2"));
                state.AssignVariable(fs, " ");
                var host = new MockFileSystem();
                var io = Io(state, host, counters);
                using var output = new AwkOutputSink(state.MemoryAccounting, counters);
                if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
                await program.MainActions[0].ExecuteAsync(state, io, output);
                Assert.Equal(6, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
                Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(boundary == "default" ? 1 : 0, counters.SynchronousActionRuns);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
                await io.CloseAsync();
            }
    }

    [Fact]
    public async Task PureScalarActionsMatchLegacyAndCallerScope()
    {
        {
            foreach (var caller in new[] { false, true })
            {
                var prepared = await RunPureAction(false, caller);
                var legacy = await RunPureAction(true, caller);
                Assert.Equal(legacy with { Prepared = prepared.Prepared }, prepared);
                Assert.Equal(1, prepared.Prepared);
                Assert.Equal(0, legacy.Prepared);
            }
        }
    }

    [Fact]
    public async Task PureScalarActionMemoryRefusalPreservesEarlierEffects()
    {
        var prepared = await RunPureAction(false, false, failMemory: true);
        var legacy = await RunPureAction(true, false, failMemory: true);
        Assert.Equal(legacy with { Prepared = prepared.Prepared }, prepared);
        Assert.Equal("ok", prepared.Text);
        Assert.Equal(0, prepared.Total);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
        Assert.Equal(1, prepared.Prepared);
        Assert.Equal(0, legacy.Prepared);
    }

    [Theory]
    [InlineData("default", true)]
    [InlineData("syntax", false)]
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    public async Task PureScalarActionKeepsExistingPolicyBoundaries(string boundary, bool prepared)
    {
        var program = Bind("{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id:beta:tail"));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal("beta", state.ResolveVariableValue(AwkVariableName.FromText("s")).AsString());
        Assert.Equal(12, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PureScalarActionsUseCapturedFieldDiagnosticBoundary(bool capturedRegex)
    {
        var program = Bind("{x=$1;y=2}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var fs = AwkVariableName.FromText("FS");
        state.AssignVariable(fs, capturedRegex ? "[,;]" : " ");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("left,right"));
        state.AssignVariable(fs, capturedRegex ? " " : "[,;]");
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(capturedRegex ? "left" : "left,right",
            state.ResolveVariableValue(AwkVariableName.FromText("x")).AsString());
        Assert.Equal(2, state.ResolveVariableValue(AwkVariableName.FromText("y")).AsNumber());
        Assert.Equal(capturedRegex ? 0 : 1, counters.SynchronousActionRuns);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(16, true)]
    [InlineData(17, false)]
    public async Task PureScalarActionsKeepStatementBoundWithoutFunctions(int count, bool prepared)
    {
        var program = Bind("{" + string.Join(';', Enumerable.Repeat("x++", count)) + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(count, state.ResolveVariableValue(AwkVariableName.FromText("x")).AsNumber());
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("{x=1;y=2}")]
    [InlineData("{for(i=0;i<3;i++)x++;y=2}")]
    [InlineData("{while(x<10)x++;y=2}")]
    public async Task PureScalarActionCancellationBeforeBodyPreservesValues(string source)
    {
        var program = Bind(source);
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var x = AwkVariableName.FromText("x"); var y = AwkVariableName.FromText("y");
        state.AssignVariableValue(x, AwkValue.FromNumber(7));
        state.AssignVariableValue(y, AwkValue.FromNumber(8));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await program.MainActions[0].ExecuteAsync(state, io, output));
        Assert.Equal(7, state.ResolveVariableValue(x).AsNumber());
        Assert.Equal(8, state.ResolveVariableValue(y).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    private static async Task<PureActionResult> RunPureAction(bool legacy, bool caller, bool failMemory = false, string? loop = null)
    {
        var program = Bind(loop is not null
            ? loop == "for" ? "{s=\"ok\";for(i=0;i<3;i++)total+=2}" : "{s=\"ok\";i=0;while(i<3){total+=2;i++}}"
            : failMemory ? "{s=\"ok\";total=0}"
            : "{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))}");
        var statements = program.MainActions[0].Statements;
        if (failMemory)
        {
            var reads = new AwkGlobalReadLayout(program.GlobalNames, null);
            Assert.True(reads.TryGetIndex(AwkVariableName.FromText("total"), out var slot));
            var assignment = AwkStatement.Assignment(
                AwkReference.PreparedGlobalScalar(AwkVariableName.FromText("total"), slot), AwkExpression.Literal(new string('x', 2 << 20)));
            var condition = new AwkCondition(AwkExpression.Number(1));
            statements = [statements[0], loop is null ? assignment : loop == "for"
                ? AwkStatement.For(AwkStatement.NoOp(),condition,AwkStatement.NoOp(),[assignment])
                : AwkStatement.While(condition,[assignment])];
        }
        if (legacy) statements = statements.Select(s => (AwkStatement)new OrdinaryStatement(s)).ToArray();
        var action = new AwkAction(null, statements);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: failMemory);
        var state = State(counters, limits, owner: memory.Owner);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id:beta:tail"));
        var total = AwkVariableName.FromText("total");
        if (caller) state.PushFunctionFrame(new([total]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try { await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var local = caller ? state.ResolveFunctionLocalValue(new(0), total).AsNumber() : 0;
        if (caller) state.PopFunctionFrame();
        var result = new PureActionResult(state.ResolveVariableValue(AwkVariableName.FromText("s")).AsString(), state.ResolveVariableValue(total).AsNumber(), local, state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), counters.SynchronousActionRuns, state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private sealed class OrdinaryStatement(AwkStatement inner) : AwkStatement
    {
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            inner.ExecuteAsync(context);
    }

    private readonly record struct PureActionResult(string Text, double Total, double Caller, double Index, long Prepared, long Reserved, string Output, string Error);

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
    public async Task PreparedCallsMatchOrdinaryExecution(string source)
    {
        {
            var prepared = await RunAsync(source, false);
            var ordinary = await RunAsync(source, true);
            Assert.Equal(ordinary, prepared with { PreparedFunctions = 0 });
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
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
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    [InlineData("default", true, true)]
    [InlineData("syntax", false, true)]
    [InlineData("lint", false, true)]
    [InlineData("pending", false, true)]
    public async Task CallLoopsUseOriginalExecutionAtPolicyBoundaries(string boundary, bool prepared, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " BEGIN{total=0;for(i=0;i<3;i++)total+=" + (multiple ? "twice(7,1)" : "twice(7)") + ";print total}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    [InlineData("default", true, true)]
    [InlineData("syntax", false, true)]
    [InlineData("lint", false, true)]
    [InlineData("pending", false, true)]
    public async Task ScalarCallActionsUseOriginalExecutionAtPolicyBoundaries(string boundary, bool prepared, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " BEGIN{total=" + (multiple ? "twice(7,1)" : "twice(7)") + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [InlineData(16, true)]
    [InlineData(17, false)]
    public async Task ScalarCallActionsStopAtTheirStatementBound(int count, bool prepared)
    {
        var program = Bind(Functions + " BEGIN{" + string.Join(';', Enumerable.Repeat("total=inc(7)", count)) + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [InlineData("function f(v,w,x,y,z){return v+w+x+y+z}", "f(1,1,1,1,3)", true, true)]
    [InlineData("function f(x,y){return x+y}", "f(7)", true, false)]
    [InlineData("function f(x){return x}", "f(7,2)", true, false)]
    public async Task CompletePreparedCallActionsUseInlineOrAdmittedArgumentStorage(
        string function, string call, bool prepared, bool rents)
    {
        var program = Bind(function + " BEGIN{total=" + call + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await program.BeginActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(prepared ? 1 : 0, counters.SynchronousActionRuns);
        Assert.Equal(rents ? 1 : 0, counters.FunctionArgumentBufferRents);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
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
                        [AwkExpression.FunctionCall(AwkFunctionName.FromText("inc"), [AwkExpression.Number(7)]), operand])
                    : AwkExpression.FunctionCall(AwkFunctionName.FromText("twice"), [operand]))], prepared);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task CancellationInsideCallRegionsPreservesCallerAndRefunds(bool governed, bool scalarAction, bool multiple)
    {
        foreach (var ordinary in new[] { false, true })
        {
            var budget = new CooperativeTestBudget(2L << 20);
            await using (var owner = governed ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None)
                : AwkExecutionMemoryOwner.Disabled)
            {
                using var cancellation = new CancellationTokenSource();
                var counters = AwkRuntimeCounters.CreateEnabled();
                var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
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
                    AwkExpression.PreparedGlobalVariable(total, totalSlot), AwkExpression.FunctionCall(AwkFunctionName.FromText("inc"), multiple
                        ? [AwkExpression.Number(1), operand] : [operand])));
                var loop = AwkStatement.For(AwkStatement.Assignment(indexTarget, AwkExpression.Number(0)), new(AwkExpression.Compare(index, AwkExpression.Number(8), AwkComparison.LessThan)), AwkStatement.PostIncrement(indexTarget), [body], ordinary ? null : prepared);
                var action = new AwkAction(null, [body, body, body], ordinary ? null : prepared);
                var host = new MockFileSystem();
                var io = Io(state, host, counters);
                using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
                await io.CloseAsync();
            }
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Fact]
    public async Task PreparedCallsPreserveOrdinaryState()
    {
        var source = Functions + " BEGIN{total=5;for(i=0;i<8;i++)total=total+twice(7);print total}";
        var prepared = await RunAsync(source, false);
        var ordinary = await RunAsync(source, true);
        Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
        Assert.Equal(0, ordinary.PreparedFunctions);
        {
            Assert.Equal("77\n", prepared.Output);
            Assert.Equal(24, prepared.PreparedFunctions);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
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
        Assert.Equal("awk: function recursion limit exceeded\n", prepared.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
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
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task NestedMemoryRefusalPreservesCallerAndRefundsAdmittedFrame(bool multiple)
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(),
            owner: owner);
        var functions = Bind((multiple
            ? "function inc(x,y){return x+y} function twice(x,y){return inc(inc(x,y),y)}"
            : Functions) + " BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var frameBytes = AwkRuntimeMemory.EstimateFunctionFrameBytes() +
            (multiple ? 2 : 1) * AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(
                AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(AwkValue.FromNumber(7))));
        var fill = budget.Limit - state.MemoryAccounting.CurrentCommittedBytes - frameBytes;
        state.MemoryAccounting.ReserveAndCommit(fill);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await
                functions[AwkFunctionName.FromText("twice")].InvokeAsync(multiple
                    ? [AwkExpression.Number(7), AwkExpression.Number(1)]
                    : [AwkExpression.Number(7)], new(state, output, io)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            // An admitted child may remain in the funded frame cache after unwind.
            Assert.InRange(state.MemoryAccounting.CurrentCommittedBytes, before,
                before + AwkRuntimeMemory.EstimateFunctionFrameBytes());
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
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    public async Task ConservativeBoundariesSelectOrdinaryExecution(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var functions = Bind(Functions + " BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        var value = await functions[AwkFunctionName.FromText("twice")].InvokeAsync(
            [AwkExpression.Number(7)], new(state, output, io));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparedReturnSnapshotsRejectArraysAndDetachUntypedOrigins(bool array)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        var layout = Bind("function f(){return 0} BEGIN{}").Functions[AwkFunctionName.FromText("f")].LocalLayout;
        var origin = new AwkUntypedScalar();
        Assert.True(AwkIdentifier.TryParse("a", out var identifier));
        var value = array ? AwkValue.FromGlobalArrayReference(identifier) : AwkValue.FromUntypedScalar(origin);
        var name = AwkFunctionName.FromText("result");
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>
        {
            [name] = new(name, layout, [AwkStatement.Return(AwkOptionalExpression.Some(new FixedReturnExpression(value)))])
        };
        var prepared = AwkSynchronousFunction.Prepare(functions, null) ??
            throw new InvalidOperationException("Expected a prepared function.");
        var function = prepared[name];
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        state.PushFunctionFrame(layout, []);
        var frameBytes = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            if (array)
                Assert.Throws<AwkRuntimeException>(() => function.EvaluateBody(new(state, output, io)));
            else
            {
                var result = function.EvaluateBody(new(state, output, io));
                Assert.True(result.IsUnassignedValue);
                Assert.False(result.TryGetUntypedScalar(out _));
                Assert.False(origin.IsScalar);
                origin.Promote();
                Assert.True(result.IsUnassignedValue);
            }
            Assert.Equal(1, counters.SynchronousFunctionRuns);
            Assert.Equal(frameBytes, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { state.PopFunctionFrame(); }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        await io.CloseAsync();
    }

    [Fact]
    public void NumericLocalReadsKeepNumberBitsAndFallbackLifetimes()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        var name = AwkVariableName.FromText("x");
        var slot = new AwkFunctionLocalSlotId(0);
        var layout = new AwkFunctionLocalLayout([name]);
        state.AssignVariableValue(name, AwkValue.FromNumber(99));
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        foreach (var number in new[] { 0d, -0d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, 42.5 })
        {
            state.PushFunctionFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(number))]);
            try
            {
                var values = counters.ValuesCreated;
                var value = state.ResolveFunctionLocalValue(slot, name);
                Assert.True(value.IsNumberValue);
                Assert.Equal(BitConverter.DoubleToInt64Bits(number), BitConverter.DoubleToInt64Bits(value.AsNumber()));
                Assert.Equal(values, counters.ValuesCreated);
            }
            finally { state.PopFunctionFrame(); }
            Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(6, counters.NumericLocalReads);
        Assert.Equal(0, counters.ScalarLocalValueReads);
        Assert.Equal(99, state.ResolveFunctionLocalValue(slot, name).AsNumber());
        Assert.Equal(6, counters.NumericLocalReads);
        state.PushFunctionFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromText("007"))]);
        try
        {
            Assert.Equal("007", state.ResolveFunctionLocalValue(slot, name).AsString());
            Assert.Equal("007", state.ResolveFunctionLocalValue(new(-1), name).AsString());
            Assert.Equal("007", state.ResolveFunctionLocalValue(new(9), name).AsString());
            Assert.Equal(6, counters.NumericLocalReads);
            Assert.Equal(1, counters.ScalarLocalValueReads);
        }
        finally { state.PopFunctionFrame(); }
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.False(state.IsInsideFunction);
    }

    private static AwkProgram Bind(string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));
        Assert.True(binding.Success);
        Assert.True(binding.Program.HasProgram);
        return binding.Program.Program;
    }

    private static async Task<Result> RunAsync(string source, bool ordinary, int functionDepth = 0, AwkRuntimeLimits? limitsOverride = null)
    {
        var program = Bind(source);
        var beginActions = program.BeginActions;
        if (ordinary)
        {
            // Omit call preparation when lowering the control's loops as well as its functions.
            var reads = new AwkGlobalReadLayout(program.GlobalNames, null);
            var scope = AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, globalReads: reads);
            beginActions = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None).Syntax.Rules.Where(rule => rule.Kind == AwkRuleKind.Begin)
                .Select(rule => AwkStatementLowerer.LowerList(rule.ActionStatements, scope))
                .Select(lowered =>
                {
                    Assert.True(lowered.Succeeded);
                    return new AwkAction(null, lowered.Value);
                }).ToArray();
        }
        var functions = program.Functions.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (ordinary)
        {
            var reads = new AwkGlobalReadLayout(program.GlobalNames, null);
            var syntax = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue)
                { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None).Syntax;
            foreach (var definition in syntax.Functions)
            {
                var name = AwkFunctionName.FromText(definition.Name);
                var layout = program.Functions[name].LocalLayout;
                var body = AwkStatementLowerer.LowerList(definition.BodyStatements,
                    AwkLoweringScope.Function(layout, globalReads: reads));
                Assert.True(body.Succeeded);
                functions[name] = new(name, layout, body.Value);
            }
        }
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = limitsOverride ?? AwkRuntimeLimits.CreateDefault();
        if (functionDepth > 0) limits = limits.WithMaxFunctionCallDepth(functionDepth);
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try { foreach (var action in beginActions) await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), counters.MemoryPeakLiveBytes, counters.SynchronousFunctionRuns, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, AwkRuntimeLimits limits, CancellationToken token = default, AwkExecutionMemoryOwner? owner = null, AwkLintMode lint = AwkLintMode.Off)
    {
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner ?? AwkExecutionMemoryOwner.Disabled };
        return new(AwkFieldSeparatorSpec.Default, token, options, new AwkRuntimeResourceMonitor(limits, counters, token), AwkCompatibilityProfile.Default.WithLintMode(lint));
    }

    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host, AwkRuntimeCounters counters) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, counters, state.Limits, state, static _ => []);

    private sealed class FixedReturnExpression(AwkValue value) : AwkSynchronousExpression
    {
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) => value;
    }

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

    private readonly record struct Result(double Total, double Index, long Peak, long PreparedFunctions, string Output, string Error);
}
