namespace Lokad.Awk.Tests;

public sealed partial class AwkFieldProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedExpressionsUsePrefixBoundsAndChargeCompletionWork(bool finite)
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
        AwkExpression Field(int index) => AwkExpression.FieldIndex(AwkExpression.Number(index));
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
    public void DirectProjectionCopiesBeforeBorrowedRecordMutationAndAvoidsFieldValues(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
    [Fact]
    public void FailedCombinedAppendDoesNotPublishTheFieldBeforeItsTerminator()
    {
        using var memoryForState = State(true);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var prefix = new string('k', AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity - 4);
        output.Append(AwkText.FromString(prefix));
        state.SetCurrentRecord(AwkText.FromString("1234"));
        _ = state.FieldCount;
        _ = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => state.AppendFieldPrint(output, 1));
        Assert.Equal(prefix, output.ToString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.SetCurrentRecord(AwkText.FromString("ok"));
        state.AppendFieldPrint(output, 1);
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
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
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
        using var builder = new AwkUtf8Builder(AwkMemoryAccounting.Disabled);
        builder.Append("keep"u8);
        Assert.Throws<OperationCanceledException>(() => builder.Append(new byte[16_384], AwkText.FromString("end"), () =>
        {
            if (++checks == 4) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal("keep", AwkUtf8Codec.Decode(builder.AsSpan()));
        Assert.Equal(4, builder.Length);
    }

    private static CooperativeTestMemory State(bool finite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: finite);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options)));
    }
}
