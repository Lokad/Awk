namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldConsumptionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ScalarEscapesReuseClassificationAndOwnTheOriginalSpelling(bool captureText, bool governed)
    {
        foreach (var spelling in new[] { "0007", "7tail", "7e+", "word" })
        {
            using var memory = State(governed);
            var state = memory.State;
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("k " + spelling));
            var expected = AwkTestHarness.NumericInput(AwkText.FromSourceLiteralString(spelling));
            Assert.Equal(expected.AsNumber(), state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
            Assert.Equal(spelling == "0007", state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out _));
            var captured = captureText ? state.CaptureFieldStringText(2) : AwkText.Empty;

            var value = state.GetFieldValue(2);

            Assert.Equal(expected.RuntimeKind, value.RuntimeKind);
            Assert.Equal(expected.AsNumber(), value.AsNumber());
            Assert.Equal(spelling, value.AsString());
            if (captureText) Assert.True(captured.HasSameStorage(value.ByteText));
            state.AssignFieldValue(new(2), AwkValue.FromText("0007"));
            Assert.Equal(AwkRuntimeValueKind.String, state.GetFieldValue(2).RuntimeKind);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("next"));
            Assert.Equal(spelling, value.AsString());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledClassifiedEscapeKeepsOwnedSpellingAndRefundsStorage(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(governed, cancellation.Token);
        var state = memory.State;
        var spelling = new string('0', 8192) + "7";
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k " + spelling));
        Assert.True(state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out var number));
        Assert.Equal(7, number);
        var captured = state.CaptureFieldStringText(2);
        var committed = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.GetFieldValue(2));
        Assert.Equal(spelling, captured.AsString());
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteArithmeticFactsServeComparisonsWithoutChangingSpelling(bool governed)
    {
        foreach (var mode in Enum.GetValues<AwkNumberParsingMode>())
            foreach (var spelling in new[] { "0007", " 7", "1.25", "-0.0", "1e309", "1e-9999", "nan", "0x1p2", "077" })
            {
                var counters = AwkRuntimeCounters.CreateEnabled();
                var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters);
                var memoryBudget = memory.Budget;
                using (memory)
                {
                    memory.Budget.SetAvailableBytes(memory.Budget.Limit);
                    using var context = new AwkFieldTestContext(memory, separator: "|");
                    var frame = context.Frame;
                    var text = AwkText.FromSourceLiteralString(spelling);
                    frame.SetRecordText(AwkText.FromSourceLiteralString(spelling + "|tail"));
                    var expected = AwkTestHarness.NumericInput(text, mode);
                    var number = frame.GetFieldNumber(new(1), mode);
                    Assert.Equal(BitConverter.DoubleToUInt64Bits(expected.AsNumber()), BitConverter.DoubleToUInt64Bits(number));
                    var strict = expected.TryGetStoredComparisonNumber(out var expectedNumber, null);
                    Assert.Equal(strict, frame.TryGetStoredFieldComparisonNumber(new(1), out var actualNumber));
                    if (strict)
                        Assert.Equal(BitConverter.DoubleToUInt64Bits(expectedNumber), BitConverter.DoubleToUInt64Bits(actualNumber));
                    Assert.Equal(strict, frame.TryGetFieldComparisonNumber(new(1), mode, out _));

                    Assert.True(frame.TryGetFieldTextSnapshot(new(1), out var captured));
                    Assert.Equal(spelling, captured.AsString());
                    frame.SetRecordText(AwkText.FromSourceLiteralString("next"));
                    Assert.Equal(spelling, captured.AsString());
                }
                Assert.Equal(0, memoryBudget.Reserved);
            }
    }

    [Theory]
    [InlineData("7tail", false)]
    [InlineData("7tail", true)]
    [InlineData("7e+", false)]
    [InlineData("7e+", true)]
    [InlineData("7\u00e9", false)]
    [InlineData("7\u00e9", true)]
    [InlineData("7 ", false)]
    [InlineData("7 ", true)]
    [InlineData("", false)]
    [InlineData("", true)]
    public void IncompleteArithmeticFactsRetainStrictClassificationAtUse(string spelling, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: "|");
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(spelling + "|tail"));
            Assert.Equal(spelling.Length == 0 ? 0 : 7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            Assert.False(frame.TryGetStoredFieldComparisonNumber(new(1), out _));
            Assert.Equal(spelling == "7 ", frame.TryGetFieldComparisonNumber(new(1), AwkNumberParsingMode.Decimal, out _));

            Assert.Equal(spelling, frame.GetFieldValue(new(1)).AsString());
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericCacheFollowsSlicesThroughCompletionAndReconstruction(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 " +
            string.Join(' ', Enumerable.Repeat("tail", 64))));
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));

        Assert.Equal(66, state.FieldCount);
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        state.AssignFieldValue(new(3), AwkValue.FromText("changed"));
        Assert.StartsWith("k 0007 changed ", state.CurrentRecordByteText.AsString());
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));

        Assert.Equal("0007", state.GetFieldValue(2).AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, state.GetFieldValue(2).RuntimeKind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericReadsReuseInterpretationAndRespectRecordReplacementAndFieldWrites(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        var bytes = "k 007"u8.ToArray();
        AwkTestHarness.PublishBufferedRecord(state, bytes);
        _ = state.FieldCount;
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        var allocations = counters[AwkCounter.TextAllocations];
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        for (var repeat = 0; repeat < 8; repeat++) Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));

        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 008"));
        Assert.Equal(8, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));

        state.AssignFieldValue(new(2), AwkValue.FromNumber(1.25));
        Assert.Equal(1.25, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericReadsKeepTextViewsAndEscapingValuesDistinct(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 12x abc"));
        var textAllocations = state.Counters[AwkCounter.TextAllocations];
        for (var repeat = 0; repeat < 8; repeat++)
        {
            Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
            Assert.Equal(12, state.RecordFields.GetFieldNumber(new(3), state.NumberParsingMode));
            Assert.Equal(0, state.RecordFields.GetFieldNumber(new(4), state.NumberParsingMode));
        }

        Assert.Equal(textAllocations, state.Counters[AwkCounter.TextAllocations]);
        Assert.True(state.TryGetPrintfFieldBytes(2, out var bytes));
        Assert.True(bytes.SequenceEqual("0007"u8));
        var retained = state.GetFieldValue(2);
        Assert.Equal("0007", retained.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, retained.RuntimeKind);
        Assert.False(AwkComparisonOperations.Evaluate(state.GetFieldValue(3), AwkValue.FromNumber(12), AwkComparison.Equal, state));
        Assert.True(state.GetFieldValue(4).IsTruthy());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next 9"));
        Assert.Equal("0007", retained.AsString());
        Assert.Equal(9, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
    }

    [Theory]
    [InlineData(false, 128)]
    [InlineData(true, 128)]
    [InlineData(false, 256)]
    [InlineData(true, 256)]
    public void OnePassWideNumbersReuseBoundedCacheStorage(bool governed, int fields)
    {
        using var memory = State(governed);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Range(1, fields)));
        const int records = 8;
        for (var row = 0; row < records; row++)
        {
            state.SetCurrentRecord(record);
            Assert.Equal(fields, state.FieldCount);
            for (var field = 1; field <= fields; field++) Assert.Equal(field, state.RecordFields.GetFieldNumber(new(field), state.NumberParsingMode));
            Assert.Equal(1, state.RecordFields.GetFieldNumber(new(1), state.NumberParsingMode));
            Assert.Equal(record, state.CurrentRecordByteText);
        }

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 256)]
    [InlineData(true, 256)]
    public void FirstAccessToAnEarlyOrLateFieldCanReuseItsNumber(bool governed, int field)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("0007", 512))));
        var texts = state.Counters[AwkCounter.TextAllocations];
        for (var repeat = 0; repeat < 8; repeat++) Assert.Equal(7, state.RecordFields.GetFieldNumber(new(field), state.NumberParsingMode));

        Assert.Equal(texts, state.Counters[AwkCounter.TextAllocations]);
        Assert.True(state.Counters[AwkCounter.FieldsPrepared] <= Math.Max(field, 1));
        Assert.Equal("0007", state.GetFieldValue(field).AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericReadsLeavePreviouslyFundedOwnedCacheStorageUnchanged(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("0007", 12)));
        state.SetCurrentRecord(record);
        for (var field = 1; field <= 12; field++) Assert.Equal("0007", state.GetFieldValue(field).AsString());

        state.SetCurrentRecord(record);
        for (var repeat = 0; repeat < 8; repeat++)
            for (var field = 1; field <= 12; field++) Assert.Equal(7, state.RecordFields.GetFieldNumber(new(field), state.NumberParsingMode));

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InlineNumbersKeepValuesAndCanMaterializeActualScalarEscapes(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("1 2 3 4 0005 0006"));
        for (var field = 1; field <= 4; field++) Assert.Equal(field, state.RecordFields.GetFieldNumber(new(field), state.NumberParsingMode));

        for (var repeat = 0; repeat < 2; repeat++) Assert.Equal(5, state.RecordFields.GetFieldNumber(new(5), state.NumberParsingMode));

        Assert.Equal("0005", state.GetFieldValue(5).AsString());
        for (var repeat = 0; repeat < 8; repeat++) Assert.Equal(6, state.RecordFields.GetFieldNumber(new(6), state.NumberParsingMode));

        Assert.Equal("1 2 3 4 0005 0006", state.CurrentRecordByteText.AsString());
    }

    [Fact]
    public void NumericReadsNeedNoAdmissionAndScalarEscapeRefusalKeepsTheRecord()
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("k 0007");
        state.SetCurrentRecord(record);
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        Assert.True(state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out var comparison));
        Assert.Equal(7, comparison);

        Assert.Throws<AwkRuntimeException>(() => state.GetFieldValue(2));
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        Assert.Equal(record, state.CurrentRecordByteText);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void StoredComparisonProbeCannotSplitOrAdmitAndNeverPromotesArithmeticPrefixes()
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 7tail"));
        var splits = state.Counters[AwkCounter.FieldSplits];
        var committed = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.False(state.RecordFields.TryGetStoredFieldComparisonNumber(new(2), out _));
        Assert.False(state.RecordFields.TryGetStoredFieldComparisonNumber(new(0), out _));
        Assert.Equal(splits, state.Counters[AwkCounter.FieldSplits]);
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = null;
        Assert.Equal(3, state.FieldCount);
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        Assert.True(state.RecordFields.TryGetStoredFieldComparisonNumber(new(2), out _));
        Assert.True(state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out _));
        Assert.True(state.RecordFields.TryGetStoredFieldComparisonNumber(new(2), out var number));
        Assert.Equal(7, number);
        Assert.Equal(7, state.RecordFields.GetFieldNumber(new(3), state.NumberParsingMode));
        Assert.False(state.RecordFields.TryGetStoredFieldComparisonNumber(new(3), out _));
        Assert.False(state.RecordFields.TryGetFieldComparisonNumber(new(3), state.NumberParsingMode, out _));
        Assert.False(state.RecordFields.TryGetStoredFieldComparisonNumber(new(3), out _));

    }

    [Fact]
    public void StrictComparisonFactsNeedNoAdmissionAndEscapeRefusalPreservesThem()
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("k 0007");
        state.SetCurrentRecord(record);
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var repeat = 0; repeat < 8; repeat++)
        {
            Assert.True(state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out var number));
            Assert.Equal(7, number);
            Assert.Equal(7, state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        }

        Assert.Throws<AwkRuntimeException>(() => state.GetFieldValue(2));
        Assert.True(state.RecordFields.TryGetFieldComparisonNumber(new(2), state.NumberParsingMode, out var retained));
        Assert.Equal(7, retained);
        Assert.Equal(record, state.CurrentRecordByteText);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void RecordCellAndTextLayoutsMatchTheirOwners()
    {
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkValue>());
        Assert.Equal(AwkRuntimeMemory.FieldTextBytes, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkFieldText>());
        Assert.Equal(AwkRuntimeMemory.RecordFieldBytes, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkRecordField>());
        Assert.False(System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<AwkRecordField>());
        Assert.Equal(AwkFieldTextKind.Empty, AwkRecordField.Empty.Kind);
        Assert.Equal(AwkFieldTextKind.Empty, AwkRecordField.FromOwned(1, 0).Kind);
        Assert.Equal(AwkFieldTextKind.Owned, AwkRecordField.FromOwned(1, 4).Kind);
        var field = AwkRecordField.FromRecordSlice(1, 5);
        field = field.WithRuneLength(4);
        field.WriteNumericValue(out var numeric, 7, completeInput: false);
        field = numeric.WithCacheSlot(2);
        Assert.Equal(AwkFieldTextKind.RecordSlice, field.Kind);
        Assert.Equal("café"u8.ToArray(), field.BytesInRecord(" café!"u8).ToArray());
        Assert.Equal(4, field.RuneLength);
        Assert.Equal(AwkRuntimeMemory.CachedFieldValueBytes,
            System.Runtime.CompilerServices.Unsafe.SizeOf<AwkFieldFrame.CachedFieldValue>());
    }

    [Fact]
    public void RecordCachePublicationPreservesCapturedCellsAndSliceCoordinates()
    {
        var fields = new AwkRecordFieldBuffer(1);
        fields.Add(AwkRecordField.FromRecordSlice(1, 5));
        var original = fields[0];
        fields.PublishNumericValue(0, in original, 7, completeInput: false);
        fields[0] = fields[0].WithRuneLength(4);
        var captured = fields[0];
        fields.EnsureCapacity(4);
        fields.PublishNumericValue(0, in captured, 8, completeInput: true);

        Assert.False(original.TryGetNumericValue(out _));
        Assert.Equal(-1, original.RuneLength);
        Assert.True(captured.TryGetNumericValue(out var earlier));
        Assert.Equal(7, earlier);
        Assert.Equal(4, captured.RuneLength);
        Assert.True(fields[0].TryGetNumericValue(out var current));
        Assert.Equal(8, current);
        Assert.True(fields[0].HasInputClassification);
        Assert.True(fields[0].TryGetComparisonNumber(out var comparison));
        Assert.Equal(8, comparison);
        Assert.Equal(4, fields[0].RuneLength);
        Assert.Equal("café"u8.ToArray(), fields[0].BytesInRecord(" café!"u8).ToArray());
        Assert.Equal(original.RecordStart, fields[0].RecordStart);
        Assert.Equal(original.ByteLength(), fields[0].ByteLength());
    }

    [Fact]
    public void CanceledNumericReadKeepsRecordAndReleasesTemporaryAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        Assert.True(state.MemoryAccounting.IsEnabled);
        var record = AwkText.FromString("k " + new string('1', 16_384));
        state.SetCurrentRecord(record);
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.RecordFields.GetFieldNumber(new(2), state.NumberParsingMode));
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        Assert.Equal(record, state.CurrentRecordByteText);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static CooperativeTestMemory State(bool governed, CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
