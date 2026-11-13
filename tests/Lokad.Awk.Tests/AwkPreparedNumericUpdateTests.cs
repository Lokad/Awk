namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumericUpdateTests
{
    private const int Limit = 1 << 20;
    private static readonly AwkVariableName Value = AwkVariableName.FromText("value");
    private static readonly AwkVariableName Result = AwkVariableName.FromText("result");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteUpdatesKeepThePriorNumberAcrossRightAdmission(bool arrayTarget)
    {
        using var memory = State(true);
        var state = memory.State;
        var array = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("k");
        var region = Bind(state, arrayTarget ? "values[\"k\"]+=NF" : "value+=NF");
        if (arrayTarget) state.SetArrayAwkValue(array, key, AwkValue.FromNumber(7));
        else state.AssignVariableValue(Value, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("field", 300))));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var admitted = false;
        memory.Budget.OnGrowth = bytes =>
        {
            if (bytes < 4096) return;
            memory.Budget.OnGrowth = null;
            admitted = true;
            if (arrayTarget) state.SetArrayAwkValue(array, key, AwkValue.FromNumber(100));
            else state.AssignVariableValue(Value, AwkValue.FromNumber(100));
        };
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.True(admitted);
        Assert.Equal(307, state.ResolveVariableValue(Result).AsNumber());
        Assert.Equal(307, arrayTarget ? state.GetArrayAwkValue(array, key).AsNumber() : state.ResolveVariableValue(Value).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void NativeUpdatesReturnNumericValuesAndPreserveStorage(bool decrement, bool post, bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var operation = decrement ? "--" : "++";
        var region = Bind(state, post ? "value" + operation : operation + "value");
        state.AssignVariable(Value, "007");
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        var result = state.ResolveVariableValue(Result);
        Assert.True(result.IsNumberValue);
        Assert.Equal(post ? 7 : decrement ? 6 : 8, result.AsNumber());
        Assert.Equal(decrement ? 6 : 8, state.ResolveVariableValue(Value).AsNumber());
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void StorageRefusalDoesNotBindOrUpdateMissingScalar()
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "++value");
        using var storage = new AwkRegionStorage(state);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.False(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)] [InlineData(-0.0)] [InlineData(1.25)]
    public void NumericUpdatesPreserveBinary64AtFullBudget(double number)
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "value*=1");
        state.AssignVariableValue(Value, AwkValue.FromNumber(number));
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        var reads = state.Counters.NumericGlobalReads;
        Assert.True(memory.Budget.TryReserve(Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
            Assert.Equal(BitConverter.DoubleToInt64Bits(number),
                BitConverter.DoubleToInt64Bits(state.ResolveVariableValue(Value).AsNumber()));
            Assert.Equal(BitConverter.DoubleToInt64Bits(number),
                BitConverter.DoubleToInt64Bits(state.ResolveVariableValue(Result).AsNumber()));
            Assert.Equal(reads + 1, state.Counters.NumericGlobalReads);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void LongInputCancellationPrecedesMutationAndRetainsItsFunding()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        var region = Bind(state, "++value");
        state.AssignVariable(Value, "1" + new string('0', 8000));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        var failure = Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRegion Bind(AwkRuntimeState state, string expression)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{result=(" + expression + ")}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        return Assert.Single(program.BeginActions);
    }
    private static CooperativeTestMemory State(bool governed, CancellationToken token = default)
    {
        var memory = new CooperativeTestMemory(Limit, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
                AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault()))));
    }
}
