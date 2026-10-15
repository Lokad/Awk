namespace Lokad.Awk.Tests;

public sealed class AwkSingleCallArgumentTests
{
    [Fact]
    public async Task PendingArgumentRunsInCallerAndCallerFrameIsRestored()
    {
        var state = State(10);
        var name = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([name]);
        var function = new AwkUserFunction(AwkFunctionName.FromText("inner"), layout, []);
        state.PushFunctionFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        var entered = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new Operand(async context =>
        {
            entered.SetResult(context.State.ResolveVariableValue(name).AsNumber());
            return await completion.Task.ConfigureAwait(false);
        });
        var pending = function.InvokeAsync([operand], new(state)).AsTask();
        try
        {
            Assert.Equal(42, await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.False(pending.IsCompleted);
            Assert.Equal(42, state.ResolveVariableValue(name).AsNumber());
            state.AssignVariableValue(name, AwkValue.FromNumber(43));
            completion.SetResult(AwkValue.FromNumber(7));
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(state.IsInsideFunction);
            Assert.Equal(43, state.ResolveVariableValue(name).AsNumber());
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            completion.TrySetResult(AwkValue.FromNumber(7));
            await pending;
            state.PopFunctionFrame();
        }
        Assert.False(state.IsInsideFunction);
    }

    [Fact]
    public async Task FrameDepthRefusalOccursAfterArgumentWithoutMutatingCaller()
    {
        var state = State(1);
        var name = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([name]);
        var function = new AwkUserFunction(AwkFunctionName.FromText("inner"), layout, []);
        state.PushFunctionFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        try
        {
            var committed = state.MemoryAccounting.CurrentCommittedBytes;
            var effects = 0;
            var operand = new Operand(context =>
            {
                effects++;
                Assert.Equal(42, context.State.ResolveVariableValue(name).AsNumber());
                return ValueTask.FromResult(AwkValue.FromNumber(7));
            });
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await function.InvokeAsync([operand], new(state)));
            Assert.Equal(Lokad.Awk.AwkMessages.ErrorFunctionRecursionLimit.ToArray(), error.MessageBytes.ToArray());
            Assert.Equal(1, effects);
            Assert.True(state.IsInsideFunction);
            Assert.Equal(42, state.ResolveVariableValue(name).AsNumber());
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            state.PopFunctionFrame();
        }
        Assert.False(state.IsInsideFunction);
    }

    private sealed class Operand(Func<AwkEvaluationContext, ValueTask<AwkValue>> evaluate) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => evaluate(context);
    }

    private static AwkRuntimeState State(int depth)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(depth)
            .WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }
}
