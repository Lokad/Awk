namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldConsumptionTests
{
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
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
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
        Assert.InRange(state.Counters.FieldValueCacheBufferAllocations,1,2);
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
    public void NumericReadsUsePreviouslyFundedOwnedCacheCapacity(bool governed)
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
    public void FullNumericCacheKeepsValuesAndCanGrowForActualScalarEscape(bool governed)
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
        Assert.Equal(7,state.Counters.NumericFieldByteParses);
        Assert.Equal("1 2 3 4 0005 0006",state.CurrentRecordByteText.AsString());
        Assert.Equal(0,state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void NumericCacheAdmissionRefusalDoesNotChangeTheRecordOrLeaveReservations()
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("k 0007");
        state.SetCurrentRecord(record);
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => state.GetFieldNumber(2));
        Assert.Equal(record, state.CurrentRecordByteText);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void NumericCacheUsesExistingRecordLayouts()
    {
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkValue>());
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkFieldText>());
        var entry = Assert.IsAssignableFrom<Type>(typeof(AwkFieldFrame).GetNestedType("CachedFieldValue", System.Reflection.BindingFlags.NonPublic));
        var size = typeof(System.Runtime.CompilerServices.Unsafe).GetMethods()
            .Single(method => method.Name == nameof(System.Runtime.CompilerServices.Unsafe.SizeOf))
            .MakeGenericMethod(entry).Invoke(null, []);
        Assert.Equal(32, Assert.IsType<int>(size));
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
