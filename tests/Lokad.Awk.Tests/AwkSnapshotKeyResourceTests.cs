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
        foreach (var source in new[] { "symbols", "ordinals", "texts" })
        foreach (var selector in new[] { "", "@unsorted", "@ind_str_asc" })
            yield return [observed, source, selector];
    }

    public static IEnumerable<object[]> NewPayloads()
    {
        foreach (var observed in new[] { false, true })
        {
            foreach (var selector in new[] { "", "@unsorted", "@ind_str_asc" })
                yield return [observed, "symbols", selector];
            yield return [observed, "ordinals", "@ind_str_asc"];
        }
    }

    [Theory]
    [MemberData(nameof(Snapshots))]
    public async Task SnapshotHoldsItsNewKeyPayloadUntilDisposalAndAcceptsExactAdmission(bool observed, string source, string selector)
    {
        var (state, name) = CreateState(observed, source, selector);
        var (keys, containers, payload) = await MeasureExpected(state, name, source, selector);
        var required = AwkRuntimeMemory.CheckedAdd(containers, payload);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var snapshot = await state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state)))
        {
            Assert.Equal(keys, snapshot.ToArray());
            Assert.Equal(before + required, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            if (source != "symbols" && selector != "@ind_str_asc")
                Assert.All(snapshot, key => Assert.Equal(source == "ordinals", key.IsOrdinal));
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [MemberData(nameof(NewPayloads))]
    public async Task NewKeyPayloadShortfallFailsBeforeSnapshotAllocationAndAllowsRetry(bool observed, string source, string selector)
    {
        var (state, name) = CreateState(observed, source, selector);
        var (keys, containers, payload) = await MeasureExpected(state, name, source, selector);
        Assert.True(payload > 0);
        var required = containers + payload;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var steps = state.Counters.ExecutionSteps;

            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
                state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state)).AsTask());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(steps, state.Counters.ExecutionSteps);
        }
        using (var retry = await state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state)))
        {
            Assert.Equal(keys, retry.ToArray());
            Assert.Equal(baseline + required, state.MemoryAccounting.CurrentCommittedBytes);
        }
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, "symbols")]
    [InlineData(true, "symbols")]
    [InlineData(false, "ordinals")]
    [InlineData(true, "ordinals")]
    public async Task KeySortTransfersTemporaryPayloadIntoDestinationWithoutDuplicateAdmission(bool observed, string source)
    {
        var (state, name) = CreateState(observed, source, "@ind_str_asc");
        state.SetArrayString(Destination, First, "keep");
        var (keys, _, payload) = await MeasureExpected(state, name, source, "@ind_str_asc");
        var entries = AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(keys.Length);
        var destinationBytes = keys.Sum(static key => AwkRuntimeMemory.EstimateExternalArrayElementBytes(0, key.ByteLength));
        var oldBytes = AwkRuntimeMemory.EstimateExternalArrayElementBytes(0, 4);
        var required = entries + payload + Math.Max(0, destinationBytes - oldBytes - payload);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var count = await state.SortArrayKeysAsync(name, Destination, AwkArraySortOrder.Parse("@ind_str_asc"),
            new AwkEvaluationContext(state));

        Assert.Equal(keys.Length, count);
        for (var index = 0; index < keys.Length; index++)
            Assert.Equal(keys[index].ToString(), state.GetArrayByteText(Destination, AwkArrayKey.FromOrdinal(index + 1)).AsString());
        Assert.Equal(before - oldBytes + destinationBytes, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.ClearArray(Destination);
        Assert.Equal(before - oldBytes, state.MemoryAccounting.CurrentCommittedBytes);
    }

    public static IEnumerable<object[]> SortFailures()
    {
        foreach (var observed in new[] { false, true })
        foreach (var source in new[] { "symbols", "ordinals" })
        foreach (var keys in new[] { false, true })
            yield return [observed, source, keys];
    }

    [Theory]
    [MemberData(nameof(SortFailures))]
    public async Task SortPayloadAdmissionFailurePreservesSourceAndDestination(bool observed, string source, bool keys)
    {
        var (state, name) = CreateState(observed, source, "@ind_str_asc");
        state.SetArrayString(Destination, First, "keep");
        var (original, _, payload) = await MeasureExpected(state, name, source, "@ind_str_asc");
        var required = AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(original.Length) + payload;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(MemoryLimit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var steps = state.Counters.ExecutionSteps;

            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => (keys
                ? state.SortArrayKeysAsync(name, Destination, AwkArraySortOrder.Parse("@ind_str_asc"), new AwkEvaluationContext(state))
                : state.SortArrayValuesAsync(name, Destination, AwkArraySortOrder.Parse("@ind_str_asc"), new AwkEvaluationContext(state))).AsTask());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal("keep", state.GetArrayByteText(Destination, First).AsString());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(steps, state.Counters.ExecutionSteps);
        }
        using var retry = await state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state));
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
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(MemoryLimit), counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        void Check()
        {
            if (!armed) return;
            if (++checks == 4) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var variables = new AwkVariables(4_096, 4_096, 1_024, governor, counters,
            AwkValue.FromText, AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            (_, _) => { }, _ => { }, checkpoint: Check);
        var name = source == "symbols" ? Symbols : Source;
        if (source == "symbols")
            variables.InitializeGlobalNames(Enumerable.Range(1, 512).Select(index => AwkVariableName.FromText("v" + index)).ToHashSet());
        else
            for (var index = 1; index <= 512; index++)
                variables.SetArrayValue(name, source == "ordinals" ? AwkArrayKey.FromOrdinal(index) : new AwkArrayKey("v" + index),
                    AwkValue.FromNumber(index));
        var before = governor.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        var encodes = counters.Utf8Encodes;
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => variables.MeasureArraySnapshot(name, materializeOrdinalKeys: true));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(4, checks);
        Assert.Equal(before, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(encodes, counters.Utf8Encodes);
        armed = false;
        var measured = variables.MeasureArraySnapshot(name, materializeOrdinalKeys: true);
        Assert.Equal(512, measured.Count);
        var expectedPayload = source == "texts" ? 0 : Enumerable.Range(1, 512).Sum(index =>
            AwkRuntimeMemory.EstimateTextBytes(index.ToString(System.Globalization.CultureInfo.InvariantCulture).Length +
                (source == "symbols" ? 1 : 0)));
        Assert.Equal(expectedPayload, measured.NewKeyTextBytes);
        Assert.Equal(before, governor.CurrentCommittedBytes);
        Assert.Equal(0, counters.ExecutionSteps);
    }

    private static async Task<(AwkArrayKey[] Keys, long Containers, long Payload)> MeasureExpected(
        AwkRuntimeState state, AwkArrayName name, string source, string selector)
    {
        using var snapshot = await state.SnapshotArrayKeyValuesAsync(name, new AwkEvaluationContext(state));
        var keys = snapshot.ToArray();
        var sorted = selector == "@ind_str_asc";
        var containers = AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(keys.Length) +
            (sorted ? AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(keys.Length) : 0);
        var payload = source == "symbols" || source == "ordinals" && sorted
            ? keys.Sum(static key => AwkRuntimeMemory.EstimateTextBytes(key.ByteLength)) : 0;
        return (keys, containers, payload);
    }

    private static (AwkRuntimeState State, AwkArrayName Name) CreateState(bool observed, string source, string selector)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(MemoryLimit)
                .WithMaxIdentifierBytes(16_384), AwkRegexEngineKind.Utf8Adapter, AwkExecutionEngineKind.Interpreter, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.SetArrayString(AwkArrayName.FromText("PROCINFO"), new AwkArrayKey("sorted_in"), selector);
        if (source == "symbols")
        {
            state.InitializeGlobalNames(Enumerable.Range(0, 8).Select(index =>
                AwkVariableName.FromText("v" + index + new string('a', 8_192))).ToHashSet());
            return (state, Symbols);
        }
        for (var index = 1; index <= 32; index++)
            state.SetArrayAwkValue(Source, source == "ordinals" ? AwkArrayKey.FromOrdinal(index) : new AwkArrayKey("v" + index),
                AwkValue.FromNumber(33 - index));
        return (state, Source);
    }
}
