namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumberReadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyBoundNumberSlotsUseTheCurrentNumericValue(bool slots)
    {
        using var memoryForState = State(slots: slots);
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("x");
        var reference = AwkResolvedReference.PreparedGlobalScalar(name, 0);
        Assert.Equal(0, reference.EvaluateNumber(state));
        Assert.Equal(slots, state.HasBoundGlobalScalar(0));
        Assert.Equal(0, reference.EvaluateNumber(state));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
        reference.AssignValue(state, AwkValue.FromNumber(7));
        Assert.Equal(7, reference.EvaluateNumber(state));
        state.AssignVariableValue(name, AwkValue.FromNumericString("007"));
        Assert.Equal(7, reference.EvaluateNumber(state));
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(slots ? 1 : 0, state.Counters.NumericGlobalReads);
        state.AssignVariableValue(name, AwkValue.FromNumber(9));
        Assert.Equal(9, reference.EvaluateNumber(state));
        Assert.Equal(slots ? 2 : 0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void ComputedFieldCountRetainsItsLiveValueAfterBinding()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var reference = AwkResolvedReference.PreparedGlobalScalar(AwkVariableName.FromText("NF"), 0);
        state.SetCurrentRecord(AwkText.FromString("old tail"));
        Assert.Equal(2, reference.EvaluateNumber(state));
        Assert.True(state.HasBoundGlobalScalar(0));
        state.SetCurrentRecord(AwkText.FromString("next has three"));
        Assert.Equal(3, reference.EvaluateNumber(state));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void OtherReferenceKindsUseTheOriginalValueConversion()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var scalar = AwkResolvedReference.Scalar(AwkVariableName.FromText("x"));
        scalar.AssignValue(state, AwkValue.FromNumericString("007"));
        Assert.Equal(7, scalar.EvaluateNumber(state));
        var array = AwkResolvedReference.ArrayElement(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1));
        array.AssignValue(state, AwkValue.FromText("4tail"));
        Assert.Equal(4, array.EvaluateNumber(state));
        state.SetCurrentRecord(AwkText.FromString("old 007"));
        Assert.Equal(7, AwkResolvedReference.Field(new AwkFieldIndex(2)).EvaluateNumber(state));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    private static CooperativeTestMemory State(AwkRuntimeLimits? limits = null, bool slots = true)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), (limits ?? AwkRuntimeLimits.CreateDefault()),
            AwkRegexEngineKind.DotNetCompatibility);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        if (slots) state.InitializeGlobalReadSlots(1);
        return testMemory.Initialize(() => state);
    }
}
