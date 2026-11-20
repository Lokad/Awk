using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkNumericFieldResourceTests
{
    [Theory]
    [InlineData("%.6g")]
    [InlineData("%.2f")]
    public void NumericAssignmentDoesNotFormatOrAllocateText(string initialFormat)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(AwkRuntimeLimits.CreateDefault(), counters);
        var state = memory.State;
        SetFormat(state, initialFormat);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a b"));
        var allocations = counters[AwkCounter.TextAllocations];

        state.AssignFieldValue(new(1), AwkValue.FromNumber(1.25));

        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.True(state.RecordFields.GetFieldValue(new(1)).IsNumberValue);
        SetFormat(state, "%.4f");
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.2500 b"u8));

        var rebuiltAllocations = counters[AwkCounter.TextAllocations];
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.2500 b"u8));
        Assert.Equal(rebuiltAllocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(1, counters[AwkCounter.FieldSplits]);
        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(1)).AsNumber());

        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, AwkValue.FromNumber(0));
        Assert.Empty(state.CurrentRecordByteText.AsSpan().ToArray());

    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionByteLimitFailurePreservesNumericFieldsAndAllowsRetry(bool changeSeparator)
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        Prepare(state, "%.20f");
        var committed = state.MemoryAccounting.LiveBytes;

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (changeSeparator)
                state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText("|"));
            else
                _ = state.CurrentRecordByteText;
        });

        AssertMemoryFailure(error, state, committed);
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).AsSpan().SequenceEqual(" "u8));
        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(1)).AsNumber());
        Assert.Equal(2, state.FieldCount);
        SetFormat(state, "%.2f");
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.25 b"u8));
    }

    [Fact]
    public void SparseNumericFieldsKeepTheirSpellingOrderAfterTruncation()
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a b c d e"));
        state.AssignFieldValue(new(4), AwkValue.FromNumber(2.75));
        state.AssignFieldValue(new(2), AwkValue.FromNumber(1.25));
        state.AssignFieldValue(new(3), AwkValue.FromText("003"));
        SetFormat(state, "[%.2f]");

        Assert.Equal("a [1.25] 003 [2.75] e", state.CurrentRecordByteText.AsString());

        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, AwkValue.FromNumber(3));
        SetFormat(state, "%.3f");

        Assert.Equal("a 1.250 003", state.CurrentRecordByteText.AsString());
        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(2)).AsNumber());
        Assert.Equal("003", state.RecordFields.GetFieldValue(new(3)).AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CombinedNumericFieldByteLimitFailurePreservesBothFields(bool changeSeparator)
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        Prepare(state, "%.8f");
        state.AssignFieldValue(new(2), AwkValue.FromNumber(2.75));
        var committed = state.MemoryAccounting.LiveBytes;

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (changeSeparator)
                state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText("|"));
            else
                _ = state.CurrentRecordByteText;
        });

        AssertMemoryFailure(error, state, committed);
        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(1)).AsNumber());
        Assert.Equal(2.75, state.RecordFields.GetFieldValue(new(2)).AsNumber());
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).AsSpan().SequenceEqual(" "u8));
        SetFormat(state, "%.2f");
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.25 2.75"u8));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionMemoryLimitFailureReleasesAllTemporaryCharges(bool changeSeparator)
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        Prepare(state, "%.200f");
        var committed = state.MemoryAccounting.LiveBytes;

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (changeSeparator)
                state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText("|"));
            else
                _ = state.CurrentRecordByteText;
        });

        AssertMemoryFailure(error, state, committed);
        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(1)).AsNumber());
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).AsSpan().SequenceEqual(" "u8));
        SetFormat(state, "%.2f");
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.25 b"u8));
    }

    [Fact]
    public void SeparatorGrowthIsReservedBeforeNumericRecordConversion()
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        Prepare(state, "%.80f");
        var committed = state.MemoryAccounting.LiveBytes;
        var separator = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 120)));

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.AssignSpecialVariableValue(
            AwkSpecialVariableId.OutputFieldSeparator, separator));

        AssertMemoryFailure(error, state, committed);
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).AsSpan().SequenceEqual(" "u8));
        SetFormat(state, "%.2f");
        Assert.True(state.CurrentRecordByteText.AsSpan().SequenceEqual("1.25 b"u8));
    }

    [Fact]
    public void CancelledConversionPreservesTheDirtyRecordAndReleasesItsBuffers()
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = State(AwkRuntimeLimits.CreateDefault(), counters, cancellation.Token);
        var state = memory.State;
        Prepare(state, "%.4f");
        var committed = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _ = state.CurrentRecordByteText);

        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);

        Assert.Equal(1.25, state.RecordFields.GetFieldValue(new(1)).AsNumber());
    }

    [Fact]
    public void FieldBufferEstimateIncludesValueTypeEntriesAndArrayHeaders()
    {
        Assert.Equal(AwkRuntimeMemory.FieldTextBytes, Unsafe.SizeOf<AwkFieldText>());
        Assert.Equal(24 + 4 * Unsafe.SizeOf<(int, int, int)>(), AwkRuntimeMemory.EstimateNumericFieldSpellingArrayBytes(4));
        Assert.Equal(0, AwkRuntimeMemory.EstimateNumericFieldSpellingArrayBytes(0));
        Assert.Equal(32 + 24 + 4 * Unsafe.SizeOf<AwkRecordField>(), AwkRuntimeMemory.EstimateRecordFieldListBytes(4));
        Assert.Equal(0, AwkRuntimeMemory.EstimateRecordFieldListBytes(0));
    }

    private static void AssertMemoryFailure(AwkRuntimeException error, AwkRuntimeState state, long committed)
    {
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
    }

    private static void Prepare(AwkRuntimeState state, string format)
    {
        SetFormat(state, format);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a b"));
        state.AssignFieldValue(new(1), AwkValue.FromNumber(1.25));
    }

    private static void SetFormat(AwkRuntimeState state, string format) =>
        state.AssignSpecialVariableValue(AwkSpecialVariableId.ConversionFormat,
            AwkValue.FromText(AwkText.FromSourceLiteralString(format)));

    private static CooperativeTestMemory State(AwkRuntimeLimits limits, AwkRuntimeCounters? counters = null,
        CancellationToken cancellationToken = default)
    {
        var memory = new CooperativeTestMemory(1 << 20);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken,
            memory.Configure(new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters ?? AwkRuntimeCounters.Disabled, limits, regexEngine: AwkUtf8RegexEngine.Instance))));
    }
}
