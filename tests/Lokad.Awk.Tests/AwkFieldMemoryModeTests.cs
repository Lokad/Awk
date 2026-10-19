namespace Lokad.Awk.Tests;

public sealed class AwkFieldMemoryModeTests
{
    private static readonly AwkText Separator = AwkText.FromSourceLiteralString("|");

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReadThenMutateGrowShrinkAndResetPreservePayloadAndRefunds(bool governed, bool staged)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        Assert.Equal(governed, accounting.IsEnabled);
        var frame = Frame(accounting, counters);
        var target = staged ? 1537 : 4;
        for (var iteration = 0; iteration < 2; iteration++)
        {
            frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
            Assert.Equal("café", frame.GetFieldValue(new(1), Split, AwkValue.FromText).AsString());
            Assert.Equal(2, frame.GetFieldCount(AlreadySplit));
            frame.AssignFieldValue(new(1), AwkValue.FromNumber(1.25), Separator, AlreadySplit);
            Assert.Equal("1.25|beta", frame.RecordAwkText.AsString());
            frame.AssignFieldValue(new(target), AwkValue.FromText("tail"), Separator, AlreadySplit);
            Assert.Equal(target, frame.GetFieldCount(AlreadySplit));
            Assert.Equal("1.25|beta" + new string('|', target - 2) + "tail", frame.RecordAwkText.AsString());
            frame.AssignFieldCount(new(1), AwkValue.FromText("01.8"), Separator, AlreadySplit);
            Assert.Equal("01.8", frame.GetFieldCountValue(AlreadySplit).AsString());
            Assert.Equal("1.25", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(0, frame.GetFieldCount(AlreadySplit));
            Assert.Equal("", frame.RecordAwkText.AsString());
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedRefusalAfterReadPreservesFieldsAndRecord(bool count)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var frame = Frame(accounting, counters);
        frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
        Assert.Equal("café", frame.GetFieldValue(new(1), Split, AwkValue.FromText).AsString());
        var record = frame.RecordAwkText;
        var committed = accounting.CurrentCommittedBytes;
        var materialized = counters.FieldsMaterialized;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (count)
                frame.AssignFieldCount(new(32), Separator, AlreadySplit);
            else
                frame.AssignFieldValue(new(3), AwkValue.FromText(new string('x', 32)), Separator, AlreadySplit);
        });
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(2, frame.GetFieldCount(AlreadySplit));
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(materialized, counters.FieldsMaterialized);
        frame.AssignFieldValue(new(1), AwkValue.FromText("z"), Separator, AlreadySplit);
        Assert.Equal("z|beta", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledFieldCreationDoesNotPublishCacheAndCanRetry(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var frame = Frame(accounting, counters, () => token.ThrowIfCancellationRequested());
        frame.SetRecordText(AwkText.FromSourceLiteralString("café beta"));
        Assert.Equal(2, frame.GetFieldCount(Split));
        var committed = accounting.CurrentCommittedBytes;
        var creations = 0;
        var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1), AlreadySplit, text =>
        {
            creations++;
            cancellation.Cancel();
            return AwkValue.FromText(text);
        }));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        token = CancellationToken.None;
        var value = frame.GetFieldValue(new(1), AlreadySplit, text => { creations++; return AwkValue.FromText(text); });
        Assert.Equal("café", value.AsString());
        Assert.Equal(2, creations);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }


    private static AwkFieldFrame Frame(AwkMemoryAccounting accounting, AwkRuntimeCounters counters, Action? checkpoint = null) =>
        new(counters, accounting, static value => AwkText.FromString(value.AsString()), checkpoint);

    private static List<AwkFieldText> Split(AwkText _) =>
        [AwkFieldText.FromRecordSlice(0, 5), AwkFieldText.FromRecordSlice(6, 4)];

    private static List<AwkFieldText> AlreadySplit(AwkText _) =>
        throw new InvalidOperationException("Fields must already be split.");
}
