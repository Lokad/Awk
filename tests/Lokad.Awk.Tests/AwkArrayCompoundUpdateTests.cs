using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    private static readonly AwkArrayKey Key = new("k");
    private static readonly AwkArrayIndex Index = AwkArrayIndex.Simple(AwkExpression.Literal("k"));

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(0, 10, true)]
    [InlineData(1, 4, false)]
    [InlineData(1, 4, true)]
    [InlineData(2, 21, false)]
    [InlineData(2, 21, true)]
    [InlineData(3, 7.0 / 3, false)]
    [InlineData(3, 7.0 / 3, true)]
    [InlineData(4, 1, false)]
    [InlineData(4, 1, true)]
    [InlineData(5, 343, false)]
    [InlineData(5, 343, true)]
    public async Task CapturesLeftValueBeforeRightEffectsAndConsumesOnce(int operation, double expected, bool pending)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(name, Key, AwkValue.FromText("007"));
        var source = new ValueSource(3, pending);
        var right = new EffectExpression(source, () => state.SetArrayAwkValue(name, Key, AwkValue.FromNumber(100)));
        var expression = AwkExpression.CompoundAssignment(AwkReference.ArrayElement(name, Index), right,
            (AwkCompoundAssignmentOperation)operation);
        var steps = state.Counters.ExecutionSteps;
        var value = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pending)
        {
            Assert.False(value.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, Key).AsNumber());
            source.Complete();
        }
        var result = await value;
        Assert.True(result.IsNumberValue);
        Assert.Equal(expected, result.AsNumber());
        Assert.Equal(expected, state.GetArrayAwkValue(name, Key).AsNumber());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(3, state.Counters.ExecutionSteps - steps);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RightFailurePreservesTheTarget(bool pending, bool canceled)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(name, Key, AwkValue.FromText("007"));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(3, pending: true);
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("test RHS failure"u8.ToArray());
        if (!pending) source.Fail(error);
        var result = AwkReference.ArrayElement(name, Index).ApplyCompoundAssignmentAsync(
            new AwkEvaluationContext(state), new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Add);
        if (pending)
        {
            Assert.False(result.IsCompleted);
            source.Fail(error);
        }
        var observed = await Record.ExceptionAsync(() => result.AsTask());
        if (canceled) Assert.IsAssignableFrom<OperationCanceledException>(observed);
        else Assert.Same(error, observed);
        Assert.Equal("007", state.GetArrayAwkValue(name, Key).AsString());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DivisionFailureDoesNotPublishTheResult(bool pending)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(name, Key, AwkValue.FromText("007"));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(0, pending);
        var result = ApplyAsync();
        async Task<AwkValue> ApplyAsync() => await AwkReference.ArrayElement(name, Index).ApplyCompoundAssignmentAsync(
            new AwkEvaluationContext(state), new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Divide);
        if (pending) source.Complete();
        await Assert.ThrowsAsync<AwkRuntimeException>(() => result);
        Assert.Equal("007", state.GetArrayAwkValue(name, Key).AsString());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task KeyFinishesBeforeRightEffectsAndIsConsumedOnce(bool keyPending, bool rightPending)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var keySource = new ValueSource(1, keyPending);
        var rightSource = new ValueSource(3, rightPending);
        var rightStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = AwkReference.ArrayElement(name, AwkArrayIndex.Simple(new EffectExpression(keySource, () => { })));
        var right = new EffectExpression(rightSource, () =>
        {
            state.SetArrayAwkValue(name, key, AwkValue.FromNumber(100));
            rightStarted.SetResult();
        });
        var result = target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), right, AwkCompoundAssignmentOperation.Add);
        if (keyPending)
        {
            Assert.False(result.IsCompleted);
            Assert.False(rightStarted.Task.IsCompleted);
            Assert.Equal(7, state.GetArrayAwkValue(name, key).AsNumber());
            keySource.Complete();
        }
        await rightStarted.Task;
        if (rightPending)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, key).AsNumber());
            rightSource.Complete();
        }
        Assert.Equal(10, (await result).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.Equal(1, keySource.Consumptions);
        Assert.Equal(1, rightSource.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task KeyFailureStopsRightEvaluation(bool pending, bool canceled)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(name, Key, AwkValue.FromNumber(7));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var keySource = new ValueSource(1, pending: true);
        var rightSource = new ValueSource(3, pending: false);
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("test key failure"u8.ToArray());
        if (!pending) keySource.Fail(error);
        var target = AwkReference.ArrayElement(name, AwkArrayIndex.Simple(new EffectExpression(keySource, () => { })));
        var result = target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state),
            new EffectExpression(rightSource, () => { }), AwkCompoundAssignmentOperation.Add);
        if (pending)
        {
            Assert.False(result.IsCompleted);
            keySource.Fail(error);
        }
        var observed = await Record.ExceptionAsync(() => result.AsTask());
        if (canceled) Assert.IsAssignableFrom<OperationCanceledException>(observed);
        else Assert.Same(error, observed);
        Assert.False(rightSource.Started);
        Assert.Equal(1, keySource.Consumptions);
        Assert.Equal(7, state.GetArrayAwkValue(name, Key).AsNumber());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task FailedFirstAdmissionStopsRightEvaluationAndDoesNotPublish()
    {
        var state = State();
        var name = AwkArrayName.FromText("missing");
        var target = AwkReference.ArrayElement(name, Index);
        var source = new ValueSource(3, pending: false);
        var right = new EffectExpression(source, () => { });
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient((1 << 20) - before))
        {
            var pressured = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.Throws<AwkRuntimeException>(() => target.ApplyCompoundAssignmentAsync(
                new AwkEvaluationContext(state), right, AwkCompoundAssignmentOperation.Add));
            Assert.False(source.Started);
            Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.False(state.ContainsArrayValue(name, Key));
        Assert.Equal(3, (await target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), right,
            AwkCompoundAssignmentOperation.Add)).AsNumber());
        Assert.Equal(1, source.Consumptions);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BorrowedFieldTargetSurvivesPendingIndexRightDeletionAndRecordReuse(bool keyPending, bool rightPending)
    {
        var state = State();
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(keyPending ? "miss old" : "hit old"));
        var keySource = new ValueSource(1, keyPending);
        var rightSource = new ValueSource(3, rightPending);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = AwkReference.ArrayElement(name, AwkArrayIndex.Simple(
            AwkExpression.FieldIndex(new EffectExpression(keySource, () => { }))));
        var right = new EffectExpression(rightSource, () =>
        {
            state.DeleteArrayValue(name, key);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
            state.SetArrayAwkValue(name, key, AwkValue.FromNumber(100));
            started.SetResult();
        });
        var result = target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), right, AwkCompoundAssignmentOperation.Add);
        if (keyPending)
        {
            Assert.False(started.Task.IsCompleted);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit after"));
            keySource.Complete();
        }
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (rightPending)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, key).AsNumber());
            rightSource.Complete();
        }
        Assert.Equal(10, (await result).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("other")));
        Assert.Equal(1, keySource.Consumptions);
        Assert.Equal(1, rightSource.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private sealed class EffectExpression(ValueSource source, Action effect) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            effect();
            return source.Start();
        }
    }

    private sealed class ValueSource(double value, bool pending) : IValueTaskSource<AwkValue>
    {
        private ManualResetValueTaskSourceCore<AwkValue> _source = CreateSource(value, pending);
        public bool Started { get; private set; }
        public int Consumptions { get; private set; }
        private static ManualResetValueTaskSourceCore<AwkValue> CreateSource(double value, bool pending)
        {
            var source = new ManualResetValueTaskSourceCore<AwkValue> { RunContinuationsAsynchronously = true };
            if (!pending) source.SetResult(AwkValue.FromNumber(value));
            return source;
        }
        public ValueTask<AwkValue> Start()
        {
            Assert.False(Started);
            Started = true;
            return new(this, _source.Version);
        }
        public void Complete() => _source.SetResult(AwkValue.FromNumber(value));
        public void Fail(Exception error) => _source.SetException(error);
        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _source.OnCompleted(continuation, state, token, flags);
        public AwkValue GetResult(short token)
        {
            Assert.Equal(0, Consumptions++);
            return _source.GetResult(token);
        }
    }

    private static AwkRuntimeState State()
    {
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        return state;
    }
}
