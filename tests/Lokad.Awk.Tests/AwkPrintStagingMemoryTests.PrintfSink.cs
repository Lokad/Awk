namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
    [Fact]
    public async Task LatePrintfFailureClearsUnpublishedSinkBytes()
    {
        using var memoryForState = State(observed: true, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var host = new MockFileSystem();
        var io = Io(state, host);
        var statement = AwkStatement.Printf(AwkExpression.Literal("%s:%d"), [AwkExpression.Literal("payload")]);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => statement.ExecuteAsync(new(state, output, io)).AsTask());

        Assert.Equal("awk: not enough arguments to satisfy format string\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("", output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.PrintfSinkFormats);
        await output.FlushAsync(io);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        await io.CloseAsync();
    }

    [Fact]
    public void PrintfSinkUsesAdmittedCapacityAndClearsPartialGrowthRefusal()
    {
        using var memoryForState = State(observed: true, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        using var empty = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("kept")), state.Counters);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        Assert.True(output.TryFormatPrintf("%s", arguments, state));
        Assert.Equal("kept", output.ToString());
        Assert.False(output.TryFormatPrintf("%s", arguments, state));
        Assert.Equal("kept", output.ToString());
        // The governed buffer is already admitted; a failed growth must remove its prefix.
        var error = Assert.Throws<AwkRuntimeException>(() => empty.TryFormatPrintf("prefix:%10000s", arguments, state));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("", empty.ToString());
        Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void PartialPrintfCancellationClearsUnpublishedSinkBytes()
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(observed: true, "default", cancellation.Token);
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var arguments = new CancelingPrintfArguments(cancellation);
        var error = Assert.ThrowsAny<OperationCanceledException>(() => output.TryFormatPrintf("prefix:%s", arguments, state));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("", output.ToString());
        Assert.Equal(0, state.Counters.PrintfSinkFormats);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private sealed class CancelingPrintfArguments(CancellationTokenSource cancellation) : IPrintfArguments
    {
        public int Count => 1;
        public AwkText GetByteText(int index)
        {
            cancellation.Cancel();
            return AwkText.Empty;
        }
        public double GetNumber(int index) => throw new InvalidOperationException("Unexpected numeric conversion.");
        public bool IsNumeric(int index) => false;
    }
}
