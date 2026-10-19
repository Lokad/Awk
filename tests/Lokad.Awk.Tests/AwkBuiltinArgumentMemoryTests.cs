namespace Lokad.Awk.Tests;

public sealed class AwkBuiltinArgumentMemoryTests
{
    private const int Limit = 1 << 20;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StagingEvaluatesOnceInOrderAcrossSuspension(int pendingIndex)
    {
        var state = State(observed: true, CancellationToken.None);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var entries = new List<int>();
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        AwkExpression[] arguments = Enumerable.Range(0, 3).Select(index => (AwkExpression)new ObservedOperand(() =>
        {
            entries.Add(index);
            return index == pendingIndex ? new(completion.Task) : ValueTask.FromResult(AwkValue.FromNumber(index + 7));
        })).ToArray();
        var evaluations = 0;
        var builtin = AwkBuiltinImplementation.Sync(context =>
        {
            evaluations++;
            Assert.Equal(new[] { 0, 1, 2 }, entries);
            Assert.Equal(3, context.ArgumentCount);
            Assert.Equal(7, context.GetNumber(0));
            Assert.Equal(8, context.GetNumber(1));
            Assert.Equal(9, context.GetNumber(2));
            return AwkValue.FromNumber(24);
        });
        var pending = builtin.EvaluateAsync(arguments, new(state)).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(Enumerable.Range(0, pendingIndex + 1), entries);
        Assert.Equal(0, evaluations);
        Assert.Equal(baseline + AwkRuntimeMemory.EstimateAwkValueArrayBytes(3), state.MemoryAccounting.CurrentCommittedBytes);
        completion.SetResult(AwkValue.FromNumber(pendingIndex + 7));
        Assert.Equal(24, (await pending).AsNumber());
        Assert.Equal(1, evaluations);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuiltinArgumentCapacityShortfallPrecedesMutationAndAllowsRetry(bool observed)
    {
        var state = State(observed, CancellationToken.None);
        var name = AwkVariableName.FromText("count");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        Assert.True(AwkBuiltins.TryGetCallable("sqrt", out var builtin));
        AwkExpression[] arguments = [AwkExpression.PreIncrement(AwkReference.Scalar(name))];
        var context = new AwkEvaluationContext(state);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - AwkRuntimeMemory.EstimateAwkValueArrayBytes(1) + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await builtin.EvaluateAsync(arguments, context));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, state.ResolveVariableValue(name).AsNumber());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        var retry = await builtin.EvaluateAsync(arguments, context);
        Assert.Equal(1, retry.AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BuiltinArgumentsRemainChargedWhileAnOperandIsPending(bool observed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(observed, cancellation.Token);
        var operand = new PendingOperand(cancellation.Token);
        Assert.True(AwkBuiltins.TryGetCallable("sqrt", out var builtin));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var pending = builtin.EvaluateAsync([operand], new AwkEvaluationContext(state)).AsTask();
        try
        {
            await operand.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(pending.IsCompleted);
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateAwkValueArrayBytes(1), state.MemoryAccounting.CurrentCommittedBytes);
            if (cancel)
            {
                cancellation.Cancel();
                var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(cancellation.Token, failure.CancellationToken);
            }
            else
            {
                operand.Allow.TrySetResult();
                Assert.Equal(2, (await pending.WaitAsync(TimeSpan.FromSeconds(10))).AsNumber());
            }
            Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            operand.Allow.TrySetResult();
            try { await pending.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    private sealed class PendingOperand(CancellationToken token) : AwkExpression
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Allow { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Entered.TrySetResult();
            await Allow.Task.WaitAsync(token).ConfigureAwait(false);
            return AwkValue.FromNumber(4);
        }
    }

    private sealed class ObservedOperand(Func<ValueTask<AwkValue>> evaluate) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => evaluate();
    }

    private static AwkRuntimeState State(bool observed, CancellationToken token)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(Limit),
            AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
    }
}
