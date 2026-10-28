namespace Lokad.Awk.Tests;

public sealed class AwkSortProjectionMemoryTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkArrayName Source = AwkArrayName.FromText("source");
    private static readonly AwkArrayName Destination = AwkArrayName.FromText("destination");
    private static readonly AwkArrayKey First = AwkArrayKey.FromOrdinal(1);

    public static IEnumerable<object[]> Projections()
    {
        foreach (var observed in new[] { false, true })
            foreach (var format in new[] { "default", "integer", "wide" })
                foreach (var selector in new[] { "@val_str_asc", "@val_str_desc" })
                    yield return [observed, format, selector];
    }

    [Theory]
    [MemberData(nameof(Projections))]
    public async Task StringValueSnapshotHoldsFormattedProjectionTextUntilDisposal(bool observed, string format, string selector)
    {
        using var memoryForState = State(observed, format, selector);
        var state = memoryForState.State;
        var expected = Values(state, format, 32);
        var payload = expected.Sum(static value => AwkRuntimeMemory.EstimateTextBytes(value.Text.ByteLength));
        var containers = SnapshotStorage(state, expected.Select(static item => item.Key).ToArray());
        var scratch = format == "wide" ? 8_192 : 0;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - containers - payload - scratch);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        using (var snapshot = await state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)))
        {
            var ordered = selector.EndsWith("desc", StringComparison.Ordinal)
                ? expected.OrderByDescending(static value => value.Text.AsString(), StringComparer.Ordinal)
                : expected.OrderBy(static value => value.Text.AsString(), StringComparer.Ordinal);
            Assert.Equal(ordered.Select(static value => value.Key), snapshot.ToArray());
            Assert.Equal(before + containers + payload, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.All(expected, value => Assert.True(state.GetArrayAwkValue(Source, value.Key).IsNumberValue));
    }

    [Theory]
    [MemberData(nameof(Projections))]
    public async Task ProjectionShortfallReleasesStagingAndPreservesStorage(bool observed, string format, string selector)
    {
        using var memoryForState = State(observed, format, selector);
        var state = memoryForState.State;
        var expected = Values(state, format, 32);
        var payload = expected.Sum(static value => AwkRuntimeMemory.EstimateTextBytes(value.Text.ByteLength));
        var containers = SnapshotStorage(state, expected.Select(static item => item.Key).ToArray());
        var scratch = format == "wide" ? 8_192 : 0;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - containers - payload - scratch + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;

            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
                state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)).AsTask());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.All(expected, value => Assert.True(state.GetArrayAwkValue(Source, value.Key).IsNumberValue));
        }
        using (var retry = await state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)))
            Assert.Equal(32, retry.Count);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    public static IEnumerable<object[]> Ties()
    {
        foreach (var observed in new[] { false, true })
            foreach (var format in new[] { "default", "wide" })
                foreach (var selector in new[] { "@val_num_asc", "@val_num_desc" })
                    yield return [observed, format, selector];
    }

    [Theory]
    [MemberData(nameof(Ties))]
    public async Task NumericTieShortfallPreservesStorageAndReleasesComparisonText(bool observed, string format, string selector)
    {
        using var memoryForState = State(observed, format, selector);
        var state = memoryForState.State;
        var left = new AwkArrayKey("a");
        var right = new AwkArrayKey("b");
        state.SetArrayAwkValue(Source, left, AwkValue.FromNumber(1.25));
        state.SetArrayAwkValue(Source, right, AwkValue.FromNumber(1.25));
        var payload = AwkRuntimeMemory.EstimateTextBytes(state.FormatStringByteText(AwkValue.FromNumber(1.25)).ByteLength) * 2;
        var required = SnapshotStorage(state, [left, right]) + payload + (format == "wide" ? 8_192 : 0);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;

            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
                state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)).AsTask());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        using (var retry = await state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)))
            Assert.Equal(selector.EndsWith("desc", StringComparison.Ordinal) ? new[] { right, left } : new[] { left, right }, retry.ToArray());
        Assert.True(state.GetArrayAwkValue(Source, left).IsNumberValue);
        Assert.True(state.GetArrayAwkValue(Source, right).IsNumberValue);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [MemberData(nameof(Ties))]
    public async Task NumericTieTextReleasesBeforeReturningTheSnapshot(bool observed, string format, string selector)
    {
        using var memoryForState = State(observed, format, selector);
        var state = memoryForState.State;
        state.SetArrayAwkValue(Source, new AwkArrayKey("a"), AwkValue.FromNumber(1.25));
        state.SetArrayAwkValue(Source, new AwkArrayKey("b"), AwkValue.FromNumber(1.25));
        var payload = AwkRuntimeMemory.EstimateTextBytes(state.FormatStringByteText(AwkValue.FromNumber(1.25)).ByteLength) * 2;
        var containers = SnapshotStorage(state, [new AwkArrayKey("a"), new AwkArrayKey("b")]);
        var required = containers + payload + (format == "wide" ? 8_192 : 0);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - required);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var snapshot = await state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)))
        {
            Assert.Equal(2, snapshot.Count);
            Assert.Equal(before + containers, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    public static IEnumerable<object[]> Nonfinite()
    {
        foreach (var observed in new[] { false, true })
            foreach (var format in new[] { "default", "wide" })
                foreach (var selector in new[] { "@val_str_asc", "@val_str_desc" })
                    yield return [observed, format, selector];
    }

    [Theory]
    [MemberData(nameof(Nonfinite))]
    public async Task ValueSortFailurePreservesDestinationAndSuccessTransfersOnlyNonfiniteText(bool observed, string format, string selector)
    {
        using var memoryForState = State(observed, format, selector);
        var state = memoryForState.State;
        state.ClearArray(Destination);
        var beforeDestination = state.MemoryAccounting.CurrentCommittedBytes;
        state.SetArrayString(Destination, First, "keep");
        var oldBytes = state.MemoryAccounting.CurrentCommittedBytes - beforeDestination;
        var numbers = new[] { 1.25, double.PositiveInfinity, double.NegativeInfinity, double.NaN };
        var expected = numbers.Select((number, index) =>
        {
            var key = new AwkArrayKey("k" + index);
            state.SetArrayAwkValue(Source, key, AwkValue.FromNumber(number));
            return (Number: number, Key: key, Text: state.FormatStringByteText(AwkValue.FromNumber(number)));
        }).ToArray();
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        {
            var beforeFailure = state.MemoryAccounting.CurrentCommittedBytes;
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
                state.SortArrayValuesAsync(Source, Destination, AwkArraySortOrder.Parse(selector), new AwkEvaluationContext(state)).AsTask());
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal("keep", state.GetArrayAwkValue(Destination, First).ByteText.AsString());
            Assert.All(expected, item => Assert.True(state.GetArrayAwkValue(Source, item.Key).IsNumberValue));
            Assert.Equal(beforeFailure, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var count = await state.SortArrayValuesAsync(Source, Destination, AwkArraySortOrder.Parse(selector), new AwkEvaluationContext(state));

        Assert.Equal(numbers.Length, count);
        var ordered = selector.EndsWith("desc", StringComparison.Ordinal)
            ? expected.OrderByDescending(static item => item.Text.AsString(), StringComparer.Ordinal).ToArray()
            : expected.OrderBy(static item => item.Text.AsString(), StringComparer.Ordinal).ToArray();
        for (var index = 0; index < count; index++)
        {
            var value = state.GetArrayAwkValue(Destination, AwkArrayKey.FromOrdinal(index + 1));
            Assert.Equal(double.IsFinite(ordered[index].Number), value.IsNumberValue);
            if (value.IsNumberValue) Assert.Equal(ordered[index].Number, value.AsNumber());
            else Assert.Equal(ordered[index].Text.AsString(), value.ByteText.AsString());
            Assert.True(state.GetArrayAwkValue(Source, ordered[index].Key).IsNumberValue);
        }
        Assert.True(state.MemoryAccounting.CurrentCommittedBytes > before - oldBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.ClearArray(Destination);
        Assert.Equal(before - oldBytes, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StringSortingRetainsExistingTextWithoutCopyingAtItsAdmittedBudget(bool observed)
    {
        using var memoryForState = State(observed, "default", "@val_str_asc");
        var state = memoryForState.State;
        state.SetArrayString(Source, new AwkArrayKey("a"), new string('x', 16_000));
        state.SetArrayString(Source, new AwkArrayKey("b"), new string('y', 16_000));
        var containers = SnapshotStorage(state, [new AwkArrayKey("a"), new AwkArrayKey("b")]);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - containers);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var snapshot = await state.SnapshotArrayKeyValuesAsync(Source, new AwkEvaluationContext(state)))
        {
            Assert.Equal(new[] { new AwkArrayKey("a"), new AwkArrayKey("b") }, snapshot.ToArray());
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void LeasedPrintfResultCopyObservesCancellationAndReleasesBothOwners(bool observed, bool duringCopy)
    {
        using var memoryForState = State(observed, "default", "@val_str_asc");
        var state = memoryForState.State;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var cancellation = new CancellationTokenSource();
        var copyCalls = 0;
        void Checkpoint()
        {
            if (counters.Utf8Encodes == 1 && ++copyCalls == (duringCopy ? 3 : 1))
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        using (var lease = state.MemoryAccounting.CreateTransientLease())
        {
            var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
                "%20000s", new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.Empty), counters), PrintfMissingArgumentPolicy.Throw, counters, state.MemoryAccounting, checkpoint: Checkpoint, retainedTextLease: lease));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(duringCopy ? 3 : 1, copyCalls);
            Assert.Equal(baseline + AwkRuntimeMemory.EstimateTextBytes(20_000), state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    private static (AwkArrayKey Key, AwkText Text)[] Values(AwkRuntimeState state, string format, int count) =>
        Enumerable.Range(1, count).Select(index =>
        {
            var number = format == "integer" ? index : index + 0.25;
            var key = new AwkArrayKey("k" + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
            state.SetArrayAwkValue(Source, key, AwkValue.FromNumber(number));
            return (key, state.FormatStringByteText(AwkValue.FromNumber(number)));
        }).ToArray();

    private static long SnapshotStorage(AwkRuntimeState state, IReadOnlyCollection<AwkArrayKey> keys) =>
        SnapshotContainers(keys.Count) + keys.Sum(key =>
            (key.IsOrdinal ? AwkRuntimeMemory.EstimateTextBytes(key.ByteLength) : key.RetainedTextMemoryBytes) +
            state.GetArrayAwkValue(Source, key).RetainedTextMemoryBytes);

    private static long SnapshotContainers(int count) =>
        AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(count) + AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count);

    private static CooperativeTestMemory State(bool observed, string format, string selector)
    {

        var testMemory = new CooperativeTestMemory(Limit);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options));
        if (format == "wide")
            state.AssignVariableValue(AwkVariableName.FromText("CONVFMT"), AwkValue.FromText(AwkText.FromSourceLiteralString("%8192.2f")));
        state.SetArrayString(AwkArrayName.FromText("PROCINFO"), new AwkArrayKey("sorted_in"), selector);
        return testMemory.Initialize(() => state);
    }
}
