namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("t=x*3+y;return t")]
    [InlineData("x=x*3+y;return x")]
    [InlineData("t=x;return t+1")]
    [InlineData("t=1/(x-5);return t")]
    [InlineData("t=x+1;return 1/(x-5)")]
    [InlineData("t=length(x);return t")]
    public async Task LocalAssignmentReturnsMatchOrdinary(string body)
    {
        var source = "function step(x,y,t){" + body + "} BEGIN{total=5;for(i=0;i<3;i++)total=step(total,2);print total}";
        {
            var prepared = await RunAsync(source, false);
            var ordinary = await RunAsync(source, true);
            Assert.Equal(ordinary, prepared with { PreparedFunctions = ordinary.PreparedFunctions });
            Assert.Equal(0, ordinary.PreparedFunctions);
            Assert.True(prepared.PreparedFunctions > 0);
        }
    }

    [Theory]
    [InlineData("t=x+1;return t", true)]
    [InlineData("t=x+1;return", true)]
    [InlineData("t=x+1;return t;t++", true)]
    [InlineData("t+=x;return t", true)]
    [InlineData("t++;return t", true)]
    [InlineData("global=x;return global", true)]
    [InlineData("a[x]=1;return x", false)]
    [InlineData("$1=x;return x", false)]
    [InlineData("t=(getline line);return t", false)]
    [InlineData("t=step(x);return t", false)]
    public void LocalAssignmentReturnsPrepareCompleteBoundedBodies(string body, bool expected)
    {
        var function = Bind("function step(x,t){" + body + "} BEGIN{}").Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        Assert.Equal(expected, AwkSynchronousFunction.Prepare(functions, null)?.Count == 1);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void LocalAssignmentReturnBoundsIncludeBothExpressions(bool returned, bool calls)
    {
        var name = AwkFunctionName.FromText("step");
        var local = AwkVariableName.FromText("t");
        var value = AwkExpression.Number(1);
        AwkExpression deep = calls ? new PrefixDeepCallExpression() : new TooDeepExpression();
        var function = new AwkUserFunction(name, new([local]),
            [AwkStatement.Assignment(AwkReference.LocalScalar(local, new(0)), returned ? value : deep),
             AwkStatement.Return(AwkOptionalExpression.Some(returned ? deep : value))]);
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [name] = function };
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>>(
            AwkSynchronousFunction.Prepare(functions, null)));
    }

    [Theory]
    [InlineData("default", true)]
    [InlineData("syntax", false)]
    [InlineData("lint", false)]
    [InlineData("pending", false)]
    [InlineData("captured-regex", false)]
    public async Task LocalAssignmentReturnsKeepExistingGuardsAndFieldDiagnostics(string boundary, bool prepared)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        var functions = Bind("function step(x,t){t=$1+x;return t} BEGIN{}").Functions;
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
            Assert.Equal(prepared ? 1 : 0, counters.SynchronousFunctionRuns);
            Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally { await io.CloseAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalAssignmentReturnPendingArgumentKeepsCaller(bool cancel)
    {
        var functions = Bind("function step(x,t){t=x+2;return t} BEGIN{}").Functions;
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
            Assert.Equal(0, counters.SynchronousFunctionRuns);
            if (cancel) cancellation.Cancel();
            completion.SetResult(AwkValue.FromNumber(7));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            else Assert.Equal(9, (await pending).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
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
    [InlineData(1, 0, true)]
    [InlineData(2, 1, true)]
    [InlineData(4, 1, true)]
    [InlineData(5, 1, true)]
    [InlineData(65, 1, true)]
    [InlineData(2, 3, true)]
    [InlineData(2, 2, true)]
    public void LocalAssignmentReturnCallsSupportOmittedLocalsAndExcessArguments(int slots, int arguments, bool expected)
    {
        var parameters = string.Join(",", Enumerable.Range(0, slots).Select(i => i == 0 ? "x" : "p" + i));
        var function = Bind("function step(" + parameters + "){x=1;return x} BEGIN{}").Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        var prepared = Assert.IsAssignableFrom<IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>>(
            AwkSynchronousFunction.Prepare(functions, null));
        var operands = Enumerable.Range(0, arguments).Select(_ => AwkExpression.Number(1)).ToArray();
        Assert.Equal(expected, AwkExpression.FunctionCall(function.FunctionName, operands)
            .TryPrepareFunctionCalls(prepared, 16, out _));
    }
    [Fact]
    public async Task LocalAssignmentReturnNestedFrameRefusalKeepsCallerAndRefunds()
    {
        var budget = new CooperativeTestBudget(2L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var functions = Bind("function inc(x){return x+1} function step(x){x=inc(x);return x} BEGIN{}").Functions;
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

    [Fact]
    public async Task LocalAssignmentReturnSharedRefusalLeavesLocalUnchangedWithoutRetry()
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var function = Bind("function step(x,t){t=\"" + new string('a', 4096) + "\";return t} BEGIN{}")
            .Functions[AwkFunctionName.FromText("step")];
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction> { [function.FunctionName] = function };
        var prepared = Assert.IsAssignableFrom<IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>>(
            AwkSynchronousFunction.Prepare(functions, null))[function.FunctionName];
        state.InitializeFunctions(functions);
        state.PushFunctionFrame(function.LocalLayout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7))]);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            var error = Assert.Throws<AwkRuntimeException>(() => prepared.EvaluateBody(new(state)));
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

    private sealed class PrefixDeepCallExpression : AwkSynchronousExpression
    {
        internal override int SynchronousCallDepth => 4;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) => AwkValue.FromNumber(1);
    }
}
