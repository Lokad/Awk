namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed partial class AwkFunctionStorageTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReaderTextMaterializationFailureLeavesBorrowedCellUnchanged(bool cancel)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20, counters);
        using var cancellation = new CancellationTokenSource();
        var stack = new AwkFunctionCallStack(1_024, memory.Accounting, AwkNumberParsingMode.Decimal,
            checkpoint: cancellation.Token.ThrowIfCancellationRequested, counters: counters);
        var frame = stack.PrepareFrame(new([AwkVariableName.FromText("s")]));
        using var borrow = AwkReadBuffer.FromContent(new byte[3000], AwkReadCompletion.EndOfFile, static _ => { });
        stack.StageReaderText(frame, 0, borrow, 0, 3000);
        var before = memory.Budget.Reserved;
        var allocations = counters.AwkTextAllocations;
        if (cancel)
        {
            memory.Budget.OnGrowth = _ => cancellation.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(() => stack.ReadFrameValue(frame, new(0)));
        }
        else
        {
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            Assert.Throws<AwkRuntimeException>(() => stack.ReadFrameValue(frame, new(0)));
            Assert.Equal(1, memory.Budget.Refusals);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        }
        Assert.Equal(AwkFunctionLocalSlotKind.ReaderText, frame.GetSlot(0).Kind);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(before, memory.Budget.Reserved);
        Assert.Equal(0, frame.RetainedMemoryBytes);
        memory.Budget.OnGrowth = null;
        stack.DiscardIncomingFrame(frame);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReaderTextMaterializesOnceWithinItsFundedFrame(bool governed)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var stack = Stack(memory);
        var bytes = "0007é"u8.ToArray();
        using var borrow = AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, _ => bytes.AsSpan().Fill(0xFF));
        var frame = stack.PrepareFrame(new([AwkVariableName.FromText("s")]));
        stack.StageReaderText(frame, 0, borrow, 0, bytes.Length);
        var before = memory.Budget.Reserved;
        var value = stack.ReadFrameValue(frame, new(0));
        var funded = memory.Budget.Reserved;
        if (governed) Assert.True(funded > before);
        memory.Budget.OnGrowth = _ => throw new InvalidOperationException("Cached owning text cannot admit again.");
        Assert.Equal(AwkFunctionLocalSlotKind.Scalar, frame.GetSlot(0).Kind);
        Assert.Equal(funded, memory.Budget.Reserved);
        borrow.Dispose();
        Assert.Equal("0007é", value.ByteText.AsString());
        Assert.Equal(AwkRuntimeValueKind.String, value.RuntimeKind);
        Assert.Equal(7, value.AsNumber());
        Assert.True(value.ByteText.HasSameStorage(stack.ReadFrameValue(frame, new(0)).ByteText));
        memory.Budget.OnGrowth = null;
        stack.DiscardIncomingFrame(frame);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void ReaderForwardingUsesFundedCellsWithoutPrepayingForCopies()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20, counters);
        var stack = new AwkFunctionCallStack(1_024, memory.Accounting, AwkNumberParsingMode.Decimal,
            counters: counters);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("s")]);
        var first = stack.PrepareFrame(layout);
        var second = stack.PrepareFrame(layout);
        using var borrow = AwkReadBuffer.FromContent("text"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { });
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            stack.StageReaderText(first, 0, borrow, 0, 4);
            stack.StageArgument(second, 0, in first.GetSlot(0));
            Assert.True(second.GetSlot(0).TryGetReaderText(out var forwarded));
            Assert.True(forwarded.Span.SequenceEqual("text"u8));
            Assert.Equal(0, first.RetainedMemoryBytes);
            Assert.Equal(0, second.RetainedMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
            Assert.Equal(0, counters.AwkTextAllocations);
        }
        stack.DiscardIncomingFrame(second);
        stack.DiscardIncomingFrame(first);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReaderMaterializationPreservesItsSnapshotAfterAdmissionChangesTheCell(bool recycle)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("s")]);
        var frame = stack.PrepareFrame(layout);
        using var borrow = AwkReadBuffer.FromContent("0007é"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { });
        stack.StageReaderText(frame, 0, borrow, 0, borrow.Content.Length);
        var before = memory.Budget.Reserved;
        memory.Budget.OnGrowth = _ =>
        {
            memory.Budget.OnGrowth = null;
            if (recycle)
            {
                stack.DiscardIncomingFrame(frame);
                Assert.Same(frame, stack.PrepareFrame(layout));
            }
            frame.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(9));
        };
        var value = stack.ReadFrameValue(frame, new(0));
        Assert.Equal("0007é", value.ByteText.AsString());
        Assert.Equal(AwkRuntimeValueKind.String, value.RuntimeKind);
        Assert.Equal(9, stack.ReadFrameValue(frame, new(0)).AsNumber());
        Assert.Equal(before, memory.Budget.Reserved);
        Assert.Equal(0, frame.RetainedMemoryBytes);
        stack.DiscardIncomingFrame(frame);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ReaderArgumentSlicesKeepTheirSpellingAndAdjacentCells(bool governed, bool empty)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var stack = Stack(memory);
        var bytes = "prefix0007é suffix"u8.ToArray();
        using var borrow = AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, _ => bytes.AsSpan().Fill(0xFF));
        var frame = stack.PrepareFrame(new([AwkVariableName.FromText("n"), AwkVariableName.FromText("s")]));
        frame.StageNumber(0, 9);
        stack.StageReaderText(frame, 1, borrow, 6, empty ? 0 : 6);
        var value = stack.ReadFrameValue(frame, new(1));
        borrow.Dispose();
        Assert.Equal(empty ? "" : "0007é", value.ByteText.AsString());
        Assert.Equal(AwkRuntimeValueKind.String, value.RuntimeKind);
        Assert.Equal(9, stack.ReadFrameValue(frame, new(0)).AsNumber());
        stack.DiscardIncomingFrame(frame);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GeneralCallPreparationObservesCancellationBeforeTheArgumentTail(bool preCanceled)
    {
        const int count = 256;
        using var cancellation = new CancellationTokenSource();
        var visited = 0;
        var name = AwkFunctionName.FromText("score");
        var layout = new AwkFunctionLocalLayout(Enumerable.Range(0, count)
            .Select(index => AwkVariableName.FromText("p" + index)).ToArray());
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>
        {
            [name] = new(name, layout)
        };
        var parsed = AwkParser.Parse("BEGIN{value=score(" + string.Join(',', Enumerable.Repeat("1", count)) + ")}");
        Assert.True(parsed.Success);
        var scope = AwkLoweringScope.GlobalFor(AwkCompatibilityMode.GnuDefault, checkpoint: () =>
        {
            if (++visited == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }, functions: functions);
        if (preCanceled) cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), scope, -1, out _));
        Assert.True(visited < count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LargeCallRefusalPrecedesStorageAllocationAndArgumentEffects(bool suppliedArguments)
    {
        const int count = 10_000;
        var parameters = string.Join(',', Enumerable.Range(0, count).Select(i => "p" + i));
        var arguments = suppliedArguments ? "argument()," + string.Join(',', Enumerable.Repeat("0", count - 1)) : "";
        var source = "function argument(){print \"argument\";return 1} function f(" + parameters +
            "){print \"body\"} BEGIN{print \"before\";f(" + arguments + ");print \"after\"} END{print \"end\"}";
        var budget = new CooperativeTestBudget(128L << 20);
        var host = new MockFileSystem();
        long before = 0, allocated = -1;
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n" || budget.Refusals != 0) return;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            before = GC.GetAllocatedBytesForCurrentThread();
        };
        budget.OnRefusal = () => allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []);
        var tool = AwkTestHarness.ParseAwk(command);

        Assert.Equal(2, (await tool.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(allocated, 0, 64 * 1024);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public void CachedFramesStayFundedAndReuseTheirCapacity()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        Cycle();
        var retained = memory.Accounting.CurrentCommittedBytes;
        Assert.True(retained > 0);
        Cycle();
        Assert.Equal(retained, memory.Accounting.CurrentCommittedBytes);
        Assert.False(stack.IsInsideFunction);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Cycle()
        {
            for (var i = 0; i < 64; i++) stack.PushFrame(AwkFunctionLocalLayout.Empty, []);
            for (var i = 0; i < 64; i++) stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void AliasIndexCapacityStaysFundedAndReusableAfterUnwind()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        stack.PushFrame(AwkFunctionLocalLayout.Empty, []);
        stack.PopFrame(static _ => { });
        var frameCache = memory.Accounting.CurrentCommittedBytes;
        var names = Enumerable.Range(0, 512).Select(i => AwkVariableName.FromText("p" + i)).ToArray();
        var layout = new AwkFunctionLocalLayout(names);
        var arguments = Enumerable.Range(0, names.Length)
            .Select(i => AwkFunctionLocalSlot.PotentialArrayAlias(AwkArrayName.FromText("a" + i))).ToArray();
        stack.PushFrame(layout, arguments);
        Assert.True(stack.TryResolveOrPromoteArrayName(names[0], out var alias));
        Assert.Equal(AwkArrayName.FromText("a0"), alias);
        stack.PopFrame(static _ => { });
        var retained = memory.Accounting.CurrentCommittedBytes;
        Assert.True(retained > frameCache);
        stack.PushFrame(layout, arguments);
        stack.PopFrame(static _ => { });
        Assert.Equal(retained, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CancellationDuringFrameSetupLeavesTheCallerAndRefundsUnpublishedSlots()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var enabled = false;
        long callerBytes = 0;
        var stack = Stack(memory, () =>
        {
            if (enabled && memory.Budget.Reserved > callerBytes) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        });
        var name = AwkVariableName.FromText("x");
        stack.PushFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);
        callerBytes = memory.Budget.Reserved;
        enabled = true;
        Assert.ThrowsAny<OperationCanceledException>(() => stack.PushFrame(new([name]), []));
        Assert.True(stack.TryResolveLocalValue(name, out var caller));
        Assert.Equal(42, caller.Value.AsNumber());
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        stack.PopFrame(static _ => { }); // Cleanup must not poll the cancelled token.
        Assert.False(stack.IsInsideFunction);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static AwkFunctionCallStack Stack(CooperativeTestMemory memory, Action? checkpoint = null) =>
        new(1_024, memory.Accounting, AwkNumberParsingMode.Decimal, checkpoint: checkpoint);
}
