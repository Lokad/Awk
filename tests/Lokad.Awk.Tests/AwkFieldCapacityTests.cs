namespace Lokad.Awk.Tests;

public sealed class AwkFieldCapacityTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkText Space = AwkText.FromSourceLiteralString(" ");

    [Theory]
    [InlineData(65)] [InlineData(4097)]
    public async Task RetainedCapacitySurvivesShrinkUntilStorageIsReplaced(int count)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        using var context = Fields(accounting, AwkRuntimeCounters.Disabled, count);
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
        AssertRefunded(accounting, budget);
    }

    public static IEnumerable<object[]> GrowthCases()
    {
        foreach (var operation in new[] { "field", "count", "reference" })
            foreach (var target in new[] { 65, 128, 2048 })
                yield return [operation, target];
    }

    [Theory]
    [MemberData(nameof(GrowthCases))]
    public async Task EveryGrowthRouteFundsItsBackingStorage(string operation, int target)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Fields(accounting, counters, 64);
        var frame = context.Frame;
        var record = frame.RecordAwkText;
        Grow(frame, operation, target);
        Assert.Equal(target, frame.GetFieldCount());
        Assert.True(accounting.CurrentCommittedBytes >= AwkRuntimeMemory.EstimateTextBytes(record) +
            AwkRuntimeMemory.EstimateFieldTextListBytes(target));
        Assert.Equal(target, counters.FieldsMaterialized);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        if (operation == "reference") Assert.True(record.HasSameStorage(frame.RecordAwkText));
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    [Theory]
    [InlineData("field")] [InlineData("count")] [InlineData("reference")]
    public async Task RefusedGrowthPreservesTheRecordAndFundingWithoutRetry(string operation)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Fields(accounting, counters, 64);
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
        Assert.Equal(64, counters.FieldsMaterialized);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    [Theory]
    [InlineData(false, "field")] [InlineData(true, "field")]
    [InlineData(false, "count")] [InlineData(true, "count")]
    [InlineData(false, "reference")] [InlineData(true, "reference")]
    public async Task CanceledGrowthCannotPublishAPrefix(bool governed, string operation)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        using var context = Fields(accounting, counters, 64, () =>
        {
            if (armed && ++checks == 3) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        var record = frame.RecordAwkText;
        var committed = accounting.CurrentCommittedBytes;
        var reserved = budget.Reserved;
        armed = true;
        var error = Assert.ThrowsAny<OperationCanceledException>(() => Grow(frame, operation, 4096));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        Assert.Equal(64, frame.GetFieldCount());
        Assert.True(record.HasSameStorage(frame.RecordAwkText));
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(reserved, budget.Reserved);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    [Fact]
    public async Task RepeatedGrowthIsBoundedAndZeroNfReleasesStorageOnRecomposition()
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Fields(accounting, counters, 1);
        var frame = context.Frame;
        for (var count = 2; count <= 513; count++) frame.AssignFieldCount(new(count), Space);
        Assert.Equal(513, frame.GetFieldCount());
        Assert.Equal(513, counters.FieldsMaterialized);
        Assert.InRange(accounting.CurrentCommittedBytes, 1, 64 << 10);
        frame.AssignFieldCount(new(0), Space);
        Assert.True(accounting.CurrentCommittedBytes > 0);
        Assert.True(AwkText.Empty.HasSameStorage(frame.RecordAwkText));
        AssertRefunded(accounting, budget);
        frame.ResetRecord();
    }

    private static AwkFieldTestContext Fields(AwkMemoryAccounting accounting, AwkRuntimeCounters counters,
        int count, Action? checkpoint = null)
    {
        var context = new AwkFieldTestContext(counters, accounting, checkpoint: checkpoint);
        context.Frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", count))));
        Assert.Equal(count, context.Frame.GetFieldCount());
        return context;
    }

    private static void Grow(AwkFieldFrame frame, string operation, int target)
    {
        if (operation == "field") frame.AssignFieldValue(new(target), AwkValue.FromNumber(0), Space);
        else if (operation == "count") frame.AssignFieldCount(new(target), Space);
        else frame.ExtendFieldCountForReference(new(target));
    }

    private static void AssertRefunded(AwkMemoryAccounting accounting, CooperativeTestBudget budget)
    {
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
    }
}
