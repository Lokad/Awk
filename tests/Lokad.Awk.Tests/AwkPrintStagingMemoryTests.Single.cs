namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
    [Theory]
    [InlineData(false, "field")] [InlineData(true, "field")]
    [InlineData(false, "terminator")] [InlineData(true, "terminator")]
    [InlineData(false, "recomposed-terminator")] [InlineData(true, "recomposed-terminator")]
    public async Task FieldPrintNumericFormattingRefusalKeepsEarlierOutputAndAllowsIndependentRetry(bool observed, string scenario)
    {
        using var memory = State(observed, "wide");
        var state = memory.State;
        state.AssignField(0, "head tail");
        var index = scenario == "recomposed-terminator" ? 0 : 1;
        if (scenario == "field") state.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        else
        {
            if (index == 0) state.AssignField(1, "kept");
            state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromNumber(1.25));
            state.AssignSpecialVariableValue(AwkSpecialVariableId.ConversionFormat,
                AwkValue.FromText(AwkText.FromSourceLiteralString("%8192.2f")));
        }
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var io = Io(state, new MockFileSystem());
        // Warm field preparation and output capacity so refusal observes the
        // numeric payload's funding rather than cold descriptors or output growth.
        state.AppendFieldPrint(output, index);
        var expected = output.ToString();
        await output.FlushAsync(io);
        output.Append(AwkText.FromSourceLiteralString("prior:"));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - 1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() => state.AppendFieldPrint(output, index));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("prior:", output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);

        pressure.Dispose();
        state.AppendFieldPrint(output, index);
        Assert.Equal("prior:" + expected, output.ToString());
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        await io.CloseAsync();
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleOwnedValueUsesAdmittedOutputWithoutStagingHeadroom(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("prior:"));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline);
        var io = Io(state, new MockFileSystem());
        AwkPrintRuntime.AppendSingle(output, AwkValue.FromText("kept"), state);
        Assert.Equal("prior:kept\n", output.ToString());
        Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(observed ? 1 : 0, state.Counters.SingleValuePrintRuns);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false, "default")]
    [InlineData(true, "default")]
    [InlineData(false, "wide")]
    [InlineData(true, "wide")]
    public async Task SingleNumericPrintReleasesFormattingAfterCopyingToOwnedOutput(bool observed, string format)
    {
        using var memoryForState = State(observed, format);
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var io = Io(state, new MockFileSystem());
        if (format == "wide")
        {
            output.Append(AwkText.FromSourceLiteralString(new string('p', 8193)));
            await output.FlushAsync(io);
        }
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        AwkPrintRuntime.AppendSingle(output, AwkValue.FromNumber(1.25), state);
        Assert.Equal((format == "wide" ? new string(' ', 8188) : "") + "1.25\n", output.ToString());
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SinglePrintFormattingRefusalKeepsPriorOutputAndReleasesTemporaryDemand(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("prior:"));
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes - 1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => AwkPrintRuntime.AppendSingle(output, AwkValue.FromNumber(1.25), state));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("prior:", output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

}
