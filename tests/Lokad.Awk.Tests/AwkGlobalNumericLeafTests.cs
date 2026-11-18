namespace Lokad.Awk.Tests;

public sealed class AwkGlobalNumericLeafTests
{
    private static readonly AwkVariableName Result = AwkVariableName.FromText("result");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NumericReadsKeepCurrentValuesAndOriginalSpelling(bool preparedBinding)
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("x");
        var region = Bind(state, "x", preparedBinding);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(0, await ReadAsync(region, state, storage, regionOutput, regionIo));
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        Assert.Equal(7, await ReadAsync(region, state, storage, regionOutput, regionIo));
        state.AssignVariableValue(name, AwkTestHarness.NumericInput("007"));
        Assert.Equal(7, await ReadAsync(region, state, storage, regionOutput, regionIo));
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());

        state.AssignVariableValue(name, AwkValue.FromNumber(9));
        Assert.Equal(9, await ReadAsync(region, state, storage, regionOutput, regionIo));

    }

    [Fact]
    public async Task ComputedNfKeepsCurrentAndAssignedValues()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "NF");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("old tail"));
        Assert.Equal(2, await ReadAsync(region, state, storage, regionOutput, regionIo));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next has three"));
        Assert.Equal(3, await ReadAsync(region, state, storage, regionOutput, regionIo));
        state.AssignVariableValue(AwkVariableName.FromText("NF"), AwkValue.FromNumber(1.5));
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(1.5, await ReadAsync(region, state, storage, regionOutput, regionIo));

    }

    [Fact]
    public async Task ArrayFailureDoesNotPublishScalarValues()
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("x");
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(7));
        var region = Bind(state, "x");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal("awk: attempt to use array `x' in a scalar context\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(state.HasScalar(name));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());

    }

    [Fact]
    public async Task CanceledComputationDoesNotReadOrPublish()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(cancellation.Token);
        var state = memory.State;
        var region = Bind(state, "x");
        state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(state.HasScalar(Result));

    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.0)]
    [InlineData(1.25)]
    public async Task BoundNumericReadsPreserveTheirBinary64Value(double value)
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "x");
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(value));
        // Warm the real binding without doing an arithmetic operation that changes -0.
        Assert.Equal(value, state.ResolveVariableValue(name).AsNumber());
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        var actual = await ReadAsync(region, state, storage, regionOutput, regionIo);
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(actual));
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
    }

    [Fact]
    public async Task ComparisonKeepsLeftNumberAcrossRightFieldAdmission()
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("x");
        var region = Bind(state, "(x<NF)");
        state.AssignVariableValue(name, AwkValue.FromNumber(2));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("one two"));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(0, await ReadAsync(region, state, storage, regionOutput, regionIo));

        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("field", 300))));
        var admitted = false;
        memory.Budget.OnGrowth = _ =>
        {
            memory.Budget.OnGrowth = null;
            admitted = true;
            state.AssignVariableValue(name, AwkValue.FromNumber(999));
        };
        Assert.Equal(1, await ReadAsync(region, state, storage, regionOutput, regionIo));
        Assert.True(admitted);
        Assert.Equal(999, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static async ValueTask<double> ReadAsync(AwkRegion region, AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink regionOutput, AwkIo regionIo)
    {
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        return state.ResolveVariableValue(Result).AsNumber();
    }

    private static AwkRegion Bind(AwkRuntimeState state, string name, bool preparedBinding = true)
    {
        var parsed = AwkTestHarness.ParseSource("BEGIN{result=+" + name + "}");
        if (!preparedBinding)
        {
            Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
            return region;
        }
        var binding = AwkTestHarness.BindDefaultProfile(parsed);
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        return Assert.Single(program.BeginActions);
    }

    private static CooperativeTestMemory State(CancellationToken token = default)
    {
        var memory = new CooperativeTestMemory(1 << 20);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
                AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
}
