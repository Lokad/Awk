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

        reference.AssignValue(state, AwkValue.FromNumber(7));
        Assert.Equal(7, reference.EvaluateNumber(state));
        state.AssignVariableValue(name, AwkValue.FromNumericString("007"));
        Assert.Equal(7, reference.EvaluateNumber(state));
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());

        state.AssignVariableValue(name, AwkValue.FromNumber(9));
        Assert.Equal(9, reference.EvaluateNumber(state));

    }

    [Fact]
    public async Task ComputedFieldCountRetainsItsLiveValueInPreparedExecution()
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{result=+NF}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        var action = Assert.Single(program.BeginActions);
        var result = AwkVariableName.FromText("result");
        state.SetCurrentRecord(AwkText.FromString("old tail"));
        Assert.Equal(AwkStatementFlow.Continue, await action.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(2, state.ResolveVariableValue(result).AsNumber());
        state.SetCurrentRecord(AwkText.FromString("next has three"));
        Assert.Equal(AwkStatementFlow.Continue, await action.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(3, state.ResolveVariableValue(result).AsNumber());

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

    }

    private static CooperativeTestMemory State(AwkRuntimeLimits? limits = null, bool slots = true)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), (limits ?? AwkRuntimeLimits.CreateDefault()), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        if (slots) state.InitializeGlobalReadSlots(1);
        return testMemory.Initialize(() => state);
    }
}
