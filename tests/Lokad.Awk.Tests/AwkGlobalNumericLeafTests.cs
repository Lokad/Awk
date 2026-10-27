namespace Lokad.Awk.Tests;

public sealed class AwkGlobalNumericLeafTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyBoundNumbersUseTheirCurrentNumericValue(bool slots)
    {
        using var memoryForState = State(slots: slots);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        var expression = Leaf(name);
        var context = new AwkEvaluationContext(state);
        Assert.Equal(0, expression.ResolveNumberSynchronous(context));
        Assert.Equal(slots, state.HasBoundGlobalScalar(0));
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        Assert.Equal(7, expression.ResolveNumberSynchronous(context));
        state.AssignVariableValue(name, AwkValue.FromNumericString("007"));
        Assert.Equal(7, expression.ResolveNumberSynchronous(context));
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(slots ? 1 : 0, state.Counters.NumericGlobalReads);
        state.AssignVariableValue(name, AwkValue.FromNumber(9));
        Assert.Equal(9, expression.ResolveNumberSynchronous(context));
        Assert.Equal(slots ? 2 : 0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void ComputedNfKeepsCurrentAndAssignedValues()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("NF");
        var expression = Leaf(name);
        var context = new AwkEvaluationContext(state);
        state.SetCurrentRecord(AwkText.FromString("old tail"));
        Assert.Equal(2, expression.ResolveNumberSynchronous(context));
        state.SetCurrentRecord(AwkText.FromString("next has three"));
        Assert.Equal(3, expression.ResolveNumberSynchronous(context));
        state.AssignVariableValue(name, AwkValue.FromNumber(1.5));
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(1.5, expression.ResolveNumberSynchronous(context));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void ArrayFailureDoesNotPublishAScalarBinding()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        _ = state.GetArrayAwkValue(AwkArrayName.FromText("x"), AwkArrayKey.FromOrdinal(1));
        var error = Assert.Throws<AwkRuntimeException>(() => Leaf(AwkVariableName.FromText("x"))
            .ResolveNumberSynchronous(new(state)));
        Assert.Equal("awk: attempt to use array `x' in a scalar context\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundNumbersRetainTheExpressionCancellationCheckpoint(bool numericAsync)
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(token: cancellation.Token);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        _ = state.ResolvePreparedGlobalValue(name, 0);
        cancellation.Cancel();
        if (numericAsync)
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await Leaf(name).ResolveNumberAsync(new(state)));
        else
            Assert.Throws<OperationCanceledException>(() => Leaf(name).ResolveNumberSynchronous(new(state)));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.0)]
    [InlineData(1.25)]
    public void BoundNumbersPreserveTheirBinary64Value(double value)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(value));
        _ = state.ResolvePreparedGlobalValue(name, 0);
        var actual = Leaf(name).ResolveNumberSynchronous(new(state));
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(actual));
        Assert.Equal(1, state.Counters.NumericGlobalReads);
    }

    private static AwkSynchronousExpression Leaf(AwkVariableName name) =>
        Assert.IsAssignableFrom<AwkSynchronousExpression>(AwkExpression.PreparedGlobalVariable(name, 0));

    private static CooperativeTestMemory State(AwkRuntimeLimits? limits = null, bool slots = true,
        CancellationToken token = default)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), (limits ?? AwkRuntimeLimits.CreateDefault()), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options));
        if (slots) state.InitializeGlobalReadSlots(1);
        return testMemory.Initialize(() => state);
    }
}
