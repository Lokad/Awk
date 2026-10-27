using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed class AwkPreparedCompoundUpdateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlainAssignmentWaitsForRightAndConsumesOnce(bool pending)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "007");
        var source = new ValueSource(3, pending);
        var right = new EffectExpression(source, () => state.AssignVariableValue(name, AwkValue.FromNumber(100)));
        var expression = AwkExpression.Assignment(AwkReference.PreparedGlobalScalar(name, 0), right);
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pending)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(100, state.ResolveVariableValue(name).AsNumber());
            source.Complete();
        }
        Assert.Equal(3, (await result).AsNumber());
        Assert.Equal(3, state.ResolvePreparedGlobalValue(name, 0).AsNumber());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PlainRightFailurePreservesTheTarget(bool pending, bool canceled)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "007");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(3, pending: true);
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("test RHS failure"u8.ToArray());
        if (!pending) source.Fail(error);
        var expression = AwkExpression.Assignment(AwkReference.PreparedGlobalScalar(name, 0),
            new EffectExpression(source, () => { }));
        async Task<AwkValue> EvaluateAsync() => await expression.EvaluateAsync(new AwkEvaluationContext(state));
        var result = EvaluateAsync();
        if (pending)
        {
            Assert.False(result.IsCompleted);
            source.Fail(error);
        }
        var observed = await Record.ExceptionAsync(() => result);
        if (canceled) Assert.IsAssignableFrom<OperationCanceledException>(observed);
        else Assert.Same(error, observed);
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task PlainFirstAdmissionFailureDoesNotBindTheTarget()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("missing");
        var reference = AwkReference.PreparedGlobalScalar(name, 0);
        var source = new ValueSource(3, pending: false);
        var expression = AwkExpression.Assignment(reference, new EffectExpression(source, () => { }));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient((1 << 20) - before))
        {
            var pressured = state.MemoryAccounting.CurrentCommittedBytes;
            async Task<AwkValue> EvaluateAsync() => await expression.EvaluateAsync(new AwkEvaluationContext(state));
            await Assert.ThrowsAsync<AwkRuntimeException>(EvaluateAsync);
            Assert.Equal(1, source.Consumptions);
            Assert.False(state.HasScalar(name));
            Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        await reference.AssignValueAsync(new AwkEvaluationContext(state), AwkValue.FromNumber(5));
        Assert.Equal(5, state.ResolvePreparedGlobalValue(name, 0).AsNumber());
    }

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
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "007");
        var source = new ValueSource(3, pending);
        var right = new EffectExpression(source, () => state.AssignVariableValue(name, AwkValue.FromNumber(100)));
        var expression = AwkExpression.CompoundAssignment(AwkReference.PreparedGlobalScalar(name, 0), right, (AwkCompoundAssignmentOperation)operation);
        var value = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pending)
        {
            Assert.False(value.IsCompleted);
            Assert.Equal(100, state.ResolveVariableValue(name).AsNumber());
            source.Complete();
        }
        var result = await value;
        Assert.True(result.IsNumberValue);
        Assert.Equal(expected, result.AsNumber());
        Assert.Equal(expected, state.ResolvePreparedGlobalValue(name, 0).AsNumber());
        Assert.Equal(1, source.Consumptions);
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
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "007");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(3, pending: true);
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("test RHS failure"u8.ToArray());
        if (!pending) source.Fail(error);
        var result = AwkReference.PreparedGlobalScalar(name, 0).ApplyCompoundAssignmentAsync(
            new AwkEvaluationContext(state), new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Add);
        if (pending)
        {
            Assert.False(result.IsCompleted);
            source.Fail(error);
        }
        var observed = await Record.ExceptionAsync(() => result.AsTask());
        if (canceled) Assert.IsAssignableFrom<OperationCanceledException>(observed);
        else Assert.Same(error, observed);
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DivisionFailureDoesNotPublishTheResult(bool pending)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "007");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(0, pending);
        var result = ApplyAsync();
        async Task<AwkValue> ApplyAsync() => await AwkReference.PreparedGlobalScalar(name, 0).ApplyCompoundAssignmentAsync(
            new AwkEvaluationContext(state), new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Divide);
        if (pending) source.Complete();
        await Assert.ThrowsAsync<AwkRuntimeException>(() => result);
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task FailedFirstAdmissionStopsRightEvaluationAndDoesNotBind()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("missing");
        var target = AwkReference.PreparedGlobalScalar(name, 0);
        var source = new ValueSource(3, pending: false);
        var right = new EffectExpression(source, () => { });
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient((1 << 20) - before))
        {
            var pressured = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.Throws<AwkRuntimeException>(() => target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), right, AwkCompoundAssignmentOperation.Add));
            Assert.False(source.Started);
            Assert.False(state.HasScalar(name));
            Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(3, (await target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state), right, AwkCompoundAssignmentOperation.Add)).AsNumber());
        Assert.Equal(1, source.Consumptions);
    }

    [Fact]
    public void ArrayTypeErrorStopsRightEvaluationWithoutMutation()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("values");
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(array, key, "kept");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new ValueSource(3, pending: false);
        var target = AwkReference.PreparedGlobalScalar(name, 0);
        Assert.Throws<AwkRuntimeException>(() => target.ApplyCompoundAssignmentAsync(new AwkEvaluationContext(state),
            new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Add));
        Assert.False(source.Started);
        Assert.False(state.HasScalar(name));
        Assert.Equal("kept", state.GetArrayAwkValue(array, key).AsString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
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

    private static CooperativeTestMemory State()
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        state.InitializeGlobalReadSlots(1);
        return testMemory.Initialize(() => state);
    }
}
