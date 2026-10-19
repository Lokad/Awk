namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
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
        await AwkStatement.Print([AwkExpression.Literal("kept")]).ExecuteAsync(new(state, output, io));
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
        await AwkStatement.Print([AwkExpression.Number("1.25", 1.25)]).ExecuteAsync(new(state, output, io));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SinglePrintWaitsForItsOperandOnceAndDoesNotPublishAfterCancellation(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(true, "default", cancellation.Token);
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var io = Io(state, new MockFileSystem(), cancellation.Token);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new SinglePendingOperand(completion.Task);
        var pending = AwkStatement.Print([operand]).ExecuteAsync(new(state, output, io)).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, operand.Evaluations);
            Assert.Equal("", output.ToString());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            if (cancel) cancellation.Cancel();
            completion.TrySetResult(AwkValue.FromNumber(1.25));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            else await pending;
            Assert.Equal(cancel ? "" : "1.25\n", output.ToString());
            Assert.Equal(cancel ? 0 : 1, state.Counters.SingleValuePrintRuns);
            Assert.Equal(1, operand.Evaluations);
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            completion.TrySetResult(AwkValue.FromNumber(1.25));
            await io.CloseAsync();
        }
    }

    private sealed class SinglePendingOperand(Task<AwkValue> value) : AwkExpression
    {
        public int Evaluations { get; private set; }
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            Evaluations++;
            return new(value);
        }
    }
}
