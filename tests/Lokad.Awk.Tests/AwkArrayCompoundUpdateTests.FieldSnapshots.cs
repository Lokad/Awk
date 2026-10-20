namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    private static AwkArrayIndex LiteralField(int index) => AwkArrayIndex.Simple(
        AwkExpression.FieldIndex(AwkExpression.Number(index.ToString(System.Globalization.CultureInfo.InvariantCulture), index)));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiteralFieldSnapshotSurvivesPendingRightAndRecordReuse(bool pending)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var bytes = "hit old"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        var rightSource = new ValueSource(3, pending);
        var right = new EffectExpression(rightSource, () =>
        {
            Assert.Equal(1, state.Counters.BorrowedFieldTargetSnapshots);
            state.DeleteArrayValue(name, key);
            Array.Fill(bytes, (byte)'x');
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("other record"));
            state.SetArrayAwkValue(name, key, AwkValue.FromNumber(100));
        });
        var result = AwkReference.ArrayElement(name, LiteralField(1)).ApplyCompoundAssignmentAsync(
            new(state), right, AwkCompoundAssignmentOperation.Add);
        if (pending)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, key).AsNumber());
            rightSource.Complete();
        }
        Assert.Equal(10, (await result).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("xxx")));
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("other")));
        Assert.Equal(1, rightSource.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LiteralFieldSnapshotRightFailureKeepsOriginalText(bool pending, bool cancel)
    {
        using var memoryForState = State();
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromText("007"));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        var source = new ValueSource(3, true);
        Exception error = cancel ? new OperationCanceledException() : new AwkRuntimeException("test RHS failure"u8.ToArray());
        if (!pending) source.Fail(error);
        var result = AwkReference.ArrayElement(name, LiteralField(1)).ApplyCompoundAssignmentAsync(new(state),
            new EffectExpression(source, () => { }), AwkCompoundAssignmentOperation.Add);
        if (pending)
        {
            Assert.False(result.IsCompleted);
            source.Fail(error);
        }
        var observed = await Record.ExceptionAsync(() => result.AsTask());
        Assert.Same(error, observed);
        Assert.Equal("007", state.GetArrayAwkValue(name, key).AsString());
        Assert.Equal(1, state.Counters.BorrowedFieldTargetSnapshots);
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task LiteralFieldSnapshotKeepsFullArrayHitsAndMissingRefusal(int mode, bool missing)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits) { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        var effects = 0;
        var source = new ValueSource(3, false);
        var target = AwkReference.ArrayElement(name, LiteralField(missing ? 2 : 1));
        var right = new EffectExpression(source, () => effects++);
        if (missing) budget.RefuseNextAdmissionMinimumGrowth = 1;
        if (missing)
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await target.ApplyCompoundAssignmentAsync(new(state), right, AwkCompoundAssignmentOperation.Add));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        }
        else Assert.Equal(10, (await target.ApplyCompoundAssignmentAsync(new(state), right, AwkCompoundAssignmentOperation.Add)).AsNumber());
        Assert.Equal(missing ? 0 : 1, effects);
        Assert.Equal(missing ? 0 : 1, source.Consumptions);
        Assert.Equal(missing ? 0 : 1, counters.BorrowedFieldTargetSnapshots);
        Assert.Equal(missing ? 7 : 10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await owner.DisposeAsync();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiteralFieldSnapshotMatchesOriginal(bool missing)
    {
        {
            var fast = await RunFieldTarget(false, missing);
            var original = await RunFieldTarget(true, missing);
            Assert.Equal(original, fast with { Snapshots = original.Snapshots });
            Assert.Equal(missing ? 0 : 1, fast.Snapshots);
            Assert.Equal(0, original.Snapshots);
        }
    }

    private static async Task<FieldTargetResult> RunFieldTarget(bool original, bool missing)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit missing"));
        var effects = 0;
        var source = new ValueSource(3, false);
        var index = LiteralField(missing ? 2 : 1);
        var error = "";
        try
        {
            await AwkReference.ArrayElement(name, original ? new ForwardingFieldTarget(index) : index)
                .ApplyCompoundAssignmentAsync(new(state), new EffectExpression(source, () => effects++), AwkCompoundAssignmentOperation.Add);
        }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var exists = state.TryPeekArrayAwkValue(name, new AwkArrayKey(missing ? "missing" : "hit"), out var value);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        return new(exists, exists ? value.AsNumber() : 0, effects, counters.MemoryPeakLiveBytes, counters.BorrowedFieldTargetSnapshots, error);
    }

    private sealed record ForwardingFieldTarget(AwkArrayIndex Inner) : AwkArrayIndex
    {
        public override ValueTask<AwkArrayKey> ResolveKeyAsync(AwkEvaluationContext context) => Inner.ResolveKeyAsync(context);
        internal override ValueTask<AwkArrayKey> ResolveTargetKeyAsync(AwkEvaluationContext context, AwkArrayName array) =>
            Inner.ResolveTargetKeyAsync(context, array);
    }

    private readonly record struct FieldTargetResult(bool Exists, double Value, int Effects, long Peak, long Snapshots, string Error);

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("1e0", 1)]
    [InlineData("01", 1)]
    [InlineData("0x1", 1)]
    [InlineData("010", 8)]
    [InlineData("0", 0)]
    [InlineData("-1", -1)]
    public async Task NumericFieldTargetIndicesKeepRuntimeEvaluation(string text, double number)
    {
        var results = new List<(double Value, string Error)>();
        foreach (var original in new[] { false, true })
        {
            using var memoryForState = State();
            var state = memoryForState.State;
            var name = AwkArrayName.FromText("values");
            const string record = "hit hit hit hit hit hit hit hit";
            var key = new AwkArrayKey(number == 0 ? record : "hit");
            state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
            state.SetCurrentRecord(AwkText.FromSourceLiteralString(record));
            var index = AwkArrayIndex.Simple(AwkExpression.FieldIndex(AwkExpression.Number(text, number)));
            var error = "";
            try
            {
                await AwkReference.ArrayElement(name, original ? new ForwardingFieldTarget(index) : index)
                    .ApplyCompoundAssignmentAsync(new(state), AwkExpression.Number("3", 3), AwkCompoundAssignmentOperation.Add);
            }
            catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
            results.Add((state.GetArrayAwkValue(name, key).AsNumber(), error));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(results[1], results[0]);
        Assert.Equal(number < 0 ? 7 : 10, results[0].Value);
        Assert.Equal(number < 0 ? "awk: negative field index\n" : "", results[0].Error);
    }
}
