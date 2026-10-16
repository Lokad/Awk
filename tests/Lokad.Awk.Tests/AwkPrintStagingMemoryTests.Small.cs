namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
    [Theory]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    [InlineData(false, 4)] [InlineData(true, 4)]
    public async Task SmallOwnedPrintUsesAdmittedOutputWithoutStagingHeadroom(bool observed, int count)
    {
        var state = State(observed, "default");
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("prior:"));
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var io = Io(state, new MockFileSystem());
        await AwkStatement.Print(Enumerable.Range(0, count).Select(_ => AwkExpression.Literal("café")).ToArray())
            .ExecuteAsync(new(state, output, io));
        Assert.Equal("prior:" + string.Join(" ", Enumerable.Repeat("café", count)) + "\n", output.ToString());
        Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(observed ? 1 : 0, state.Counters.SmallValuePrintRuns);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SmallPrintGrowthRefusalKeepsEarlierOutput(bool observed)
    {
        var state = State(observed, "default");
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var earlier = new string('p', 60_000);
        output.Append(AwkText.FromSourceLiteralString(earlier));
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes - 131_071);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var io = Io(state, new MockFileSystem());
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => AwkStatement.Print([
            AwkExpression.Literal(new string('x', 10_000)), AwkExpression.Literal(new string('y', 10_000))])
            .ExecuteAsync(new(state, output, io)).AsTask());
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(earlier, output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)] [InlineData(3, true)]
    public async Task SmallPrintWaitsForEachOperandOnceBeforePublishing(int pendingIndex, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(true, "default", cancellation.Token);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var io = Io(state, new MockFileSystem(), cancellation.Token);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new SinglePendingOperand(completion.Task);
        var operands = Enumerable.Range(0, 4).Select(index => index == pendingIndex ? (AwkExpression)operand : AwkExpression.Literal("x")).ToArray();
        var pending = AwkStatement.Print(operands).ExecuteAsync(new(state, output, io)).AsTask();
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, operand.Evaluations);
            Assert.Equal("", output.ToString());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            if (cancel) cancellation.Cancel();
            completion.TrySetResult(AwkValue.FromText(AwkText.FromSourceLiteralString("é")));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            else await pending;
            Assert.Equal(cancel ? "" : string.Join(" ", Enumerable.Range(0, 4).Select(i => i == pendingIndex ? "é" : "x")) + "\n", output.ToString());
            Assert.Equal(cancel ? 0 : 1, state.Counters.SmallValuePrintRuns);
            Assert.Equal(1, operand.Evaluations);
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            completion.TrySetResult(AwkValue.FromNumber(1));
            await io.CloseAsync();
        }
    }
}
