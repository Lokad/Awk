namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    public async Task TopLevelLoopsPreserveResultsAndCallerScope(string loop)
    {
        foreach (var caller in new[] { false, true })
        {
            var result = await RunPureAction(caller, loop: loop);
            Assert.Equal(3, result.Index);
            Assert.Equal(caller ? 42 : 0, result.Caller);
            Assert.Equal(6, result.Total);
            Assert.Empty(result.Output);
            Assert.Empty(result.Error);
            Assert.Equal(0, result.Reserved);
        }
    }

    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    public async Task TopLevelLoopMemoryRefusalPreservesEarlierEffects(string loop)
    {
        var prepared = await RunPureAction(false, failMemory: true, loop: loop);

        Assert.Equal("ok", prepared.Text);
        Assert.Equal(0, prepared.Total);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
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
                Assert.Equal(1, counters.BoundRegionRuns);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
                await io.CloseAsync();
            }
    }

    [Fact]
    public async Task PureScalarActionsPreserveResultsAndCallerScope()
    {
        foreach (var caller in new[] { false, true })
        {
            var result = await RunPureAction(caller);
            Assert.Equal("beta", result.Text);
            Assert.Equal(12, result.Total);
            Assert.Equal(caller ? 42 : 0, result.Caller);
            Assert.Empty(result.Output);
            Assert.Empty(result.Error);
            Assert.Equal(0, result.Reserved);
        }
    }

    [Fact]
    public async Task PureScalarActionMemoryRefusalPreservesEarlierEffects()
    {
        var prepared = await RunPureAction(false, failMemory: true);

        Assert.Equal("ok", prepared.Text);
        Assert.Equal(0, prepared.Total);
        Assert.Equal("awk: memory limit exceeded\n", prepared.Error);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    public async Task ScalarStringsKeepPolicyAndOutputBoundaries(string boundary)
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
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
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
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(64)]
    [InlineData(257)]
    public async Task LongScalarActionsPrepareWithoutFunctions(int count)
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
        Assert.Equal(1, counters.BoundRegionRuns);
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

    private static async Task<PureActionResult> RunPureAction(bool caller, bool failMemory = false, string? loop = null)
    {
        var work = failMemory ? "total=sprintf(\"%2097152s\",\"\")" : "total+=2";
        var source = loop is not null
            ? loop == "for" ? "{s=\"ok\";for(i=0;i<3;i++)" + work + "}"
                : "{s=\"ok\";i=0;while(i<3){" + work + ";i++}}"
            : failMemory ? "{s=\"ok\";" + work + "}"
                : "{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))}";
        var program = Bind(source);
        var action = Assert.Single(program.MainActions);
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
        try
        {
            using var globals = state.EnterGlobalBindings();
            await action.ExecuteAsync(state, io, output);
        }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var local = caller ? state.ResolveFunctionLocalValue(new(0), total).AsNumber() : 0;
        if (caller) state.PopFunctionFrame();
        var result = new PureActionResult(state.ResolveVariableValue(AwkVariableName.FromText("s")).AsString(), state.ResolveVariableValue(total).AsNumber(), local, state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), state.MemoryAccounting.CurrentReservedBytes, host.GetOutput(AwkFileDescriptor.StdOut), error);
        await io.CloseAsync();
        return result;
    }

    private readonly record struct PureActionResult(string Text, double Total, double Caller, double Index, long Reserved, string Output, string Error);

    private const string Functions = "function inc(x){return x+1} function twice(x){return inc(inc(x))}";
    private const string PairFunctions = "function inc(x,a){return x+a} function twice(x,a){return inc(inc(x,a),a)}";

    [Theory]
    [InlineData(Functions + " BEGIN{total=0;for(i=0;i<3;i++)total+=twice(7);print total}", 27, 3, "27\n", "")]
    [InlineData("function id(x){return x} function left(x){return 3-x} BEGIN{total=0;for(i=id(0);left(i);i=id(i+1))total+=id(i);print total,i}", 3, 3, "3 3\n", "")]
    [InlineData("function add(x){return x+1} BEGIN{a[1]=1;total=0;for(i=0;i<2;i++)total+=add(a);print total}", 0, 0, "", "awk: attempt to use array `x' in a scalar context\n")]
    [InlineData("function add(x){return x+1} BEGIN{x=40;total=0;for(i=0;i<2;i++)total+=add(x);print total,x}", 82, 2, "82 40\n", "")]
    [InlineData(Functions + " BEGIN{i=0;total=twice(7);i=inc(total);total+=twice(i)}", 21, 10, "", "")]
    [InlineData("function id(x){return x} BEGIN{i=40;total=id(i);i=id(i+1);total+=id(i)}", 81, 41, "", "")]
    [InlineData("function add(x){return x+1} BEGIN{a[1]=1} BEGIN{i=0;total=add(a)}", 0, 0, "", "awk: attempt to use array `x' in a scalar context\n")]
    [InlineData("function add(x){return x+1} BEGIN{i=40;total=add(i);i=add(i);total+=add(i)}", 83, 41, "", "")]
    [InlineData("function step(x,a){return (x*3+a)%101} BEGIN{x=7;for(i=0;i<8;i++)x=step(x,i);total=x}", 93, 8, "", "")]
    [InlineData("function pack(x,y){return x*10+y} function inc(x){return x+1} BEGIN{x=3;total=pack(inc(x),pack(x,inc(x)))}", 74, 0, "", "")]
    [InlineData("function second(a,x){return x+1} function forward(a,x){return second(a,x)} BEGIN{a[1]=7} BEGIN{total=forward(a,41)}", 42, 0, "", "")]
    [InlineData("function zero(){return 1} function pack(a,b,c,d){return 1000*a+100*b+10*c+d} BEGIN{total=pack(zero(),2,3,4)}", 1234, 0, "", "")]
    [InlineData("function defaults(x,y){return x+y} BEGIN{total=defaults(7);total+=defaults(1,2,3)}", 10, 0, "", "")]
    public async Task CallsPreserveResultsAndDiagnostics(string source, double expectedTotal, double expectedIndex, string expectedOutput, string expectedError)
    {
        var result = await RunAsync(source);
        AssertSemantics(result, expectedTotal, expectedIndex, expectedOutput, expectedError);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task CallLoopDepthRefusalLeavesTheSameCallerState(int depth)
    {
        var source = Functions + " BEGIN{total=0;for(i=0;i<2;i++)total+=twice(7);print total}";
        var prepared = await RunAsync(source, functionDepth: depth);

        Assert.Equal(depth == 1 ? "awk: function recursion limit exceeded\n" : "", prepared.Error);
        Assert.Equal(depth == 1 ? 0 : 18, prepared.Total);
        Assert.Equal(depth == 1 ? 0 : 2, prepared.Index);
        Assert.Equal(depth == 1 ? "" : "18\n", prepared.Output);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    [InlineData("default", true)]
    [InlineData("syntax", true)]
    [InlineData("lint", true)]
    [InlineData("pending", true)]
    public async Task CallLoopsPreservePolicyAndOutputBoundaries(string boundary, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " {total=0;for(i=0;i<3;i++)total+=" + (multiple ? "twice(7,1)" : "twice(7)") + ";print total}");
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
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal((boundary == "pending" ? "prior\n" : "") + "27\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    [InlineData("default", true)]
    [InlineData("syntax", true)]
    [InlineData("lint", true)]
    [InlineData("pending", true)]
    public async Task ScalarCallActionsPreservePolicyAndOutputBoundaries(string boundary, bool multiple = false)
    {
        var program = Bind((multiple ? PairFunctions : Functions) +
            " {total=" + (multiple ? "twice(7,1)" : "twice(7)") + "}");
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
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(9, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(boundary == "pending" ? "prior\n" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(64)]
    public async Task LongScalarCallActionsPrepare(int count)
    {
        var program = Bind(Functions + " {" + string.Join(';', Enumerable.Repeat("total=inc(7)", count)) + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(8, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.Equal(count, counters.BoundFunctionRuns);
        Assert.False(state.IsInsideFunction);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("function f(){return 7}", "f()")]
    [InlineData("function f(x){return x}", "f(7)")]
    [InlineData("function f(x,y){return x+y}", "f(3,4)")]
    [InlineData("function f(x,y,z){return x+y+z}", "f(1,2,4)")]
    [InlineData("function f(w,x,y,z){return w+x+y+z}", "f(1,1,1,4)")]
    [InlineData("function f(v,w,x,y,z){return v+w+x+y+z}", "f(1,1,1,1,3)")]
    [InlineData("function f(x,y){return x+y}", "f(7)")]
    [InlineData("function f(x){return x}", "f(7,2)")]
    public async Task CompletePreparedCallActionsUseInlineOrAdmittedArgumentStorage(
        string function, string call)
    {
        var program = Bind(function + " {total=" + call + "}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(7, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public Task PendingArgumentsPreserveCallerAndEarlierEffects(bool governed, bool cancel, bool multiple) =>
        VerifyPendingArguments(governed, cancel, multiple);

    [Theory]
    [InlineData("t=x+2;return t", 9, false)] [InlineData("t=x+2;return t", 9, true)]
    [InlineData("t=x+2;if(t>8)return t;return 0", 9, false)] [InlineData("t=x+2;if(t>8)return t;return 0", 9, true)]
    [InlineData("if(x<3)return x+2;return x-2", 5, false)] [InlineData("if(x<3)return x+2;return x-2", 5, true)]
    public Task PendingArgumentsPreserveLocalAssignmentAndConditionalCalls(string body, double expected, bool cancel) =>
        VerifyPendingArguments(governed: true, cancel, multiple: false, body, expected);

    private static async Task VerifyPendingArguments(bool governed, bool cancel, bool multiple,
        string? body = null, double expected = 9)
    {
        var argument = "((getline incoming<\"/side\")?incoming:0)";
        var call = body is not null ? "step(" + argument + ")" :
            multiple ? "pair(inc(7)," + argument + ")" : "twice(" + argument + ")";
        var program = Bind(Functions + " function pair(x,y){return x+y}" +
            (body is null ? "" : "function step(x,t){" + body + "}") + "function outer(x){i=inc(i);total=" + call + ";caller=x}BEGIN{outer(42)}");
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = governed ? AwkExecutionMemoryOwner.Create(budget, cancellation.Token) : AwkExecutionMemoryOwner.Disabled;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var i = AwkVariableName.FromText("i");
        var total = AwkVariableName.FromText("total");
        var x = AwkVariableName.FromText("x");
        state.AssignVariableValue(i, AwkValue.FromNumber(5));
        state.AssignVariableValue(total, AwkValue.FromNumber(5));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entries = 0;
        var host = new MockFileSystem();
        host.AddFile("/side", "7\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entries++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var pending = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(pending.IsCompleted);
            Assert.Equal(6, state.ResolveVariableValue(i).AsNumber());
            Assert.Equal(5, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            state.AssignVariableValue(x, AwkValue.FromNumber(43));
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(pending.IsCompleted);
                if (governed) Assert.True(budget.Reserved > 0);
            }
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(AwkStatementFlow.Continue, await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(6, state.ResolveVariableValue(i).AsNumber());
            Assert.Equal(cancel ? 5 : multiple ? 15 : expected, state.ResolveVariableValue(total).AsNumber());
            var caller = AwkVariableName.FromText("caller");
            if (cancel) Assert.False(state.HasScalar(caller));
            else Assert.Equal(43, state.ResolveVariableValue(caller).AsNumber());
            Assert.Equal(1, entries);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await pending; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public async Task CancellationDuringCallArgumentsPreservesCompletedUpdates(bool governed, bool scalarAction, bool multiple)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = governed ? AwkExecutionMemoryOwner.Create(budget, cancellation.Token) : AwkExecutionMemoryOwner.Disabled;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
        var body = "total+=inc(" + (multiple ? "1," : "") + "next_value())";
        var program = Bind((multiple ? "function inc(x,y){return x+y}" : "function inc(x){return x+1}") +
            "function next_value(v){getline v<\"/side\";return v}BEGIN{" +
            (scalarAction ? body + ";" + body + ";" + body : "for(i=0;i<8;i++){" + body + "}") + "}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var i = AwkVariableName.FromText("i");
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(5));
        if (scalarAction) state.AssignVariableValue(i, AwkValue.FromNumber(2));
        var host = new MockFileSystem();
        host.AddFile("/side", "7\n7\n7\n");
        var reads = 0;
        ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (++reads == 3) cancellation.Cancel();
            host.ReadAsyncOverride = null;
            var result = host.ReadAsync(descriptor, Math.Min(maximum, 2), CancellationToken.None);
            host.ReadAsyncOverride = Read;
            return result;
        }
        host.ReadAsyncOverride = Read;
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(3, reads);
            Assert.Equal(21, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(2, state.ResolveVariableValue(i).AsNumber());
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            await io.CloseForCleanupAsync();
            output.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Fact]
    public async Task PreparedCallsPreserveOrdinaryState()
    {
        var source = Functions + " BEGIN{total=5;for(i=0;i<8;i++)total=total+twice(7);print total}";
        var prepared = await RunAsync(source);

        {
            Assert.Equal("77\n", prepared.Output);
            Assert.Equal(24, prepared.FunctionRuns);
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
    public async Task CallChainsBindEveryKnownTarget(int length)
    {
        var source = "";
        for (var index = 0; index < length; index++)
            source += "function f" + index + "(x){return " +
                (index + 1 == length ? "x+1" : "f" + (index + 1) + "(x)") + "}";
        source += " BEGIN{total=f0(7);print total}";
        var prepared = await RunAsync(source);

        Assert.Equal("8\n", prepared.Output);
        Assert.Equal(length, prepared.FunctionRuns);
    }

    [Fact]
    public async Task MutualRecursionUsesTheSameLogicalDepthLimit()
    {
        var source = "function f(x){return g(x)} function g(x){return f(x)} BEGIN{total=f(1)}";
        var prepared = await RunAsync(source, 80);

        Assert.Equal(80, prepared.FunctionRuns);
        Assert.Equal("awk: function recursion limit exceeded\n", prepared.Error);
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
        var program = Bind(Functions + "{total=twice(7)}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        await Assert.Single(program.MainActions).ExecuteAsync(state, io, output);
        var value = state.ResolveVariableValue(AwkVariableName.FromText("total"));
        Assert.Equal(9, value.AsNumber());
        Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(3, counters.BoundFunctionRuns);
        Assert.False(state.IsInsideFunction);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReturnValuesPreserveScalarBoundaries(bool array)
    {
        if (!array)
        {
            var origin = new AwkUntypedScalar();
            var result = AwkValue.FromUntypedScalar(origin).AsScalarCopy();
            Assert.True(result.IsUnassignedValue);
            Assert.False(result.TryGetUntypedScalar(out _));
            Assert.False(origin.IsScalar);
            origin.Promote();
            Assert.True(result.IsUnassignedValue);
            return;
        }
        var program = Bind("function result(){return a}BEGIN{saved=result()}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetArrayAwkValue(AwkArrayName.FromText("a"), new("key"), AwkValue.FromNumber(1));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            await Assert.ThrowsAsync<AwkRuntimeException>(() => Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask());
            Assert.Equal(1, state.GetArrayAwkValue(AwkArrayName.FromText("a"), new("key")).AsNumber());
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
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

    private static async Task<Result> RunAsync(string source, int functionDepth = 0, AwkRuntimeLimits? limitsOverride = null)
    {
        var program = Bind(source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = limitsOverride ?? AwkRuntimeLimits.CreateDefault();
        if (functionDepth > 0) limits = limits.WithMaxFunctionCallDepth(functionDepth);
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try { foreach (var action in program.BeginActions) await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        Assert.False(state.IsInsideFunction);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var result = new Result(state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber(), state.ResolveVariableValue(AwkVariableName.FromText("i")).AsNumber(), counters.BoundFunctionRuns, host.GetOutput(AwkFileDescriptor.StdOut), error);
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

    private static void AssertSemantics(Result actual, double total, double index, string output, string error)
    {
        Assert.Equal(total, actual.Total);
        Assert.Equal(index, actual.Index);
        Assert.Equal(output, actual.Output);
        Assert.Equal(error, actual.Error);
    }

    private readonly record struct Result(double Total, double Index, long FunctionRuns, string Output, string Error);
}
