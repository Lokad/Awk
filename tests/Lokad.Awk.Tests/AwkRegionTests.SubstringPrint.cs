namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData("print length($1),substr($2,1,2)", true)]
    [InlineData("print substr($2,1,2)", true)]
    [InlineData("print 1,(substr($2,1,2))", true)]
    [InlineData("print substr($2,1,2),length($1)", false)]
    [InlineData("print (kept=substr($2,1,2))", false)]
    [InlineData("print substr($2,1,2)\"x\"", false)]
    public void SubstringPrintDestinationRequiresItsFreshFinalConsumer(string body, bool expected)
    {
        using var memory = State(CancellationToken.None, false);
        var region = Compile(body, memory.State);
        var producers = region.Instructions.Where(instruction => instruction.Kind == AwkRegionInstructionKind.Substring && instruction.Right > 0).ToArray();
        Assert.Equal(expected, producers.Length > 0);
        foreach (var producer in producers)
        {
            var print = region.Prints[producer.Right - 1];
            Assert.True(print.HasPreparedText);
            Assert.InRange(print.Arguments[^1].Index, 0, region.Layout.PrintSegments - 1);
            Assert.True(region.Layout.Builders > 0);
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ColdSubstringDestinationRefusalOrCancellationPublishesNoFragment(bool cancel, bool whole)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(cancellation.Token, true);
        var state = memory.State;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Texts: 2, Builders: 1, PrintSegments: 1));
        var print = new AwkRegionPrint(0, 0, [new(AwkRegionPrintArgumentKind.PreparedText, 0)]);
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 4096)));
        using var source = state.MemoryAccounting.RetainValue(value);
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            if (cancel)
                Assert.ThrowsAny<OperationCanceledException>(() =>
                    storage.StoreSubstringPrint(print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(whole ? 4096 : 2048), 3));
            else
            {
                var failure = Assert.Throws<AwkRuntimeException>(() =>
                    storage.StoreSubstringPrint(print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(whole ? 4096 : 2048), 3));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            }
            Assert.Equal(0, storage.Builders[0].Buffer.Length);
            Assert.Equal(0, storage.PrintSegments[0].ByteLength());
            Assert.Equal(4096, value.ByteText.ByteLength);
        }
        finally { memory.Budget.OnGrowth = null; }
        source.Dispose();
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SubstringOutputRetainsItsActualBorrowAndPayloadUntilAppendCompletes(bool redirected, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var input = System.Text.Encoding.UTF8.GetBytes("café résumé\n");
        var retired = false;
        var closed = false;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var payload = "";
        var stdout = "";
        var writes = 0;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (++writes == 1)
                {
                    Assert.Equal(redirected ? new AwkFileDescriptor(42) : AwkFileDescriptor.StdOut, descriptor);
                    Assert.Equal("4 ré\n", AwkUtf8Codec.Decode(bytes.Span));
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.Equal("4 ré\n", AwkUtf8Codec.Decode(bytes.Span));
                    payload += AwkUtf8Codec.Decode(bytes.Span);
                }
                else
                {
                    Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                    stdout += AwkUtf8Codec.Decode(bytes.Span);
                }
                return AwkAppendResult.Accepted();
            },
            CloseDescriptorOverride = (descriptor, _) =>
            {
                Assert.Equal(new AwkFileDescriptor(42), descriptor);
                Assert.False(closed);
                closed = true;
                return Task.FromResult(AwkDescriptorCloseResult.Closed());
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(input, AwkReadCompletion.EndOfFile, _ =>
        {
            Assert.False(retired);
            retired = true;
            input.AsSpan().Fill(0xFF);
        }));
        if (redirected) host.EnqueueOpenAwkFileDescriptor(42);
        var script = "{print length($1),substr($2,1,2)" + (redirected ? ">\"out\"" : "") + ";print \"later\";exit}";
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.False(retired);
            Assert.False(closed);
            Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.False(retired);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(0, (await run).Value);
            Assert.Equal("4 ré\n", payload);
            Assert.Equal(cancel ? "" : "later\n", stdout);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.True(retired);
        Assert.Equal(redirected, closed);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public void RetiredPrintFragmentsKeepTheirReusableCapacityFundedUntilStorageDisposal()
    {
        using var memory = State(CancellationToken.None, true);
        var state = memory.State;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Texts: 2, Builders: 1, PrintSegments: 1));
        var print = new AwkRegionPrint(0, 0, [new(AwkRegionPrintArgumentKind.PreparedText, 0)]);
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 8000)));
        using var source = state.MemoryAccounting.RetainValue(value);
        var before = memory.Budget.Reserved;
        storage.StoreSubstringPrint(print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(3000), 3);
        var held = memory.Budget.Reserved;
        Assert.True(held >= before + 3000);
        Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(storage.Builders[0].Buffer.AsMemory(), out var first));
        storage.ClearValues();
        Assert.Equal(held, memory.Budget.Reserved);
        storage.StoreSubstringPrint(print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(1), 3);
        Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(storage.Builders[0].Buffer.AsMemory(), out var next));
        Assert.Same(first.Array, next.Array);
        Assert.Equal(held, memory.Budget.Reserved);
        Assert.Equal(1, storage.PrintSegments[0].ByteLength());
        source.Dispose();
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
