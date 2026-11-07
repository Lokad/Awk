namespace Lokad.Awk.Tests;

public sealed class AwkFieldLiteralComparisonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComparisonConsumesBorrowedBytesWithoutCachingOrRetainingThem(bool finite)
    {
        using var memory = State(finite);
        var state = memory.State;
        var counters = state.Counters;
        var bytes = "first second"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        _ = state.FieldCount;
        var region = Bind(state, "first");
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var allocations = counters.AwkTextAllocations;
        Assert.True(Compare(region, storage, state));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        AwkTestHarness.PublishBufferedRecord(state, "XXXXX XXXXXX"u8);
        AwkTestHarness.PublishBufferedRecord(state, "XXXXX XXXXXX"u8);
        Assert.True(original.Span.StartsWith("XXXXX"u8));
        Assert.False(Compare(region, storage, state));
        state.SetCurrentRecord(AwkText.FromString("first next"));
        Assert.True(Compare(region, storage, state));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void CallerCancellationPreservesAlreadySplitRecordAndAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation: cancellation.Token);
        var state = memory.State;
        var text = new string('a', 16_384) + " tail";
        state.SetCurrentRecord(AwkText.FromString(text));
        _ = state.FieldCount;
        var region = Bind(state, new string('a', 16_384));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => Compare(region, storage, state));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(text, state.CurrentRecordByteText.AsString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes - 1)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes)]
    [InlineData(AwkRuntimeLimits.CancellationCheckBytes + 1)]
    public void LiteralLengthsPreserveRecordAndAdmission(int length)
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("x"));
        _ = state.FieldCount;
        var region = Bind(state, new string('1', length));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.False(Compare(region, storage, state));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("x", state.CurrentRecordByteText.AsString());
    }

    private static AwkRegion Bind(AwkRuntimeState state, string literal)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{result=($1==\"" + literal + "\")}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.AssignVariableValue(AwkVariableName.FromText("result"), AwkValue.FromNumber(0));
        return Assert.Single(program.BeginActions);
    }

    private static bool Compare(AwkRegion region, AwkRegionStorage storage, AwkRuntimeState state)
    {
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        return state.ResolveVariableValue(AwkVariableName.FromText("result")).IsTruthy();
    }

    private static CooperativeTestMemory State(bool finite,
        CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: finite);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
