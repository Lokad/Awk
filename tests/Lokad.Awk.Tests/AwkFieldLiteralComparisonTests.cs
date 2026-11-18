namespace Lokad.Awk.Tests;

public sealed class AwkFieldLiteralComparisonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComparisonConsumesBorrowedBytesWithoutCachingOrRetainingThem(bool finite)
    {
        using var memory = State(finite);
        var state = memory.State;
        var counters = state.Counters;
        var bytes = "first second"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        _ = state.FieldCount;
        var region = Bind(state, "first");
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        var allocations = counters[AwkCounter.TextAllocations];
        Assert.True(await CompareAsync(region, storage, state, regionOutput, regionIo));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        AwkTestHarness.PublishBufferedRecord(state, "XXXXX XXXXXX"u8);
        AwkTestHarness.PublishBufferedRecord(state, "XXXXX XXXXXX"u8);
        Assert.True(original.Span.StartsWith("XXXXX"u8));
        Assert.False(await CompareAsync(region, storage, state, regionOutput, regionIo));
        state.SetCurrentRecord(AwkText.FromString("first next"));
        Assert.True(await CompareAsync(region, storage, state, regionOutput, regionIo));
    }

    [Fact]
    public async Task CallerCancellationPreservesAlreadySplitRecordAndAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation: cancellation.Token);
        var state = memory.State;
        var text = new string('a', 16_384) + " tail";
        state.SetCurrentRecord(AwkText.FromString(text));
        _ = state.FieldCount;
        var region = Bind(state, new string('a', 16_384));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        var committed = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await CompareAsync(region, storage, state, regionOutput, regionIo));
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        Assert.Equal(text, state.CurrentRecordByteText.AsString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes - 1)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes + 1)]
    public async Task LiteralLengthsPreserveRecordAndAdmission(int length)
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("x"));
        _ = state.FieldCount;
        var region = Bind(state, new string('1', length));
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        storage.Initialize(region);
        var committed = state.MemoryAccounting.LiveBytes;
        Assert.False(await CompareAsync(region, storage, state, regionOutput, regionIo));
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        Assert.Equal("x", state.CurrentRecordByteText.AsString());
    }

    private static AwkRegion Bind(AwkRuntimeState state, string literal)
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{result=($1==\"" + literal + "\")}"));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.AssignVariableValue(AwkVariableName.FromText("result"), AwkValue.FromNumber(0));
        return Assert.Single(program.BeginActions);
    }

    private static async ValueTask<bool> CompareAsync(AwkRegion region, AwkRegionStorage storage, AwkRuntimeState state,
        AwkOutputSink regionOutput, AwkIo regionIo)
    {
        Assert.Equal(AwkStatementFlow.Continue, await region.ExecuteAsync(state, storage, regionOutput, regionIo));
        return state.ResolveVariableValue(AwkVariableName.FromText("result")).IsTruthy();
    }

    private static CooperativeTestMemory State(bool finite,
        CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, enabled: finite);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
