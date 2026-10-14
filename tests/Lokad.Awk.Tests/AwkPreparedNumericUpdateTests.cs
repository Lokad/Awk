namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumericUpdateTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task PreparedUpdatesPreserveLegacyNumericResultsAndAccounting(bool decrement, bool post, bool vector)
    {
        var name = AwkVariableName.FromText("value");
        var prepared = State();
        var legacy = State();
        foreach (var state in new[] { prepared, legacy })
        {
            if (vector) state.InitializeGlobalReadSlots(1);
            state.AssignVariable(name, "007");
        }
        var expected = await Update(AwkReference.Scalar(name), legacy, decrement, post);
        var pending = Update(AwkReference.PreparedGlobalScalar(name, 0), prepared, decrement, post);
        Assert.True(pending.IsCompletedSuccessfully);
        var actual = await pending;
        Assert.True(actual.IsNumberValue);
        Assert.Equal(post ? 7 : decrement ? 6 : 8, actual.AsNumber());
        Assert.Equal(expected.AsNumber(), actual.AsNumber());
        Assert.Equal(legacy.ResolveVariableValue(name).AsNumber(), prepared.ResolveVariableValue(name).AsNumber());
        Assert.Equal(legacy.Counters.ValuesCreated, prepared.Counters.ValuesCreated);
        Assert.Equal(legacy.Counters.ExecutionSteps, prepared.Counters.ExecutionSteps);
        Assert.Equal(legacy.Counters.StringDecodes, prepared.Counters.StringDecodes);
        Assert.Equal(legacy.Counters.Utf8Encodes, prepared.Counters.Utf8Encodes);
        Assert.Equal(legacy.MemoryAccounting.CurrentCommittedBytes, prepared.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, prepared.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public async Task FailedFirstUpdateDoesNotAdmitOrBindTheScalar()
    {
        var state = State();
        state.InitializeGlobalReadSlots(1);
        var name = AwkVariableName.FromText("missing");
        var target = AwkReference.PreparedGlobalScalar(name, 0);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient((1 << 20) - before))
        {
            var pressured = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.Throws<AwkRuntimeException>(() => target.ApplyIncrementAsync(new AwkEvaluationContext(state)));
            Assert.False(state.HasScalar(name));
            Assert.Equal(pressured, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(1, (await target.ApplyIncrementAsync(new AwkEvaluationContext(state))).AsNumber());
        Assert.Equal(1, state.ResolvePreparedGlobalValue(name, 0).AsNumber());
    }

    [Fact]
    public void ArrayTypeFailurePreservesElementsAndAdmission()
    {
        var state = State();
        state.InitializeGlobalReadSlots(1);
        var name = AwkVariableName.FromText("values");
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(array, key, "kept");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var target = AwkReference.PreparedGlobalScalar(name, 0);
        Assert.Throws<AwkRuntimeException>(() => target.ApplyPostIncrementAsync(new AwkEvaluationContext(state)));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.False(state.HasScalar(name));
        Assert.Equal("kept", state.GetArrayAwkValue(array, key).AsString());
    }

    [Fact]
    public void LongInputCancellationPrecedesMutation()
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(cancellation.Token);
        state.InitializeGlobalReadSlots(1);
        var name = AwkVariableName.FromText("value");
        state.AssignVariable(name, "1" + new string('0', 8000));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        var target = AwkReference.PreparedGlobalScalar(name, 0);
        var error = Assert.Throws<OperationCanceledException>(() => target.ApplyIncrementAsync(new AwkEvaluationContext(state)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static ValueTask<AwkValue> Update(AwkReference target, AwkRuntimeState state, bool decrement, bool post) =>
        (decrement, post) switch
        {
            (true, true) => target.ApplyPostDecrementAsync(new AwkEvaluationContext(state)),
            (true, false) => target.ApplyDecrementAsync(new AwkEvaluationContext(state)),
            (false, true) => target.ApplyPostIncrementAsync(new AwkEvaluationContext(state)),
            _ => target.ApplyIncrementAsync(new AwkEvaluationContext(state))
        };

    private static AwkRuntimeState State(CancellationToken token = default)
    {
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
    }
}
