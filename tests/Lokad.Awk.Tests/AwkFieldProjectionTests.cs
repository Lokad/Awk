namespace Lokad.Awk.Tests;

public sealed class AwkFieldProjectionTests
{
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
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options), counters);
    }
}
