namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("t=x+1;if(t>3)return t;return x")]
    [InlineData("t=x;if(1/(t-5))return t;return 0")]
    [InlineData("t=1/(x-5);if(t)return t;return 0")]
    [InlineData("t=x;if(t)return 1/(x-5);return 0")]
    [InlineData("t=x;if(t<0)return 0;return 1/(x-5)")]
    [InlineData("t=length(x);if(t)return t;return 0")]
    public async Task LocalConditionalReturnsMatchOrdinary(string body)
    {
        var source = "function step(x,t){" + body + "} BEGIN{total=5;for(i=0;i<3;i++)total=step(total);print total}";
        {
            var prepared = await RunAsync(source, false);
            var ordinary = await RunAsync(source, true);
            Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
            Assert.Equal(0, ordinary.PreparedFunctions);
            Assert.True(prepared.PreparedFunctions > 0);
        }
    }

    [Theory]
    [InlineData("t=x;if(t)return t+1;return t-1", true)]
    [InlineData("t=x;if(t){x++;return t}return 0", true)]
    [InlineData("t=x;if(t)return;return 0", true)]
    [InlineData("t=x;if(t)return t;return", true)]
    [InlineData("t=x;if(t)return t;return 0;t++", true)]
    [InlineData("t=x;u=x;if(t)return t;return 0", true)]
    [InlineData("global=x;if(x)return x;return 0", true)]
    [InlineData("t+=x;if(t)return t;return 0", true)]
    [InlineData("t=x;if((getline line)>0)return t;return 0", true)]
    [InlineData("t=step(x);if(t)return t;return 0", true)]
    public void LocalConditionalReturnsPrepareCompleteComputationalBodies(string body, bool expected)
    {
        var function = Bind("function step(x,t,u){" + body + "} BEGIN{}").Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        Assert.Equal(expected, function.Region is not null);
    }

    [Theory]
    [InlineData("prefix", false)]
    [InlineData("condition", false)]
    [InlineData("return", false)]
    [InlineData("alternative", false)]
    [InlineData("prefix", true)]
    [InlineData("condition", true)]
    [InlineData("return", true)]
    [InlineData("alternative", true)]
    public void LocalConditionalReturnBoundsIncludeAllFourExpressions(string part, bool calls)
    {
        var name = AwkFunctionName.FromText("step");
        var local = AwkVariableName.FromText("t");
        var value = AwkExpression.Number(1);
        AwkExpression deep = calls ? new PrefixDeepCallExpression() : new TooDeepExpression();
        var function = new AwkUserFunction(name, new([local]),
            [AwkStatement.Assignment(AwkReference.LocalScalar(local, new(0)), part == "prefix" ? deep : value),
             AwkStatement.If(new(part == "condition" ? deep : value),
                [AwkStatement.Return(AwkOptionalExpression.Some(part == "return" ? deep : value))]),
             AwkStatement.Return(AwkOptionalExpression.Some(part == "alternative" ? deep : value))]);
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [name] = function };
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>>(
            AwkSynchronousFunction.Prepare(functions, null)));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task LocalConditionalReturnsKeepPolicyAndFieldDiagnosticGuards(string boundary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var functions = Bind("function step(x,t){t=$1+x;if(t>8)return t;return 0} BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var fs = AwkVariableName.FromText("FS");
        if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("2"));
        state.AssignVariable(fs, " ");
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            var value = await functions[AwkFunctionName.FromText("step")].InvokeAsync(
                [AwkExpression.Number(7)], new(state, output, io));
            Assert.Equal(9, value.AsNumber());
            Assert.Equal(1, counters.BoundFunctionRuns);
            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalConditionalReturnPendingArgumentKeepsCaller(bool cancel)
    {
        var functions = Bind("function step(x,t){t=x+2;if(t>8)return t;return 0} BEGIN{}").Functions;
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
        var pending = functions[AwkFunctionName.FromText("step")].InvokeAsync(
            [new PendingOperand(completion.Task)], new(state, output, io)).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(0, (counters.SynchronousFunctionRuns + counters.BoundFunctionRuns));
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(7));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            else Assert.Equal(9, (await pending).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(cancel ? 0 : 1, (counters.SynchronousFunctionRuns + counters.BoundFunctionRuns));
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
    [InlineData(4, 1, true)]
    [InlineData(5, 1, true)]
    [InlineData(65, 1, true)]
    [InlineData(2, 3, true)]
    public void LocalConditionalReturnCallsSupportOmittedLocalsAndExcessArguments(int slots, int arguments, bool expected)
    {
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(i => i == 0 ? "x" : "p" + i));
        var call = "step(" + string.Join(",", Enumerable.Repeat("1", arguments)) + ")";
        var parametersSource = "function step(" + parameters + "){x=1;if(x)return x;return 0}{total=" + call + "}";
        var program = Bind(parametersSource);
        Assert.NotNull(program.MainActions[0].Region);
        Assert.Equal(expected, program.Functions[AwkFunctionName.FromText("step")].Region is not null);
    }
    [Fact]
    public async Task LocalConditionalReturnNestedRefusalKeepsCallerAndRefunds()
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var functions = Bind("function inc(x){return x+1} function step(x){x=inc(x);if(x>0)return x;return 0} BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var frameBytes = AwkRuntimeMemory.EstimateFunctionFrameBytes() + AwkRuntimeMemory.EstimateFunctionLocalPayloadBytes(
            AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(AwkValue.FromNumber(1))));
        var fill = budget.Limit - state.MemoryAccounting.CurrentCommittedBytes - frameBytes;
        state.MemoryAccounting.ReserveAndCommit(fill);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await functions[AwkFunctionName.FromText("step")].InvokeAsync(
                    [AwkExpression.Number(7)], new(state, output, io)));
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
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task LocalConditionalReturnSharedRefusalLeavesPrefixUnchangedWithoutRetry()
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var function = Bind("function step(x,t){t=\"" + new string('a', 4096) + "\";if(x)return t;return \"\"} BEGIN{}")
            .Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        var region = Assert.IsType<AwkRegion>(function.Region);
        state.InitializeFunctions(functions);
        state.PushFunctionFrame(function.LocalLayout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7))]);
        var storage = state.CurrentFunctionFrame.GetRegionStorage(state);
        storage.Prepare(region.NumberCount, region.ValueCount, region.TargetCount, region.BuilderCount);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await function.ExecuteBodyAsync(new(state), state.CurrentFunctionFrame));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal("", state.ResolveFunctionLocalValue(new(1), AwkVariableName.FromText("t")).AsString());
            Assert.Equal(7, state.ResolveFunctionLocalValue(new(0), AwkVariableName.FromText("x")).AsNumber());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        finally { state.PopFunctionFrame(); }
    }
}
