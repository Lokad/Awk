namespace Lokad.Awk.Tests;

public sealed class AwkBoundScalarLengthTests
{
    [Fact]
    public async Task BoundReadsKeepCurrentValuesAndRequireMatchingNames()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("s");
        var array = AwkArrayName.FromText("a");
        state.AssignPreparedGlobalValue(name, 0, AwkValue.FromText("abc"));
        state.SetArrayAwkValue(array, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var scalar = AwkExpression.PreparedGlobalVariable(name, 0);
        var expression = AwkExpression.Length(scalar, AwkOptionalIdentifier.Some(name.Identifier));
        var context = new AwkEvaluationContext(state);

        Assert.Equal(3, (await expression.EvaluateAsync(context)).AsNumber());
        state.AssignVariableValue(name, AwkValue.FromText("updated"));
        Assert.Equal(7, (await expression.EvaluateAsync(context)).AsNumber());
        Assert.Equal(2, state.Counters.BoundScalarLengths);

        var mismatched = AwkExpression.Length(scalar, AwkOptionalIdentifier.Some(array.Identifier));
        Assert.Equal(1, (await mismatched.EvaluateAsync(context)).AsNumber());
        var arrayExpression = AwkExpression.Length(
            AwkExpression.PreparedGlobalVariable(new(array.Identifier), 1), AwkOptionalIdentifier.Some(array.Identifier));
        Assert.Equal(1, (await arrayExpression.EvaluateAsync(context)).AsNumber());
        Assert.Equal(2, state.Counters.BoundScalarLengths);
        Assert.False(state.HasBoundGlobalScalar(1));
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task FirstMissingReadKeepsNormalResolutionBeforeBinding()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("missing");
        var expression = AwkExpression.Length(AwkExpression.PreparedGlobalVariable(name, 0),
            AwkOptionalIdentifier.Some(name.Identifier));
        var context = new AwkEvaluationContext(state);
        Assert.False(state.HasBoundGlobalScalar(0));
        Assert.Equal(0, (await expression.EvaluateAsync(context)).AsNumber());
        Assert.Equal(0, state.Counters.BoundScalarLengths);
        Assert.True(state.HasBoundGlobalScalar(0));
        Assert.Equal(0, (await expression.EvaluateAsync(context)).AsNumber());
        Assert.Equal(1, state.Counters.BoundScalarLengths);
    }

    private static CooperativeTestMemory State(AwkRuntimeLimits? limits = null)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), (limits ?? AwkRuntimeLimits.CreateDefault()));
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        state.InitializeGlobalReadSlots(2);
        return testMemory.Initialize(() => state);
    }
}
