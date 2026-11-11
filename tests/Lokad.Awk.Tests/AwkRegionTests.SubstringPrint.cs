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
            Assert.DoesNotContain(region.Instructions, instruction =>
                instruction.Kind == AwkRegionInstructionKind.PrintList && instruction.Left == producer.Right - 1);
            if (print.Count == 1 && !print.Output.IsRedirected)
                Assert.Equal(0, region.Layout.Builders);
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ColdSubstringDestinationRefusalOrCancellationPublishesNoOutput(bool cancel, bool whole)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(cancellation.Token, true);
        var state = memory.State;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Texts: 2));
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var print = new AwkRegionPrint(0, 0, [new(AwkRegionPrintArgumentKind.PreparedText, 0)]);
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 4096)));
        using var source = state.MemoryAccounting.RetainValue(value);
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            if (cancel)
                Assert.ThrowsAny<OperationCanceledException>(() =>
                    storage.PrintSubstring(output, print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(whole ? 4096 : 2048), 3));
            else
            {
                var failure = Assert.Throws<AwkRuntimeException>(() =>
                    storage.PrintSubstring(output, print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(whole ? 4096 : 2048), 3));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            }
            Assert.False(output.HasPendingOutput);
            Assert.Equal(0, state.Counters.AwkTextAllocations);
            Assert.Equal(4096, value.ByteText.ByteLength);
        }
        finally { memory.Budget.OnGrowth = null; }
        source.Dispose();
        storage.Dispose();
        output.Dispose();
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
    public void SubstringOutputRemainsFundedAfterItsSourceAndRegionRetire()
    {
        using var memory = State(CancellationToken.None, true);
        var state = memory.State;
        var baseline = memory.Budget.Reserved;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Texts: 2));
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var print = new AwkRegionPrint(0, 0, [new(AwkRegionPrintArgumentKind.PreparedText, 0)]);
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 8000)));
        using var source = state.MemoryAccounting.RetainValue(value);
        storage.PrintSubstring(output, print, new(value), AwkValue.FromNumber(1), AwkValue.FromNumber(3000), 3);
        source.Dispose();
        storage.Dispose();
        Assert.True(output.HasPendingOutput);
        Assert.True(memory.Budget.Reserved >= baseline + 3000);
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
