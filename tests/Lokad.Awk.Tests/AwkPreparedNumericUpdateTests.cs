namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumericUpdateTests
{
    private const int Limit = 1 << 20;
    private static readonly AwkVariableName Value = AwkVariableName.FromText("value");
    private static readonly AwkVariableName Result = AwkVariableName.FromText("result");

    [Theory]
    [InlineData("k", "number")]
    [InlineData("é", "number")]
    [InlineData("k", "cancel")]
    [InlineData("é", "cancel")]
    [InlineData("k", "refuse")]
    [InlineData("é", "refuse")]
    public async Task CompleteFieldUpdatesPreserveCapturedNumberAcrossAdmission(string spelling, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        var array = AwkArrayName.FromText("values");
        var key = new AwkArrayKey(spelling);
        var region = Bind(state, "values[$1]+=$300", "values[\"warm\"]+=0;");
        state.Arrays.SetAwkValue(array, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(spelling + " " +
            string.Join(' ', Enumerable.Repeat("3", 299))));
        state.RecordFields.GetFieldNumber(new(1), state.NumberParsingMode);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);

        var attempted = false;
        if (outcome == "refuse") memory.Budget.RefuseNextAdmissionMinimumGrowth = 4096;
        memory.Budget.OnGrowth = bytes =>
        {
            if (bytes < 4096) return;
            memory.Budget.OnGrowth = null;
            attempted = true;

            if (outcome == "number") state.Arrays.SetAwkValue(array, key, AwkValue.FromNumber(100));
            else if (outcome == "cancel") cancellation.Cancel();
        };
        if (outcome == "cancel")
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        else if (outcome == "refuse")
        {
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        }
        else
        {
            Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
            Assert.Equal(10, state.ResolveVariableValue(Result).AsNumber());
        }
        Assert.True(attempted);
        Assert.True(state.Arrays.TryPeekAwkValue(array, key, out var value));
        Assert.Equal(outcome is "cancel" or "refuse" ? 7 : 10, value.AsNumber());
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteUpdatesKeepThePriorNumberAcrossRightAdmission(bool arrayTarget)
    {
        using var memory = State(true);
        var state = memory.State;
        var array = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("k");
        var region = Bind(state, arrayTarget ? "values[\"k\"]+=NF" : "value+=NF");
        if (arrayTarget) state.Arrays.SetAwkValue(array, key, AwkValue.FromNumber(7));
        else state.AssignVariableValue(Value, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("field", 300))));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        var admitted = false;
        memory.Budget.OnGrowth = bytes =>
        {
            if (bytes < 4096) return;
            memory.Budget.OnGrowth = null;
            admitted = true;
            if (arrayTarget) state.Arrays.SetAwkValue(array, key, AwkValue.FromNumber(100));
            else state.AssignVariableValue(Value, AwkValue.FromNumber(100));
        };
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.True(admitted);
        Assert.Equal(307, state.ResolveVariableValue(Result).AsNumber());
        Assert.Equal(307, arrayTarget ? state.Arrays.GetAwkValue(array, key).AsNumber() : state.ResolveVariableValue(Value).AsNumber());
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task NativeUpdatesReturnNumericValuesAndPreserveStorage(bool decrement, bool post, bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var operation = decrement ? "--" : "++";
        var region = Bind(state, post ? "value" + operation : operation + "value");
        state.AssignVariable(Value, "007");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        var result = state.ResolveVariableValue(Result);
        Assert.True(result.IsNumberValue);
        Assert.Equal(post ? 7 : decrement ? 6 : 8, result.AsNumber());
        Assert.Equal(decrement ? 6 : 8, state.ResolveVariableValue(Value).AsNumber());
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public async Task StorageRefusalDoesNotBindOrUpdateMissingScalar()
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "++value");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.LiveBytes);
        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.False(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.0)]
    [InlineData(1.25)]
    public async Task NumericUpdatesPreserveBinary64AtFullBudget(double number)
    {
        using var memory = State(true);
        var state = memory.State;
        var region = Bind(state, "value*=1");
        state.AssignVariableValue(Value, AwkValue.FromNumber(number));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));

        Assert.True(memory.Budget.TryReserve(Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
            Assert.Equal(BitConverter.DoubleToInt64Bits(number),
                BitConverter.DoubleToInt64Bits(state.ResolveVariableValue(Value).AsNumber()));
            Assert.Equal(BitConverter.DoubleToInt64Bits(number),
                BitConverter.DoubleToInt64Bits(state.ResolveVariableValue(Result).AsNumber()));

            Assert.Equal(0, memory.Budget.Refusals);
        }
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        storage.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public async Task LongInputCancellationPrecedesMutationAndRetainsItsFunding()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        var region = Bind(state, "++value");
        state.AssignVariable(Value, "1" + new string('0', 8000));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        var before = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(state.HasScalar(Value));
        Assert.False(state.HasScalar(Result));
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    private static AwkRegion Bind(AwkRuntimeState state, string expression, string prefix = "")
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{" + prefix + "result=(" + expression + ")}"));
        Assert.True(binding.Success);
        var program = binding.Program;
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
