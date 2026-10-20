namespace Lokad.Awk.Tests;

public sealed partial class AwkFieldProjectionTests
{
    [Fact]
    public async Task FieldListPrintPreservesOutput()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        Assert.Equal(await RunPrintAsync(true, limits), await RunPrintAsync(false, limits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldListPrintCopiesBeforeTheRecordBorrowCanChange(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var bytes = "before 007 café"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        var io = new AwkIo(new MockFileSystem(), AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, counters, state.Limits, state, static () => []);
        var allocations = counters.AwkTextAllocations;
        await Statement().ExecuteAsync(new(state, output, io));
        Array.Fill(bytes, (byte)'X');
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("next 8 😀"));
        await Statement().ExecuteAsync(new(state, output, io));
        Assert.Equal("before café 007\nnext 😀 8\n", output.ToString());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(2, counters.FieldListPrintRuns);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FieldListCopyCancellationDoesNotPublishAPrefix(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        var counters = state.Counters;
        using var builder = new AwkUtf8Builder(state.MemoryAccounting);
        builder.Append("keep"u8);
        var record = AwkText.FromSourceLiteralString(new string('x', 32_768));
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() => builder.Append([
            AwkFieldText.FromRecordSlice(0, record.ByteLength), AwkFieldText.FromOwned(AwkText.FromSourceLiteralString("!"))], record, () =>
        {
            if (++checks == 6) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal("keep", AwkUtf8Codec.Decode(builder.AsSpan()));
        Assert.Equal(4, builder.Length);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkStatement Statement() => AwkStatement.Print([Field(1), Field(3), Field(2)]);
    private static AwkExpression Field(int index) => AwkExpression.FieldIndex(
        AwkExpression.Number(index));

    private static async Task<PrintResult> RunPrintAsync(bool owned, AwkRuntimeLimits limits)
    {
        var operands = owned ? "$i,$k,$j" : "$1,$3,$2";
        var invocation = AwkTestHarness.BuildInvocation("awk", "-v", "i=1", "-v", "j=2", "-v", "k=3", "{print " + operands + "}");
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), invocation, CancellationToken.None);
        var host = new MockFileSystem();
        host.SetStandardInput("head 007 café\nother -3 tail\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(2 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, memoryBudget: budget);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
        return new(exit.Value, host.GetOutput(AwkFileDescriptor.StdOut), host.GetOutput(AwkFileDescriptor.StdErr), counters.HostCalls, counters.FieldsMaterialized, counters.FieldSplits);
    }

    private readonly record struct PrintResult(int Exit, string Output, string Error, long Calls, long Fields, long Splits);
}
