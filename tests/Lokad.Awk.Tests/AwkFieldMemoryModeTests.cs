namespace Lokad.Awk.Tests;

public sealed class AwkFieldMemoryModeTests
{
    private static readonly AwkText Separator = AwkText.FromSourceLiteralString("|");

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
            var fieldMemoryBaseline = fieldMemory.Accounting.CurrentCommittedBytes;
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
                Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
                Assert.Equal(0, accounting.CurrentReservedBytes);
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
            var fieldMemoryBaseline = fieldMemory.Accounting.CurrentCommittedBytes;
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
            var committed = accounting.CurrentCommittedBytes;
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
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            frame.AssignFieldValue(new(1), AwkValue.FromText("z"), Separator);
            Assert.Equal("z|beta", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
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
            var fieldMemoryBaseline = fieldMemory.Accounting.CurrentCommittedBytes;
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
            var committed = accounting.CurrentCommittedBytes;
            if (governed) fieldMemory.Budget.OnGrowth = _ => cancellation.Cancel();
            else cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: " ");
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));

            var value = frame.GetFieldValue(new(1));
            Assert.Equal("café", value.AsString());

            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

}
