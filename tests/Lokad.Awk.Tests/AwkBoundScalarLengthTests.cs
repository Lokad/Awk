namespace Lokad.Awk.Tests;

public sealed class AwkBoundScalarLengthTests
{
    [Fact]
    public async Task BoundReadsKeepCurrentValuesAndRequireMatchingNames()
    {
        var state = State();
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
        var state = State();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValueAdmissionStillPrecedesTargetMutation(bool bound)
    {
        var state = State(AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(3));
        var name = AwkVariableName.FromText("x");
        var target = AwkVariableName.FromText("target");
        state.AssignVariableValue(name, AwkValue.FromNumber(1));
        state.AssignVariableValue(target, AwkValue.FromNumber(7));
        if (bound) _ = state.ResolvePreparedGlobalValue(name, 0);
        state.AssignVariableValue(name, AwkValue.FromNumericString("12345"));
        var length = AwkExpression.Length(AwkExpression.PreparedGlobalVariable(name, 0),
            AwkOptionalIdentifier.Some(name.Identifier));
        var assignment = AwkExpression.Assignment(AwkReference.Scalar(target), length);
        var values = state.Counters.ValuesCreated;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await assignment.EvaluateAsync(new(state)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(bound ? 1 : 0, state.Counters.BoundScalarLengths);
        Assert.Equal(values, state.Counters.ValuesCreated);
        Assert.Equal(7, state.ResolveVariableValue(target).AsNumber());
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkRuntimeLimits? limits = null)
    {
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), (limits ?? AwkRuntimeLimits.CreateDefault()).WithMaxExecutionMemoryBytes(1 << 20),
            AwkRegexEngineKind.DotNetCompatibility);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.InitializeGlobalReadSlots(2);
        return state;
    }
}
