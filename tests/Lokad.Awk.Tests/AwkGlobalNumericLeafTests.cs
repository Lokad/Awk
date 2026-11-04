namespace Lokad.Awk.Tests;

public sealed class AwkGlobalNumericLeafTests
{
    private static readonly AwkVariableName Result = AwkVariableName.FromText("result");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericReadsKeepCurrentValuesAndOriginalSpelling(bool preparedBinding)
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("x");
        var region = Bind(state, "x", preparedBinding);
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(0, Read(region, state, storage));
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        Assert.Equal(7, Read(region, state, storage));
        state.AssignVariableValue(name, AwkValue.FromNumericString("007"));
        Assert.Equal(7, Read(region, state, storage));
        Assert.Equal("007", state.ResolveVariableValue(name).AsString());
        Assert.Equal(preparedBinding ? 1 : 0, state.Counters.NumericGlobalReads);
        state.AssignVariableValue(name, AwkValue.FromNumber(9));
        Assert.Equal(9, Read(region, state, storage));
        Assert.Equal(preparedBinding ? 2 : 0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void ComputedNfKeepsCurrentAndAssignedValues()
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "NF");
        using var storage = new AwkRegionStorage(state);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("old tail"));
        Assert.Equal(2, Read(region, state, storage));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next has three"));
        Assert.Equal(3, Read(region, state, storage));
        state.AssignVariableValue(AwkVariableName.FromText("NF"), AwkValue.FromNumber(1.5));
        Assert.Equal(1, state.FieldCount);
        Assert.Equal(1.5, Read(region, state, storage));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void ArrayFailureDoesNotPublishScalarValues()
    {
        using var memory = State();
        var state = memory.State;
        var name = AwkVariableName.FromText("x");
        var array = new AwkArrayName(name.Identifier);
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(array, key, AwkValue.FromNumber(7));
        var region = Bind(state, "x");
        using var storage = new AwkRegionStorage(state);
        var error = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: attempt to use array `x' in a scalar context\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(state.HasScalar(name));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Fact]
    public void CanceledComputationDoesNotReadOrPublish()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(cancellation.Token);
        var state = memory.State;
        var region = Bind(state, "x");
        state.AssignVariableValue(AwkVariableName.FromText("x"), AwkValue.FromNumber(7));
        using var storage = new AwkRegionStorage(state);
        cancellation.Cancel();
        var error = Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(state.HasScalar(Result));
        Assert.Equal(0, state.Counters.NumericGlobalReads);
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)] [InlineData(-0.0)] [InlineData(1.25)]
    public void BoundNumericReadsPreserveTheirBinary64Value(double value)
    {
        using var memory = State();
        var state = memory.State;
        var region = Bind(state, "x");
        var name = AwkVariableName.FromText("x");
        state.AssignVariableValue(name, AwkValue.FromNumber(value));
        // Warm the real binding without doing an arithmetic operation that changes -0.
        Assert.Equal(value, state.ResolveVariableValue(name).AsNumber());
        using var storage = new AwkRegionStorage(state);
        var actual = Read(region, state, storage);
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(actual));
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    private static double Read(AwkRegion region, AwkRuntimeState state, AwkRegionStorage storage)
    {
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        return state.ResolveVariableValue(Result).AsNumber();
    }

    private static AwkRegion Bind(AwkRuntimeState state, string name, bool preparedBinding = true)
    {
        var parsed = AwkParser.Parse("BEGIN{result=+" + name + "}");
        if (!preparedBinding)
        {
            Assert.True(AwkRegionCompiler.TryCompileRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
            return region;
        }
        var binding = AwkBinder.Bind(parsed);
        Assert.True(binding.Success);
        var program = binding.Program.Program;
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
