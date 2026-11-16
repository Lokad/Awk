namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldConsumptionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ScalarEscapesReuseClassificationAndOwnTheOriginalSpelling(bool captureText, bool governed)
    {
        foreach (var spelling in new[] { "0007", "7tail", "7e+", "word" })
        {
            using var memory = State(governed);
            var state = memory.State;
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("k " + spelling));
            var expected = AwkValue.FromNumericString(AwkText.FromSourceLiteralString(spelling));
            Assert.Equal(expected.AsNumber(), state.GetFieldNumber(2));
            Assert.Equal(spelling == "0007", state.TryGetFieldComparisonNumber(2, out _));
            var captured = captureText ? state.CaptureFieldStringText(2) : AwkText.Empty;
            var classifications = state.Counters.TextValueClassifications;
            var value = state.GetFieldValue(2);
            Assert.Equal(classifications, state.Counters.TextValueClassifications);
            Assert.Equal(expected.RuntimeKind, value.RuntimeKind);
            Assert.Equal(expected.AsNumber(), value.AsNumber());
            Assert.Equal(spelling, value.AsString());
            if (captureText) Assert.True(captured.HasSameStorage(value.ByteText));
            state.AssignFieldValue(2, AwkValue.FromText("0007"));
            Assert.Equal(AwkRuntimeValueKind.String, state.GetFieldValue(2).RuntimeKind);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("next"));
            Assert.Equal(spelling, value.AsString());
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CanceledClassifiedEscapeKeepsOwnedSpellingAndRefundsStorage(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(governed, cancellation.Token);
        var state = memory.State;
        var spelling = new string('0', 8192) + "7";
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k " + spelling));
        Assert.True(state.TryGetFieldComparisonNumber(2, out var number));
        Assert.Equal(7, number);
        var captured = state.CaptureFieldStringText(2);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.GetFieldValue(2));
        Assert.Equal(spelling, captured.AsString());
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteArithmeticFactsServeComparisonsWithoutChangingSpelling(bool governed)
    {
        foreach (var mode in Enum.GetValues<AwkNumberParsingMode>())
        foreach (var spelling in new[] { "0007", " 7", "1.25", "-0.0", "1e309", "1e-9999", "nan", "0x1p2", "077" })
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
            using var context = new AwkFieldTestContext(counters, memory.Accounting, separator: "|");
            var frame = context.Frame;
            var text = AwkText.FromSourceLiteralString(spelling);
            frame.SetRecordText(AwkText.FromSourceLiteralString(spelling + "|tail"));
            var expected = AwkValue.FromNumericString(text, mode);
            var number = frame.GetFieldNumber(new(1), mode);
            Assert.Equal(BitConverter.DoubleToUInt64Bits(expected.AsNumber()), BitConverter.DoubleToUInt64Bits(number));
            var strict = expected.TryGetStoredComparisonNumber(out var expectedNumber, null);
            Assert.Equal(strict, frame.TryGetStoredFieldComparisonNumber(new(1), out var actualNumber));
            if (strict)
                Assert.Equal(BitConverter.DoubleToUInt64Bits(expectedNumber), BitConverter.DoubleToUInt64Bits(actualNumber));
            Assert.Equal(strict, frame.TryGetFieldComparisonNumber(new(1), mode, out _));
            Assert.Equal(strict ? 1 : 2, counters.NumericFieldByteParses);
            Assert.Equal(0, counters.FieldValueCacheBufferAllocations);
            Assert.True(frame.TryGetFieldTextSnapshot(new(1), out var captured));
            Assert.Equal(spelling, captured.AsString());
            frame.SetRecordText(AwkText.FromSourceLiteralString("next"));
            Assert.Equal(spelling, captured.AsString());
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        }
    }

    [Theory]
    [InlineData("7tail", false)] [InlineData("7tail", true)]
    [InlineData("7e+", false)] [InlineData("7e+", true)]
    [InlineData("7\u00e9", false)] [InlineData("7\u00e9", true)]
    [InlineData("7 ", false)] [InlineData("7 ", true)]
    [InlineData("", false)] [InlineData("", true)]
    public void IncompleteArithmeticFactsRetainStrictClassificationAtUse(string spelling, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        using var context = new AwkFieldTestContext(counters, memory.Accounting, separator: "|");
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString(spelling + "|tail"));
        Assert.Equal(spelling.Length == 0 ? 0 : 7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        Assert.False(frame.TryGetStoredFieldComparisonNumber(new(1), out _));
        Assert.Equal(spelling == "7 ", frame.TryGetFieldComparisonNumber(new(1), AwkNumberParsingMode.Decimal, out _));
        Assert.Equal(2, counters.NumericFieldByteParses);
        Assert.Equal(spelling, frame.GetFieldValue(new(1)).AsString());
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericCacheFollowsSlicesThroughCompletionAndReconstruction(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 " +
            string.Join(' ', Enumerable.Repeat("tail", 64))));
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(1, state.Counters.NumericFieldByteParses);
        Assert.Equal(66, state.FieldCount);
        Assert.Equal(7, state.GetFieldNumber(2));
        state.AssignFieldValue(3, AwkValue.FromText("changed"));
        Assert.StartsWith("k 0007 changed ", state.CurrentRecordByteText.AsString());
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(1, state.Counters.NumericFieldByteParses);
        Assert.Equal("0007", state.GetFieldValue(2).AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, state.GetFieldValue(2).RuntimeKind);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var allocations = counters.AwkTextAllocations;
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        for (var repeat = 0; repeat < 8; repeat++) Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(1, counters.NumericFieldByteParses);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 008"));
        Assert.Equal(8, state.GetFieldNumber(2));
        Assert.Equal(2, counters.NumericFieldByteParses);
        state.AssignFieldValue(2, AwkValue.FromNumber(1.25));
        Assert.Equal(1.25, state.GetFieldNumber(2));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericReadsKeepTextViewsAndEscapingValuesDistinct(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 12x abc"));
        var textAllocations = state.Counters.AwkTextAllocations;
        for (var repeat = 0; repeat < 8; repeat++)
        {
            Assert.Equal(7, state.GetFieldNumber(2));
            Assert.Equal(12, state.GetFieldNumber(3));
            Assert.Equal(0, state.GetFieldNumber(4));
        }
        Assert.Equal(3, state.Counters.NumericFieldByteParses);
        Assert.Equal(textAllocations, state.Counters.AwkTextAllocations);
        Assert.True(state.TryGetPrintfFieldBytes(2, out var bytes));
        Assert.True(bytes.SequenceEqual("0007"u8));
        var retained = state.GetFieldValue(2);
        Assert.Equal("0007", retained.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, retained.RuntimeKind);
        Assert.False(AwkComparisonOperations.Evaluate(state.GetFieldValue(3), AwkValue.FromNumber(12), AwkComparison.Equal, state));
        Assert.True(state.GetFieldValue(4).IsTruthy());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next 9"));
        Assert.Equal("0007", retained.AsString());
        Assert.Equal(9, state.GetFieldNumber(2));
        Assert.Equal(4, state.Counters.NumericFieldByteParses);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false,128)] [InlineData(true,128)]
    [InlineData(false,256)] [InlineData(true,256)]
    public void OnePassWideNumbersReuseBoundedCacheStorage(bool governed,int fields)
    {
        using var memory = State(governed);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString(string.Join(' ',Enumerable.Range(1,fields)));
        const int records=8;
        for(var row=0;row<records;row++)
        {
            state.SetCurrentRecord(record);
            Assert.Equal(fields,state.FieldCount);
            for(var field=1;field<=fields;field++) Assert.Equal(field,state.GetFieldNumber(field));
            Assert.Equal(1,state.GetFieldNumber(1));
            Assert.Equal(record,state.CurrentRecordByteText);
        }
        Assert.Equal(0,state.Counters.FieldValueCacheBufferAllocations);
        Assert.Equal(records*fields,state.Counters.NumericFieldByteParses);
        Assert.Equal(0,state.Counters.StringDecodes);
        Assert.Equal(0,state.Counters.Utf8Encodes);
        Assert.Equal(0,state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0,memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,256)] [InlineData(true,256)]
    public void FirstAccessToAnEarlyOrLateFieldCanReuseItsNumber(bool governed,int field)
    {
        using var memory=State(governed);
        var state=memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ',Enumerable.Repeat("0007",512))));
        var texts=state.Counters.AwkTextAllocations;
        for(var repeat=0;repeat<8;repeat++)Assert.Equal(7,state.GetFieldNumber(field));
        Assert.Equal(1,state.Counters.NumericFieldByteParses);
        Assert.Equal(texts,state.Counters.AwkTextAllocations);
        Assert.True(state.Counters.FieldsMaterialized<=Math.Max(field,1));
        Assert.Equal("0007",state.GetFieldValue(field).AsString());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericReadsLeavePreviouslyFundedOwnedCacheStorageUnchanged(bool governed)
    {
        using var memory=State(governed);
        var state=memory.State;
        var record=AwkText.FromSourceLiteralString(string.Join(' ',Enumerable.Repeat("0007",12)));
        state.SetCurrentRecord(record);
        for(var field=1;field<=12;field++)Assert.Equal("0007",state.GetFieldValue(field).AsString());
        var growths=state.Counters.FieldValueCacheBufferAllocations;
        state.SetCurrentRecord(record);
        for(var repeat=0;repeat<8;repeat++)
            for(var field=1;field<=12;field++)Assert.Equal(7,state.GetFieldNumber(field));
        Assert.Equal(12,state.Counters.NumericFieldByteParses);
        Assert.Equal(growths,state.Counters.FieldValueCacheBufferAllocations);
        Assert.Equal(0,state.Counters.StringDecodes);
        Assert.Equal(0,state.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InlineNumbersKeepValuesAndCanMaterializeActualScalarEscapes(bool governed)
    {
        using var memory=State(governed);
        var state=memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("1 2 3 4 0005 0006"));
        for(var field=1;field<=4;field++)Assert.Equal(field,state.GetFieldNumber(field));
        var growths=state.Counters.FieldValueCacheBufferAllocations;
        for(var repeat=0;repeat<2;repeat++)Assert.Equal(5,state.GetFieldNumber(5));
        Assert.Equal(growths,state.Counters.FieldValueCacheBufferAllocations);
        Assert.Equal("0005",state.GetFieldValue(5).AsString());
        for(var repeat=0;repeat<8;repeat++)Assert.Equal(6,state.GetFieldNumber(6));
        Assert.Equal(6,state.Counters.NumericFieldByteParses);
        Assert.Equal("1 2 3 4 0005 0006",state.CurrentRecordByteText.AsString());
        Assert.Equal(0,state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void NumericReadsNeedNoAdmissionAndScalarEscapeRefusalKeepsTheRecord()
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("k 0007");
        state.SetCurrentRecord(record);
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.True(state.TryGetFieldComparisonNumber(2, out var comparison));
        Assert.Equal(7, comparison);
        Assert.Equal(1, state.Counters.NumericFieldByteParses);
        Assert.Equal(0, state.Counters.FieldValueCacheBufferAllocations);
        Assert.Throws<AwkRuntimeException>(() => state.GetFieldValue(2));
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.Equal(record, state.CurrentRecordByteText);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void StoredComparisonProbeCannotSplitOrAdmitAndNeverPromotesArithmeticPrefixes()
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k 0007 7tail"));
        var splits = state.Counters.FieldSplits;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.False(state.TryGetStoredFieldComparisonNumber(2, out _));
        Assert.False(state.TryGetStoredFieldComparisonNumber(-1, out _));
        Assert.False(state.TryGetStoredFieldComparisonNumber(0, out _));
        Assert.Equal(splits, state.Counters.FieldSplits);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = null;
        Assert.Equal(3, state.FieldCount);
        Assert.Equal(7, state.GetFieldNumber(2));
        Assert.True(state.TryGetStoredFieldComparisonNumber(2, out _));
        Assert.True(state.TryGetFieldComparisonNumber(2, out _));
        Assert.True(state.TryGetStoredFieldComparisonNumber(2, out var number));
        Assert.Equal(7, number);
        Assert.Equal(7, state.GetFieldNumber(3));
        Assert.False(state.TryGetStoredFieldComparisonNumber(3, out _));
        Assert.False(state.TryGetFieldComparisonNumber(3, out _));
        Assert.False(state.TryGetStoredFieldComparisonNumber(3, out _));
        Assert.Equal(0, state.Counters.FieldValueCacheBufferAllocations);
    }

    [Fact]
    public void StrictComparisonFactsNeedNoAdmissionAndEscapeRefusalPreservesThem()
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("k 0007");
        state.SetCurrentRecord(record);
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        for (var repeat = 0; repeat < 8; repeat++)
        {
            Assert.True(state.TryGetFieldComparisonNumber(2, out var number));
            Assert.Equal(7, number);
            Assert.Equal(7, state.GetFieldNumber(2));
        }
        Assert.Equal(0, state.Counters.FieldValueCacheBufferAllocations);
        Assert.Throws<AwkRuntimeException>(() => state.GetFieldValue(2));
        Assert.True(state.TryGetFieldComparisonNumber(2, out var retained));
        Assert.Equal(7, retained);
        Assert.Equal(record, state.CurrentRecordByteText);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.GetFieldNumber(2));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
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
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
