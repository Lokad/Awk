namespace Lokad.Awk.Tests;

public sealed partial class AwkFieldProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnedFieldValuesUsePrefixBoundsAndChargeCompletionWork(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var memoryForEager = State(finite);
        var eager = memoryForEager.State;
        var eagerCounters = eager.Counters;
        var source = AwkText.FromString("0007 worker café " + string.Join(" ", Enumerable.Repeat("tail", 61)));
        state.SetCurrentRecord(source);
        eager.SetCurrentRecord(source);
        Assert.Equal(64, eager.FieldCount);
        var first = state.RecordFields.GetFieldValue(new(1));
        var firstEager = eager.RecordFields.GetFieldValue(new(1));
        AssertSameScalar(firstEager, first);
        Assert.Equal("0007"u8.ToArray(), first.ByteText.AsSpan().ToArray());
        Assert.Equal(1, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(64, eagerCounters[AwkCounter.FieldsPrepared]);
        var third = state.RecordFields.GetFieldValue(new(3));
        var thirdEager = eager.RecordFields.GetFieldValue(new(3));
        AssertSameScalar(thirdEager, third);
        Assert.Equal(3, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(64, state.FieldCount);
        Assert.Equal(64, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(first, state.RecordFields.GetFieldValue(new(1)));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);

        static void AssertSameScalar(AwkValue expected, AwkValue actual)
        {
            Assert.Equal(expected.RuntimeKind, actual.RuntimeKind);
            Assert.Equal(expected.ByteText, actual.ByteText);
            Assert.Equal(expected.AsNumber(), actual.AsNumber());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectProjectionCopiesBeforeBorrowedRecordMutationAndAvoidsFieldValues(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting);
        var bytes = "first second"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        _ = state.FieldCount;
        // Warm the ordinary ORS binding before comparing field-specific counters.
        _ = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        var allocations = counters[AwkCounter.TextAllocations];

        state.AppendFieldPrint(output, new(1));
        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        // ORS is already canonical, and the projected field creates no scalar.

        AwkTestHarness.PublishBufferedRecord(state, "next"u8);
        AwkTestHarness.PublishBufferedRecord(state, "next"u8);
        Assert.True(original.Span.StartsWith("next"u8));
        state.AppendFieldPrint(output, new(1));
        Assert.Equal("first\nnext\n", output.ToString());
    }
    [Fact]
    public void FailedCombinedAppendDoesNotPublishTheFieldBeforeItsTerminator()
    {
        using var memoryForState = State(true);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting);
        var prefix = new string('k', AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity - 4);
        output.Append(AwkText.FromString(prefix));
        state.SetCurrentRecord(AwkText.FromString("1234"));
        _ = state.FieldCount;
        _ = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => state.AppendFieldPrint(output, new(1)));
        Assert.Equal(prefix, output.ToString());
        state.SetCurrentRecord(AwkText.FromString("ok"));
        state.AppendFieldPrint(output, new(1));
        Assert.Equal(prefix + "ok\n", output.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachedNumericFieldsUsePrintFormattingAndUnicodeSlicesRemainRaw(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting);
        state.SetCurrentRecord(AwkText.FromString("café 😀"));
        state.AppendFieldPrint(output, new(1));
        state.AppendFieldPrint(output, new(2));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFormat, AwkValue.FromText("%.2f"));
        state.AssignFieldValue(new(1), AwkValue.FromNumber(1.25));
        state.AppendFieldPrint(output, new(1));
        Assert.Equal("café\n😀\n1.25\n", output.ToString());
        Assert.True(state.RecordFields.GetFieldValue(new(1)).IsNumberValue);
    }

    [Fact]
    public void CancellationDuringPairCopyLeavesPublishedLengthUnchanged()
    {
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled);
        var ending = AwkText.FromSourceLiteralString("end");
        var shortChecks = 0;
        builder.Append(new byte[16_384], ending, () => shortChecks++);
        builder.Clear();
        builder.Append("keep"u8);
        var content = AwkUtf8Codec.Encode(new string('x', 32_768));
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var failure = Assert.Throws<OperationCanceledException>(() => builder.Append(content.Span, ending, () =>
        {
            if (++checks > shortChecks) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(builder.AsSpan().SequenceEqual("keep"u8));
        builder.Append(content.Span, ending);
        Assert.Equal("keep" + new string('x', 32_768) + "end", AwkUtf8Codec.Decode(builder.AsSpan()));
    }

    private static CooperativeTestMemory State(bool finite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, enabled: finite);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options)));
    }
}
