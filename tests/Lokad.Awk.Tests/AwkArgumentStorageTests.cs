namespace Lokad.Awk.Tests;

public sealed class AwkArgumentStorageTests
{
    [Fact]
    public void TypedStorageClearsPayloadAndKeepsOnlyInactiveCapacityFunded()
    {
        var fixture = State(CancellationToken.None);
        var budget = fixture.Budget;
        try
        {
            var state = fixture.State;
            var baseline = state.MemoryAccounting.CurrentCommittedBytes;
            var text = AwkValue.FromText(new string('x', 4096));
            AwkFunctionArgument[] values;
            using (var arguments = state.RentFunctionArguments(5))
            {
                values = arguments.Values;
                arguments.Retain(text);
                values[0] = AwkFunctionArgument.Scalar(text);
                using var nested = state.RentFunctionArguments(5);
                Assert.NotSame(values, nested.Values);
                using var builtin = AwkValueBuffer.Rent(5, state);
                builtin.Set(0, AwkValue.FromNumber(19));
                Assert.Equal(text, values[0].Value);
            }
            Assert.All(values, value => Assert.Equal(default, value));
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateFunctionArgumentArrayBytes(5)
                + AwkRuntimeMemory.EstimateAwkValueArrayBytes(5), state.MemoryAccounting.CurrentCommittedBytes);
            using (var reused = state.RentFunctionArguments(5))
                Assert.All(reused.Values, value => Assert.Equal(default, value));
            Assert.Equal(2, state.Counters.GovernedFunctionArgumentBufferAllocations);
            Assert.Equal(1, state.Counters.GovernedFunctionArgumentBufferReuses);
        }
        finally { fixture.Dispose(); }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task NestedUserArgumentsStayInCallerScopeAndFundedThroughActualCompletion(string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = State(cancellation.Token);
        var budget = fixture.Budget;
        var state = fixture.State;
        var x = AwkVariableName.FromText("x");
        state.PushFunctionFrame(new([x]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var entries = new List<int>();
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var layout = new AwkFunctionLocalLayout(Enumerable.Range(0, 5).Select(i => AwkVariableName.FromText("p" + i)).ToArray());
        var inner = new AwkUserFunction(AwkFunctionName.FromText("inner"), layout, []);
        var outer = new AwkUserFunction(AwkFunctionName.FromText("outer"), layout, []);
        var text = AwkValue.FromText(new string('x', 32_768));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var arguments = Enumerable.Range(0, 5).Select(index => (AwkExpression)new Operand(async context =>
        {
            entries.Add(index);
            Assert.Equal(42, context.State.ResolveVariableValue(x).AsNumber());
            if (index == 0) return text;
            if (index == 2)
            {
                await inner.InvokeAsync(Enumerable.Range(0, 5).Select(i =>
                    (AwkExpression)AwkExpression.Number(i)).ToArray(), context);
                entered.TrySetResult();
                return await completion.Task;
            }
            return AwkValue.FromNumber(index);
        })).ToArray();
        var pending = outer.InvokeAsync(arguments, new(state)).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(new[] { 0, 1, 2 }, entries);
            Assert.False(pending.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= baseline + text.RetainedTextMemoryBytes);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(pending.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= baseline + text.RetainedTextMemoryBytes);
            if (outcome == "success")
            {
                completion.SetResult(AwkValue.FromNumber(7));
                await pending.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(new[] { 0, 1, 2, 3, 4 }, entries);
            }
            else if (outcome == "fault")
            {
                completion.SetException(new InvalidOperationException("operand failed"));
                await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(new[] { 0, 1, 2 }, entries);
            }
            else
            {
                completion.SetCanceled(cancellation.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(new[] { 0, 1, 2 }, entries);
            }
            Assert.Equal(42, state.ResolveVariableValue(x).AsNumber());
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes < baseline + text.RetainedTextMemoryBytes);
            Assert.True(state.IsInsideFunction);
        }
        finally
        {
            completion.TrySetCanceled();
            try { await pending.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (InvalidOperationException) when (outcome == "fault") { }
            catch (OperationCanceledException) { }
            state.PopFunctionFrame();
            fixture.Dispose();
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationRequestedByColdAdmissionPrecedesLargeArgumentAllocation(bool userFunction)
    {
        using var cancellation = new CancellationTokenSource();
        const int count = 10_000;
        var parameters = string.Join(',', Enumerable.Range(0, count).Select(i => "p" + i));
        var arguments = "effect()," + string.Join(',', Enumerable.Repeat("0", count - 1));
        var definitions = "function effect(){print \"operand\";return 1}";
        var source = userFunction
            ? definitions + "function f(" + parameters + "){print \"body\"}BEGIN{print \"before\";f("
                + arguments + ");print \"after\"}END{print \"end\"}"
            : definitions + "BEGIN{print \"before\";printf \"%d\"," + arguments
                + ";print \"after\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(128L << 20);
        long before = 0;
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n") return;
            before = GC.GetAllocatedBytesForCurrentThread();
            budget.OnGrowth = _ => cancellation.Cancel();
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tool.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 128 * 1024);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Refusals);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    private sealed class Operand(Func<AwkEvaluationContext, ValueTask<AwkValue>> evaluate) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => evaluate(context);
    }

    private static CooperativeTestMemory State(CancellationToken cancellationToken)
    {
        var fixture = new CooperativeTestMemory(4 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            cancellationToken, fixture.Configure(options)));
    }
}
