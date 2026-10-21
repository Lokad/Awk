namespace Lokad.Awk.Tests;

public sealed class AwkFieldCapacityTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkText Space = AwkText.FromSourceLiteralString(" ");

    [Theory]
    [InlineData(65)]
    [InlineData(4097)]
    public async Task RetainedCapacitySurvivesShrinkUntilStorageIsReplaced(int count)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, record, fields) = Frame(accounting, counters, count);
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(fields.Capacity), accounting.CurrentCommittedBytes);
        frame.AssignFieldCount(new(1), Space, NoSplit);
        var retainedCapacity = count - 1 >= 1024 ? 1 : fields.Capacity;
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(retainedCapacity), accounting.CurrentCommittedBytes);
        var composed = frame.RecordAwkText;
        Assert.Equal("x"u8.ToArray(), composed.AsSpan().ToArray());
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(composed) + Buffer(1), accounting.CurrentCommittedBytes);
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
    public async Task EveryGrowthRouteChargesPlannedCapacity(string operation, int target)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, record, _) = Frame(accounting, counters, 64);
        Grow(frame, operation, target);
        var capacity = target >= 2048 ? target : 128;
        var cacheBytes = operation == "field" ? 320 : 0;
        Assert.Equal(target, frame.GetFieldCount(NoSplit));
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(capacity) + cacheBytes +
            (operation == "reference" ? 0 : Space.EstimatedRetainedMemoryBytes),
            accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(target, counters.FieldsMaterialized);
        if (operation == "reference") Assert.Same(record, frame.RecordAwkText);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    public static IEnumerable<object[]> MutationCases()
    {
        foreach (var operation in new[] { "field", "count", "reference" })
            yield return [operation];
    }

    [Theory]
    [MemberData(nameof(MutationCases))]
    public async Task RefusedCapacityGrowthPreservesTheRecordAndBackingStorage(string operation)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, record, fields) = Frame(accounting, counters, 64);
        // Other retained execution state leaves room for the old count delta, not the new buffer.
        var padding = Limit - accounting.CurrentCommittedBytes - 2048;
        accounting.ReserveAndCommit(padding);
        var committed = accounting.CurrentCommittedBytes;
        var reserved = budget.Reserved;
        try
        {
            var error = Assert.Throws<AwkRuntimeException>(() => Grow(frame, operation, 65));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(64, frame.GetFieldCount(NoSplit));
            Assert.Equal(64, fields.Count);
            Assert.Equal(64, fields.Capacity);
            Assert.Same(record, frame.RecordAwkText);
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(reserved, budget.Reserved);
            Assert.Equal(64, counters.FieldsMaterialized);
        }
        finally
        {
            frame.ResetRecord();
            accounting.ReleaseCommitted(padding);
        }
        AssertRefunded(accounting, budget);
    }

    [Theory]
    [InlineData(false, "field")] [InlineData(true, "field")]
    [InlineData(false, "count")] [InlineData(true, "count")]
    [InlineData(false, "reference")] [InlineData(true, "reference")]
    public async Task CanceledCapacityGrowthCannotPublishAPrefix(bool governed, string operation)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        var (frame, record, fields) = Frame(accounting, counters, 64, () =>
        {
            if (armed && ++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        });
        var committed = accounting.CurrentCommittedBytes;
        var reserved = budget.Reserved;
        armed = true;
        var error = Assert.ThrowsAny<OperationCanceledException>(() => Grow(frame, operation, 65));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        Assert.Equal(64, frame.GetFieldCount(NoSplit));
        Assert.Equal(64, fields.Count);
        Assert.Equal(64, fields.Capacity);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(reserved, budget.Reserved);
        Assert.Equal(64, counters.FieldsMaterialized);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    [Theory]
    [MemberData(nameof(MutationCases))]
    public async Task SpareCapacityIsKeptAndChargedWithoutReplacingTheList(string operation)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, record, fields) = Frame(accounting, counters, 65);
        Grow(frame, operation, 100);
        Assert.Equal(100, fields.Count);
        Assert.Equal(128, fields.Capacity);
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(128) +
            (operation == "field" ? 320 : 0) +
            (operation == "reference" ? 0 : Space.EstimatedRetainedMemoryBytes), accounting.CurrentCommittedBytes);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    [Fact]
    public async Task RepeatedGrowthAdmitsOnlyGeometricBuffersAndZeroNfKeepsCapacityUntilRecomposition()
    {
        var budget = new CooperativeTestBudget(Limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, record, _) = Frame(accounting, counters, 1);
        for (var count = 2; count <= 513; count++) frame.AssignFieldCount(new(count), Space, NoSplit);
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(1024) + Space.EstimatedRetainedMemoryBytes,
            accounting.CurrentCommittedBytes);
        Assert.Equal(513, counters.FieldsMaterialized);
        frame.AssignFieldCount(new(0), Space, NoSplit);
        Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(record) + Buffer(1024), accounting.CurrentCommittedBytes);
        Assert.Same(AwkText.Empty, frame.RecordAwkText);
        AssertRefunded(accounting, budget);
        frame.ResetRecord();
        AssertRefunded(accounting, budget);
    }

    private static long Buffer(int capacity) => AwkRuntimeMemory.EstimateFieldTextListBytes(capacity);


    private static (AwkFieldFrame Frame, AwkText Record, List<AwkFieldText> Fields) Frame(
        AwkMemoryAccounting accounting, AwkRuntimeCounters counters, int count, Action? checkpoint = null)
    {
        var record = AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", count)));
        var fields = new List<AwkFieldText>();
        for (var i = 0; i < count; i++) fields.Add(AwkFieldText.FromRecordSlice(i * 2, 1));
        var frame = new AwkFieldFrame(counters, accounting, value => AwkText.FromString(value.AsString()), checkpoint);
        frame.SetRecordText(record);
        Assert.Equal(count, frame.GetFieldCount(_ => fields));
        return (frame, record, fields);
    }

    private static List<AwkFieldText> NoSplit(AwkText _) => throw new InvalidOperationException("Unexpected resplit.");

    private static void Grow(AwkFieldFrame frame, string operation, int target)
    {
        if (operation == "field") frame.AssignFieldValue(new(target), AwkValue.FromNumber(0), Space, NoSplit);
        else if (operation == "count") frame.AssignFieldCount(new(target), Space, NoSplit);
        else frame.ExtendFieldCountForReference(new(target), NoSplit);
    }

    private static void AssertRefunded(AwkMemoryAccounting accounting, CooperativeTestBudget budget)
    {
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
    }
}
