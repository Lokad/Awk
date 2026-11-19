namespace Lokad.Awk.Tests;

public sealed class AwkSnapshotKeyResourceTests
{
    private const int MemoryLimit = 8 << 20;
    private static readonly AwkArrayName Symbols = AwkArrayName.FromText("SYMTAB");
    private static readonly AwkArrayName Source = AwkArrayName.FromText("source");
    private static readonly AwkArrayName Destination = AwkArrayName.FromText("destination");
    private static readonly AwkArrayKey First = AwkArrayKey.FromOrdinal(1);

    public static IEnumerable<object[]> Snapshots()
    {
        foreach (var observed in new[] { false, true })
            foreach (var source in new[] { "symbols", "ordinals", "texts", "compact" })
                foreach (var selector in new[] { "", "@unsorted", "@ind_str_asc" })
                    yield return [observed, source, selector];
    }

    public static IEnumerable<object[]> NewPayloads()
    {
        foreach (var observed in new[] { false, true })
        {
            foreach (var source in new[] { "symbols", "texts" })
            foreach (var selector in new[] { "", "@unsorted", "@ind_str_asc" })
                yield return [observed, source, selector];
            yield return [observed, "ordinals", "@ind_str_asc"];
            yield return [observed, "compact", "@ind_str_asc"];
        }
    }

    [Theory]
    [MemberData(nameof(Snapshots))]
    public async Task SnapshotHoldsItsKeyPayloadUntilDisposalAndAcceptsExactAdmission(bool observed, string source, string selector)
    {
        var (memory, name) = CreateState(observed, source, selector);
        using var ownedMemory = memory;
        var state = memory.State;
        var (keys, containers, payload) = await MeasureExpected(state, name, source, selector);
        var required = AwkRuntimeMemory.CheckedAdd(containers, payload);
        var baseline = state.MemoryAccounting.LiveBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required);
        var before = state.MemoryAccounting.LiveBytes;
        using (var snapshot = AwkArrayStorageTestOperations.Snapshot(state, name))
        {
            Assert.Equal(keys, snapshot.ToArray());
            Assert.Equal(before + required, state.MemoryAccounting.LiveBytes);
            if (source == "ordinals" && selector != "@ind_str_asc")
                Assert.All(snapshot, key => Assert.Equal(0, key.RetainedTextMemoryBytes));
        }
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    [Theory]
    [MemberData(nameof(NewPayloads))]
    public async Task KeyPayloadShortfallFailsBeforeSnapshotAllocationAndAllowsRetry(bool observed, string source, string selector)
    {
        var (memory, name) = CreateState(observed, source, selector);
        using var ownedMemory = memory;
        var state = memory.State;
        var (keys, containers, payload) = await MeasureExpected(state, name, source, selector);
        Assert.True(payload > 0);
        var required = containers + payload;
        var baseline = state.MemoryAccounting.LiveBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.LiveBytes;

            var error = Assert.Throws<AwkRuntimeException>(() =>
                AwkArrayStorageTestOperations.Snapshot(state, name));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(before, state.MemoryAccounting.LiveBytes);
        }
        using (var retry = AwkArrayStorageTestOperations.Snapshot(state, name))
        {
            Assert.Equal(keys, retry.ToArray());
            Assert.Equal(baseline + required, state.MemoryAccounting.LiveBytes);
        }
        Assert.Equal(baseline, state.MemoryAccounting.LiveBytes);
    }

    [Theory]
    [InlineData(false, "symbols")]
    [InlineData(true, "symbols")]
    [InlineData(false, "ordinals")]
    [InlineData(true, "ordinals")]
    [InlineData(false, "texts")]
    [InlineData(true, "texts")]
    [InlineData(false, "compact")]
    [InlineData(true, "compact")]
    public async Task KeySortTransfersTemporaryPayloadAndRefundsDestination(bool observed, string source)
    {
        var (memory, name) = CreateState(observed, source, "@ind_str_asc");
        using var ownedMemory = memory;
        var state = memory.State;
        state.Arrays.Clear(Destination);
        var beforeDestination = state.MemoryAccounting.LiveBytes;
        state.SetArrayString(Destination, First, "keep");
        var oldBytes = state.MemoryAccounting.LiveBytes - beforeDestination;
        var (keys, _, payload) = await MeasureExpected(state, name, source, "@ind_str_asc");
        var before = state.MemoryAccounting.LiveBytes;

        var count = AwkArrayStorageTestOperations.Sort(state, name, Destination, keys: true, selector: "@ind_str_asc");

        Assert.Equal(keys.Length, count);
        for (var index = 0; index < keys.Length; index++)
            Assert.Equal(keys[index].ToString(), state.GetArrayByteText(Destination, AwkArrayKey.FromOrdinal(index + 1)).AsString());
        Assert.True(state.MemoryAccounting.LiveBytes > before - oldBytes);
        state.Arrays.Clear(Destination);
        Assert.Equal(before - oldBytes, state.MemoryAccounting.LiveBytes);
    }

    public static IEnumerable<object[]> SortFailures()
    {
        foreach (var observed in new[] { false, true })
            foreach (var source in new[] { "symbols", "ordinals", "compact" })
                foreach (var keys in new[] { false, true })
                    yield return [observed, source, keys];
    }

    [Theory]
    [MemberData(nameof(SortFailures))]
    public async Task SortPayloadAdmissionFailurePreservesSourceAndDestination(bool observed, string source, bool keys)
    {
        var (memory, name) = CreateState(observed, source, "@ind_str_asc");
        using var ownedMemory = memory;
        var state = memory.State;
        state.SetArrayString(Destination, First, "keep");
        var (original, _, payload) = await MeasureExpected(state, name, source, "@ind_str_asc");
        var required = AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(original.Length) + payload +
            (original.Length >= 2 ? AwkRuntimeMemory.EstimateInt32ArrayBytes(original.Length) : 0);
        var baseline = state.MemoryAccounting.LiveBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.LiveBytes;

            var error = Assert.Throws<AwkRuntimeException>(() => (keys
                ? AwkArrayStorageTestOperations.Sort(state, name, Destination, keys: true, selector: "@ind_str_asc")
                : AwkArrayStorageTestOperations.Sort(state, name, Destination, keys: false, selector: "@ind_str_asc")));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal("keep", state.GetArrayByteText(Destination, First).AsString());
            Assert.Equal(before, state.MemoryAccounting.LiveBytes);
        }
        using var retry = AwkArrayStorageTestOperations.Snapshot(state, name);
        Assert.Equal(original, retry.ToArray());
    }

    [Theory]
    [InlineData(false, "symbols")]
    [InlineData(true, "symbols")]
    [InlineData(false, "ordinals")]
    [InlineData(true, "ordinals")]
    [InlineData(false, "texts")]
    [InlineData(true, "texts")]
    public void SnapshotMeasurementCancelsBeforeEncodingKeysAndAllowsRetry(bool observed, string source)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(MemoryLimit).InitializeRuntime(counters, cancellation.Token);
        var governor = memory.Accounting;
        var variables = memory.State.Variables;
        var name = source == "symbols" ? Symbols : Source;
        Initialize(variables);
        var before = governor.LiveBytes;
        var allocations = counters[AwkCounter.TextAllocations];
        var encodes = counters[AwkCounter.Utf8Encodes];
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => variables.MeasureArraySnapshot(name, materializeKeys: true));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(before, governor.LiveBytes);
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(encodes, counters[AwkCounter.Utf8Encodes]);
        using var retry = new CooperativeTestMemory(MemoryLimit).InitializeRuntime(counters);
        var retryVariables = retry.State.Variables;
        Initialize(retryVariables);
        var retryBaseline = retry.Accounting.LiveBytes;
        var measured = retryVariables.MeasureArraySnapshot(name, materializeKeys: true);
        var keys = new AwkArrayKey[retryVariables.GetArraySnapshotCount(name)];
        retryVariables.CopyArrayKeys(name, keys);
        Assert.Equal(keys.Length, measured.Count);
        Assert.Equal(keys.Sum(static key => AwkRuntimeMemory.EstimateTextBytes(key.ByteLength)), measured.KeyTextBytes);
        Assert.Equal(retryBaseline, retry.Accounting.LiveBytes);

        void Initialize(AwkVariables owner)
        {
            if (source == "symbols")
                owner.InitializeGlobalNames(Enumerable.Range(1, 512).Select(index => AwkVariableName.FromText("v" + index)).ToHashSet());
            else
                for (var index = 1; index <= 512; index++)
                    owner.SetArrayValue(name, source == "ordinals" ? AwkArrayKey.FromOrdinal(index) : new AwkArrayKey("v" + index),
                        AwkValue.FromNumber(index));
        }
    }

    private static async Task<(AwkArrayKey[] Keys, long Containers, long Payload)> MeasureExpected(
        AwkRuntimeState state, AwkArrayName name, string source, string selector)
    {
        using var snapshot = AwkArrayStorageTestOperations.Snapshot(state, name);
        var keys = snapshot.ToArray();
        var sorted = selector == "@ind_str_asc";
        var containers = AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(keys.Length) +
            (sorted ? AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(keys.Length) +
                (keys.Length >= 2 ? AwkRuntimeMemory.EstimateInt32ArrayBytes(keys.Length) : 0) : 0);
        var payload = source == "symbols" || (source is "ordinals" or "compact") && sorted
            ? keys.Sum(static key => AwkRuntimeMemory.EstimateTextBytes(key.ByteLength))
            : keys.Sum(static key => key.RetainedTextMemoryBytes);
        if (sorted)
            payload += keys.Sum(key => state.Arrays.GetAwkValue(name, key).RetainedTextMemoryBytes);
        return (keys, containers, payload);
    }

    private static (CooperativeTestMemory Memory, AwkArrayName Name) CreateState(bool observed, string source, string selector)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(MemoryLimit);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options));
        memory.Initialize(state);
        state.SetArrayString(AwkArrayName.FromText("PROCINFO"), new AwkArrayKey("sorted_in"), selector);
        if (source == "symbols")
        {
            state.InitializeGlobalNames(Enumerable.Range(0, 8).Select(index =>
                AwkVariableName.FromText("v" + index + new string('a', 8_192))).ToHashSet());
            return (memory, Symbols);
        }
        for (var index = 1; index <= 32; index++)
        {
            var key = source switch
            {
                "ordinals" => AwkArrayKey.FromOrdinal(index),
                "compact" => state.CopyArrayKey(new(AwkUtf8Codec.Encode("v" + index).Span)),
                // Keep owned-text lifetime/refusal coverage distinct from compact keys.
                _ => new AwkArrayKey("long-key-" + index)
            };
            state.Arrays.SetAwkValue(Source, key, AwkValue.FromNumber(33 - index));
        }
        return (memory, Source);
    }
}
