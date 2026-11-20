namespace Lokad.Awk.Tests;

public sealed class AwkFieldMemoryModeTests
{
    private static readonly AwkText Separator = AwkText.FromSourceLiteralString("|");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterruptedBufferedFieldCopyPreservesRecordAndCache(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        using (memory)
        using (var context = new AwkFieldTestContext(memory))
        {
            AwkTestHarness.PublishBufferedRecord(context.State, "0007 tail"u8);
            var frame = context.Frame;
            Assert.Equal(2, frame.GetFieldCount());
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var live = memory.Accounting.LiveBytes;
            var reserved = memory.Budget.Reserved;
            var allocations = counters[AwkCounter.TextAllocations];

            if (cancel)
            {
                memory.Budget.OnGrowth = _ => cancellation.Cancel();
                var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1)));
                Assert.Equal(cancellation.Token, error.CancellationToken);
            }
            else
            {
                // The text fits on its own. There is insufficient space for its cache entry.
                memory.Budget.SetAvailableBytes(AwkRuntimeMemory.EstimateTextBytes(4));
                var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1)));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            }

            Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
            Assert.Equal(live, memory.Accounting.LiveBytes);
            Assert.Equal(reserved, memory.Budget.Reserved);
            Assert.True(frame.TryGetPrintableBufferedRecord(out var source));
            Assert.True(source.AsSpan().SequenceEqual("0007 tail"u8));
            if (!cancel)
            {
                memory.Budget.SetAvailableBytes(8 << 20);
                var value = frame.GetFieldValue(new(1));
                Assert.True(value.IsNumericStringValue);
                Assert.Equal("0007", value.AsString());
            }
        }
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReadThenMutateGrowShrinkAndResetPreservePayloadAndRefunds(bool governed, bool staged)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBaseline = fieldMemory.Accounting.LiveBytes;
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            Assert.Equal(governed, accounting.IsEnabled);
            using var context = new AwkFieldTestContext(fieldMemory);
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var target = staged ? 1537 : 4;
            for (var iteration = 0; iteration < 2; iteration++)
            {
                frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
                Assert.Equal("café", frame.GetFieldValue(new(1)).AsString());
                Assert.Equal(2, frame.GetFieldCount());
                frame.AssignFieldValue(new(1), AwkValue.FromNumber(1.25), Separator);
                Assert.Equal("1.25|beta", frame.RecordAwkText.AsString());
                frame.AssignFieldValue(new(target), AwkValue.FromText("tail"), Separator);
                Assert.Equal(target, frame.GetFieldCount());
                Assert.Equal("1.25|beta" + new string('|', target - 2) + "tail", frame.RecordAwkText.AsString());
                frame.AssignFieldCount(new(1), AwkValue.FromText("01.8"), Separator);
                Assert.Equal("01.8", frame.GetFieldCountValue().AsString());
                Assert.Equal("1.25", frame.RecordAwkText.AsString());
                frame.ResetRecord();
                Assert.Equal(0, frame.GetFieldCount());
                Assert.Equal("", frame.RecordAwkText.AsString());
                Assert.Equal(fieldMemoryBaseline, accounting.LiveBytes);
                Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
            }
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedRefusalAfterReadPreservesFieldsAndRecord(bool count)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBaseline = fieldMemory.Accounting.LiveBytes;
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory);
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
            Assert.Equal("café", frame.GetFieldValue(new(1)).AsString());
            Assert.Equal(2, frame.GetFieldCount());
            var record = frame.RecordAwkText;
            var committed = accounting.LiveBytes;
            var materialized = counters[AwkCounter.FieldsPrepared];
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = Assert.Throws<AwkRuntimeException>(() =>
            {
                if (count)
                    frame.AssignFieldCount(new(32), Separator);
                else
                    frame.AssignFieldValue(new(3), AwkValue.FromText(new string('x', 32)), Separator);
            });
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(2, frame.GetFieldCount());
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, accounting.LiveBytes);
            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            frame.AssignFieldValue(new(1), AwkValue.FromText("z"), Separator);
            Assert.Equal("z|beta", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.LiveBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledFieldCreationDoesNotPublishCacheAndCanRetry(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBaseline = fieldMemory.Accounting.LiveBytes;
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory);
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
            Assert.Equal(2, frame.GetFieldCount());
            var committed = accounting.LiveBytes;
            if (governed) fieldMemory.Budget.OnGrowth = _ => cancellation.Cancel();
            else cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(committed, accounting.LiveBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: " ");
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));

            var value = frame.GetFieldValue(new(1));
            Assert.Equal("café", value.AsString());

            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.LiveBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

}
