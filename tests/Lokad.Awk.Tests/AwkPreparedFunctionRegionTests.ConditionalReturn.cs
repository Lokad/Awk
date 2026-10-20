namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("if(x<3)return x+2;return x-2", 1)]
    [InlineData("if(x<3)return x+2;return x-2", 7)]
    [InlineData("if(1/x)return x+2;return x-2", 0)]
    [InlineData("if(x)return 1/0;return 9", 1)]
    [InlineData("if(x)return 9;return 1/0", 0)]
    [InlineData("if(x)return x;return 1/0", 1)]
    public async Task ConditionalReturnsMatchOrdinary(string body, int argument)
    {
        var source = "function choose(x){" + body + "} BEGIN{total=5;total=choose(" + argument + ");print total}";
        {
            var prepared = await RunAsync(source, false);
            var ordinary = await RunAsync(source, true);
            Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
            Assert.Equal(0, ordinary.PreparedFunctions);
            Assert.Equal(1, prepared.PreparedFunctions);
        }
    }

    [Theory]
    [InlineData("default", true)]
    [InlineData("syntax", false)]
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    public async Task ConditionalReturnsKeepExistingFunctionGuards(string boundary, bool prepared)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var functions = Bind("function choose(x){if(x<3)return x+2;return x-2} BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        try
        {
            var value = await functions[AwkFunctionName.FromText("choose")].InvokeAsync(
                [AwkExpression.Number(7)], new(state, output, io));
            Assert.Equal(5, value.AsNumber());
            Assert.Equal(prepared ? 1 : 0, counters.SynchronousFunctionRuns);
            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData("if(x)return 1;return 2", true)]
    [InlineData("if(x)return;return 2", false)]
    [InlineData("if(x){x++;return 1}return 2", false)]
    [InlineData("if(x)return 1;else return 2", false)]
    [InlineData("if(x){if(x)return 1}return 2", false)]
    [InlineData("if(x)return 1;return", false)]
    [InlineData("if(x)return 1;return 2;x++", false)]
    [InlineData("if((getline line)>0)return 1;return 2", false)]
    public void ConditionalReturnsPrepareOnlyTheCompleteSupportedShape(string body, bool expected)
    {
        var function = Bind("function choose(x){" + body + "} BEGIN{}").Functions[AwkFunctionName.FromText("choose")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        Assert.Equal(expected, AwkSynchronousFunction.Prepare(functions, null)?.Count == 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConditionalReturnDepthIncludesConditionAndAlternative(bool alternative)
    {
        var name = AwkFunctionName.FromText("choose");
        var value = AwkExpression.Number(1);
        var deep = new TooDeepExpression();
        var function = new AwkUserFunction(name, new([]),
            [AwkStatement.If(new([[AwkConditionClause.Truthy(alternative ? value : deep)]]),
                [AwkStatement.Return(AwkOptionalExpression.Some(value))]),
             AwkStatement.Return(AwkOptionalExpression.Some(alternative ? deep : value))]);
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [name] = function };
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>>(
            AwkSynchronousFunction.Prepare(functions, null)));
    }

    private sealed class TooDeepExpression : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => 17;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) => AwkValue.FromNumber(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConditionalReturnPendingArgumentKeepsCallerAndCancellation(bool cancel)
    {
        var functions = Bind("function choose(x){if(x<3)return x+2;return x-2} BEGIN{}").Functions;
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
        var pending = functions[AwkFunctionName.FromText("choose")].InvokeAsync(
            [new PendingOperand(completion.Task)], new(state, output, io)).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(0, counters.SynchronousFunctionRuns);
            state.AssignVariableValue(x, AwkValue.FromNumber(43));
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(7));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            else Assert.Equal(5, (await pending).AsNumber());
            Assert.Equal(43, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(cancel ? 0 : 1, counters.SynchronousFunctionRuns);
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
    public async Task ConditionalReturnRefusalPreservesCallerAndRefunds(bool branch)
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var functions = Bind("function inc(x){return x+1} function choose(x){if(x)return inc(x);return inc(x)+1} BEGIN{}").Functions;
        state.InitializeFunctions(functions);
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var frameBytes = AwkRuntimeMemory.EstimateFunctionFrameBytes() + AwkRuntimeMemory.EstimateFunctionLocalSlotBytes(
            AwkFunctionLocalSlot.Scalar(AwkVariableSlot.FromValue(AwkValue.FromNumber(1))));
        var fill = budget.Limit - state.MemoryAccounting.CurrentCommittedBytes - frameBytes;
        state.MemoryAccounting.ReserveAndCommit(fill);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await
                functions[AwkFunctionName.FromText("choose")].InvokeAsync(
                    [AwkExpression.Number(branch ? 1 : 0)], new(state, output, io)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.True(state.IsInsideFunction);
            Assert.Equal(1, counters.SynchronousFunctionRuns);
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
}
