namespace Lokad.Awk.Tests;

public sealed class AwkFieldCapacityTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkText Space = AwkText.FromSourceLiteralString(" ");

    [Theory]
    [InlineData(65)]
    [InlineData(4097)]
    public void RetainedCapacitySurvivesShrinkUntilStorageIsReplaced(int count)
    {
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            fieldMemory.Budget.SetAvailableBytes(Limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = Fields(fieldMemory, count);
            var frame = context.Frame;
            var before = accounting.CurrentCommittedBytes;
            frame.AssignFieldCount(new(1), Space);
            var shrunk = accounting.CurrentCommittedBytes;
            Assert.Equal(1, frame.GetFieldCount());
            if (count == 65) Assert.True(shrunk >= before);
            else Assert.True(shrunk < before);
            Assert.Equal("x", frame.RecordAwkText.AsString());
            Assert.True(accounting.CurrentCommittedBytes < shrunk);
            frame.ResetRecord();
            AssertRefunded(accounting, budget, context.BaselineBytes);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    public static IEnumerable<object[]> GrowthCases()
    {
        foreach (var operation in new[] { "field", "count", "reference" })
            foreach (var target in new[] { 65, 128, 2048 })
                yield return [operation, target];
    }

    [Theory]
    [MemberData(nameof(GrowthCases))]
    public void EveryGrowthRouteFundsItsBackingStorage(string operation, int target)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            fieldMemory.Budget.SetAvailableBytes(Limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = Fields(fieldMemory, 64);
            var frame = context.Frame;
            var record = frame.RecordAwkText;
            Grow(frame, operation, target);
            Assert.Equal(target, frame.GetFieldCount());
            Assert.True(accounting.CurrentCommittedBytes >= AwkRuntimeMemory.EstimateTextBytes(record) +
                AwkRuntimeMemory.EstimateFieldTextListBytes(target));
            Assert.Equal(target, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            if (operation == "reference") Assert.True(record.HasSameStorage(frame.RecordAwkText));
            frame.ResetRecord();
            AssertRefunded(accounting, budget, context.BaselineBytes);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void RefusedGrowthPreservesTheRecordAndFundingWithoutRetry(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            fieldMemory.Budget.SetAvailableBytes(Limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = Fields(fieldMemory, 64);
            var frame = context.Frame;
            var record = frame.RecordAwkText;
            var committed = accounting.CurrentCommittedBytes;
            var reserved = budget.Reserved;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = Assert.Throws<AwkRuntimeException>(() => Grow(frame, operation, 4096));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(64, frame.GetFieldCount());
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(reserved, budget.Reserved);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(64, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            frame.ResetRecord();
            AssertRefunded(accounting, budget, context.BaselineBytes);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, "field")]
    [InlineData(true, "field")]
    [InlineData(false, "count")]
    [InlineData(true, "count")]
    [InlineData(false, "reference")]
    [InlineData(true, "reference")]
    public void CanceledGrowthCannotPublishAPrefix(bool governed, string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            fieldMemory.Budget.SetAvailableBytes(Limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = Fields(fieldMemory, 64);
            var frame = context.Frame;
            var record = frame.RecordAwkText;
            var committed = accounting.CurrentCommittedBytes;
            var reserved = budget.Reserved;
            if (governed) fieldMemory.Budget.OnGrowth = _ => cancellation.Cancel();
            else cancellation.Cancel();
            var error = Assert.ThrowsAny<OperationCanceledException>(() => Grow(frame, operation, 4096));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(64, frame.GetFieldCount());
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(reserved, budget.Reserved);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            frame.ResetRecord();
            AssertRefunded(accounting, budget, context.BaselineBytes);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Fact]
    public void RepeatedGrowthIsBoundedAndZeroNfReleasesStorageOnRecomposition()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            fieldMemory.Budget.SetAvailableBytes(Limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = Fields(fieldMemory, 1);
            var frame = context.Frame;
            for (var count = 2; count <= 513; count++) frame.AssignFieldCount(new(count), Space);
            Assert.Equal(513, frame.GetFieldCount());
            Assert.Equal(513, counters[AwkCounter.FieldsPrepared]);
            Assert.InRange(accounting.CurrentCommittedBytes, 1, 64 << 10);
            frame.AssignFieldCount(new(0), Space);
            Assert.True(accounting.CurrentCommittedBytes > 0);
            Assert.True(AwkText.Empty.HasSameStorage(frame.RecordAwkText));
            AssertRefunded(accounting, budget, context.BaselineBytes);
            frame.ResetRecord();
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }
    private static AwkFieldTestContext Fields(CooperativeTestMemory memory, int count) { var context = new AwkFieldTestContext(memory); context.Frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", count)))); Assert.Equal(count, context.Frame.GetFieldCount()); return context; }

    private static void Grow(AwkFieldFrame frame, string operation, int target)
    {
        if (operation == "field") frame.AssignFieldValue(new(target), AwkValue.FromNumber(0), Space);
        else if (operation == "count") frame.AssignFieldCount(new(target), Space);
        else frame.ExtendFieldCountForReference(new(target));
    }

    private static void AssertRefunded(AwkMemoryAccounting accounting, CooperativeTestBudget budget, long baseline)
    {
        Assert.Equal(baseline, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(baseline, budget.Reserved);
    }
}
