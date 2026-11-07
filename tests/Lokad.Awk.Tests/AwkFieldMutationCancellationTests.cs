namespace Lokad.Awk.Tests;

public sealed class AwkFieldMutationCancellationTests
{
    private const int ManyFields = 16_384;
    private static readonly AwkText Separator = AwkText.FromSourceLiteralString("|");

    [Theory]
    [InlineData("field", 1_537)]
    [InlineData("field", ManyFields)]
    [InlineData("count", 1_537)]
    [InlineData("count", ManyFields)]
    [InlineData("reference", 1_537)]
    [InlineData("reference", ManyFields)]
    public void CanceledGrowthPreservesFieldsCountValueRecordAndMemory(string operation, int target)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var executionToken = cancellation.Token;
        var armed = false;
        var checks = 0;
        using var context = Frame(counters, governor, () =>
        {
            if (armed && ++checks == 3) cancellation.Cancel();
            executionToken.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        Initialize(frame, 2);
        frame.AssignFieldCount(new(2), AwkValue.FromText("02.8"), Separator);
        var previousRecord = frame.RecordAwkText;
        var first = frame.GetFieldValue(new(1));
        var committed = governor.CurrentCommittedBytes;
        var materialized = counters.FieldsMaterialized;
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, target));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(materialized, counters.FieldsMaterialized);
        Assert.Equal(2, frame.GetFieldCount());
        Assert.Equal("02.8", frame.GetFieldCountValue().AsString());
        Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
        Assert.Equal(first, frame.GetFieldValue(new(1)));

        // The frame's callback is operation-local; retry uses a fresh, uncanceled token.
        armed = false;
        using var retryCancellation = new CancellationTokenSource();
        executionToken = retryCancellation.Token;
        Grow(frame, operation, target);
        Assert.Equal(target, frame.GetFieldCount());
        Assert.Equal(operation == "count" ? "0" + target + ".8" : target.ToString(), frame.GetFieldCountValue().AsString());
        Assert.Equal(operation == "field" ? "tail" : "", frame.GetFieldValue(new(target)).AsString());
        Assert.Equal(operation == "reference" ? "a|a" : "a|a" + new string('|', target - 2) + (operation == "field" ? "tail" : ""), frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("analysis", false)]
    [InlineData("analysis", true)]
    [InlineData("fields", false)]
    [InlineData("fields", true)]
    [InlineData("cache", true)]
    public void CanceledTruncationPreservesAllFieldsAndCachedValues(string phase, bool cached)
    {
        const int retained = 8_193;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        long committed = 0;
        using var context = Frame(counters, governor, () =>
        {
            if (!armed) return;
            var inStage = governor.CurrentCommittedBytes > committed;
            if (phase == "analysis" || inStage)
            {
                var target = phase == "analysis" || phase == "fields" ? 3 : 14;
                if (++checks == target) cancellation.Cancel();
            }
            cancellation.Token.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        Initialize(frame, ManyFields);
        if (cached)
            for (var index = 1; index <= ManyFields; index++)
                _ = frame.GetFieldValue(new(index));
        committed = governor.CurrentCommittedBytes;
        var previousRecord = frame.RecordAwkText;
        var allocations = counters.AwkTextAllocations;
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => frame.AssignFieldCount(new(retained), Separator));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(ManyFields, frame.GetFieldCount());
        Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        if (cached)
            Assert.Equal("a", frame.GetFieldValue(new(ManyFields)).AsString());

        armed = false;
        frame.AssignFieldCount(new(retained), Separator);
        Assert.Equal(retained, frame.GetFieldCount());
        if (cached)
        {
            Assert.Equal("a", frame.GetFieldValue(new(retained)).AsString());
            Assert.Equal(allocations, counters.AwkTextAllocations);
        }
        Assert.Equal(string.Join("|", Enumerable.Repeat("a", retained)), frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void BoundedMutationChecksBeforeChangingLiveState(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        using var context = Frame(counters, governor, cancellation.Token.ThrowIfCancellationRequested);
        var frame = context.Frame;
        Initialize(frame, 2);
        var previousRecord = frame.RecordAwkText;
        var committed = governor.CurrentCommittedBytes;
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, 33));

        Assert.Equal(2, frame.GetFieldCount());
        Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void StagingPeakMustBeAdmittedBeforeGrowth(string operation)
    {
        const int original = 8_193;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var context = Frame(counters, governor, () => { });
        var frame = context.Frame;
        Initialize(frame, original);
        // Admit the actual splitter capacity first. The final size fits; its
        // overlapping staged replacement does not fit the remaining allowance.
        memoryForGovernor.Budget.SetAvailableBytes(1_024);
        var committed = governor.CurrentCommittedBytes;
        var previousRecord = frame.RecordAwkText;

        var error = Assert.Throws<AwkRuntimeException>(() => Grow(frame, operation, ManyFields, AwkText.Empty));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(original, frame.GetFieldCount());
        Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void SuccessfulGrowthTransfersTheStagingChargeExactlyOnce(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var context = Frame(counters, governor, () => { });
        var frame = context.Frame;
        Initialize(frame, 2);
        var committed = governor.CurrentCommittedBytes;

        Grow(frame, operation, ManyFields, AwkText.Empty, preserveNumericCount: true);

        var expected = committed - AwkRuntimeMemory.EstimateFieldTextListBytes(2) + AwkRuntimeMemory.EstimateFieldTextListBytes(ManyFields);
        if (operation == "field")
        {
            expected += AwkRuntimeMemory.EstimateTextBytes(AwkText.FromSourceLiteralString("tail"));
            expected += AwkRuntimeMemory.EstimateFieldValueListBytes(4);
            Assert.Equal(expected, governor.CurrentCommittedBytes);
        }
        else
            Assert.Equal(expected, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(ManyFields, frame.GetFieldCount());
        Assert.Equal(operation == "reference" ? "aa" : operation == "field" ? "aatail" : "aa", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private static void Grow(AwkFieldFrame frame, string operation, int target, AwkText? separator = null, bool preserveNumericCount = false)
    {
        switch (operation)
        {
            case "field": frame.AssignFieldValue(new(target), AwkValue.FromDeferredText(AwkText.FromSourceLiteralString("tail"), AwkNumberParsingMode.Decimal), separator ?? Separator); break;
            case "count": frame.AssignFieldCount(new(target), preserveNumericCount ? AwkValue.FromNumber(target) : AwkValue.FromText("0" + target + ".8"), separator ?? Separator); break;
            case "reference": frame.ExtendFieldCountForReference(new(target)); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void Initialize(AwkFieldFrame frame, int count)
    {
        frame.SetRecordText(AwkText.FromSourceLiteralString(new string('a', count)));
        Assert.Equal(count, frame.GetFieldCount());
    }


    private static AwkFieldTestContext Frame(AwkRuntimeCounters counters, AwkMemoryAccounting governor, Action checkpoint) =>
        new(counters, governor, value => value.ByteText, checkpoint, separator: "");

    private static CooperativeTestMemory Governor(AwkRuntimeCounters counters, long budget = 8 << 20) =>
        new CooperativeTestMemory(budget, counters);

}
