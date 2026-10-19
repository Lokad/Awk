namespace Lokad.Awk.Tests;

public sealed partial class AwkFieldProjectionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OwnedExpressionsUsePrefixBoundsAndChargeCompletionWork(bool dynamicIndex, bool finite)
    {
        var (state, counters) = State(finite);
        var (eager, eagerCounters) = State(finite);
        var source = AwkText.FromString("0007 worker café " + string.Join(" ", Enumerable.Repeat("tail", 61)));
        state.SetCurrentRecord(source);
        eager.SetCurrentRecord(source);
        Assert.Equal(64, eager.FieldCount);
        AwkExpression Field(int index) => dynamicIndex
            ? AwkExpression.FieldIndex(AwkExpression.Number(index.ToString(System.Globalization.CultureInfo.InvariantCulture), index))
            : AwkExpression.FieldIndex(index);
        var first = await Field(1).EvaluateAsync(new(state));
        var firstEager = await Field(1).EvaluateAsync(new(eager));
        Assert.Equal(firstEager, first);
        Assert.Equal("0007"u8.ToArray(), first.ByteText.AsSpan().ToArray());
        Assert.Equal(1, counters.FieldsMaterialized);
        Assert.Equal(64, eagerCounters.FieldsMaterialized);
        var third = await Field(3).EvaluateAsync(new(state));
        var thirdEager = await Field(3).EvaluateAsync(new(eager));
        Assert.Equal(thirdEager, third);
        Assert.Equal(3, counters.FieldsMaterialized);
        Assert.Equal(64, state.FieldCount);
        Assert.Equal(64, counters.FieldsMaterialized);
        Assert.Equal(first, await Field(1).EvaluateAsync(new(state)));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedExpressionRetainsTightFieldQuotaBeforePublishing(bool dynamicIndex)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxFields(2).WithMaxExecutionMemoryBytes(1 << 20), AwkRegexEngineKind.DotNetCompatibility);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var source = AwkText.FromString("one two " + new string('x', 160));
        state.SetCurrentRecord(source);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var field = dynamicIndex ? AwkExpression.FieldIndex(AwkExpression.Number("1", 1)) : AwkExpression.FieldIndex(1);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await field.EvaluateAsync(new(state)));
        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Same(source, state.CurrentRecordByteText);
        state.SetCurrentRecord(AwkText.FromString("ok next"));
        Assert.Equal("ok"u8.ToArray(), (await field.EvaluateAsync(new(state))).ByteText.AsSpan().ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectProjectionCopiesBeforeBorrowedRecordMutationAndAvoidsFieldValues(bool finite)
    {
        var (state, counters) = State(finite);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var bytes = "first second"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        // Warm the ordinary ORS binding before comparing field-specific counters.
        _ = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        var allocations = counters.AwkTextAllocations;
        var values = counters.ValuesCreated;
        state.AppendFieldPrint(output, 1);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        // Reading ORS creates one value; the projected field creates none.
        Assert.Equal(values + 1, counters.ValuesCreated);
        Array.Fill(bytes, (byte)'X');
        state.SetCurrentRecord(AwkText.FromString("next"));
        state.AppendFieldPrint(output, 1);
        Assert.Equal("first\nnext\n", output.ToString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedCombinedAppendDoesNotPublishTheFieldBeforeItsTerminator(bool finite)
    {
        var (state, counters) = State(finite, builderLimit: 8);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        output.Append(AwkText.FromString("keep"));
        state.SetCurrentRecord(AwkText.FromString("1234"));
        Assert.Throws<AwkRuntimeException>(() => state.AppendFieldPrint(output, 1));
        Assert.Equal("keep", output.ToString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.SetCurrentRecord(AwkText.FromString("ok"));
        state.AppendFieldPrint(output, 1);
        Assert.Equal("keepok\n", output.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachedNumericFieldsUsePrintFormattingAndUnicodeSlicesRemainRaw(bool finite)
    {
        var (state, counters) = State(finite);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        state.SetCurrentRecord(AwkText.FromString("café 😀"));
        state.AppendFieldPrint(output, 1);
        state.AppendFieldPrint(output, 2);
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFormat, AwkValue.FromText("%.2f"));
        state.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        state.AppendFieldPrint(output, 1);
        Assert.Equal("café\n😀\n1.25\n", output.ToString());
        Assert.True(state.GetFieldValue(1).IsNumberValue);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void CancellationDuringPairCopyLeavesPublishedLengthUnchanged()
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        using var builder = new AwkUtf8Builder(AwkRuntimeLimits.CreateDefault(), AwkMemoryAccounting.Disabled);
        builder.Append("keep"u8);
        Assert.Throws<OperationCanceledException>(() => builder.Append(new byte[16_384], AwkText.FromString("end"), () =>
        {
            if (++checks == 4) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal("keep", AwkUtf8Codec.Decode(builder.AsSpan()));
        Assert.Equal(4, builder.Length);
    }

    private static (AwkRuntimeState, AwkRuntimeCounters) State(bool finite, int builderLimit = int.MaxValue)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(builderLimit);
        if (finite) limits = limits.WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options), counters);
    }
}
