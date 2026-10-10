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
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var executionToken = cancellation.Token;
        var armed = false;
        var checks = 0;
        var frame = Frame(counters, governor, () =>
        {
            if (armed && ++checks == 3) cancellation.Cancel();
            executionToken.ThrowIfCancellationRequested();
        });
        Initialize(frame, 2);
        frame.AssignFieldCount(new(2), AwkValue.FromText("02.8"), Separator, AlreadySplit);
        var previousRecord = frame.RecordAwkText;
        var first = frame.GetFieldValue(new(1), AlreadySplit, AwkValue.FromText);
        var committed = governor.CurrentCommittedBytes;
        var materialized = counters.FieldsMaterialized;
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, target));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.True(governor.PeakCommittedBytes > committed);
        Assert.Equal(materialized, counters.FieldsMaterialized);
        Assert.Equal(2, frame.GetFieldCount(AlreadySplit));
        Assert.Equal("02.8", frame.GetFieldCountValue(AlreadySplit).AsString());
        Assert.Same(previousRecord, frame.RecordAwkText);
        Assert.Equal(first, frame.GetFieldValue(new(1), AlreadySplit, _ => throw new InvalidOperationException("Cached.")));

        // The frame's callback is operation-local; retry uses a fresh, uncanceled token.
        armed = false;
        using var retryCancellation = new CancellationTokenSource();
        executionToken = retryCancellation.Token;
        Grow(frame, operation, target);
        Assert.Equal(target, frame.GetFieldCount(AlreadySplit));
        Assert.Equal(operation == "count" ? "0" + target + ".8" : target.ToString(), frame.GetFieldCountValue(AlreadySplit).AsString());
        Assert.Equal(operation == "field" ? "tail" : "", frame.GetFieldValue(new(target), AlreadySplit, AwkValue.FromText).AsString());
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
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        long committed = 0;
        var frame = Frame(counters, governor, () =>
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
        Initialize(frame, ManyFields);
        if (cached)
            for (var index = 1; index <= ManyFields; index++)
                _ = frame.GetFieldValue(new(index), AlreadySplit, AwkValue.FromText);
        committed = governor.CurrentCommittedBytes;
        var previousRecord = frame.RecordAwkText;
        var allocations = counters.AwkTextAllocations;
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => frame.AssignFieldCount(new(retained), Separator, AlreadySplit));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(ManyFields, frame.GetFieldCount(AlreadySplit));
        Assert.Same(previousRecord, frame.RecordAwkText);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        if (cached)
            Assert.Equal("a", frame.GetFieldValue(new(ManyFields), AlreadySplit, _ => throw new InvalidOperationException("Cached." )).AsString());

        armed = false;
        frame.AssignFieldCount(new(retained), Separator, AlreadySplit);
        Assert.Equal(retained, frame.GetFieldCount(AlreadySplit));
        if (cached)
        {
            Assert.Equal("a", frame.GetFieldValue(new(retained), AlreadySplit, _ => throw new InvalidOperationException("Retained cache." )).AsString());
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
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var frame = Frame(counters, governor, cancellation.Token.ThrowIfCancellationRequested);
        Initialize(frame, 2);
        var previousRecord = frame.RecordAwkText;
        var committed = governor.CurrentCommittedBytes;
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, 33));

        Assert.Equal(2, frame.GetFieldCount(AlreadySplit));
        Assert.Same(previousRecord, frame.RecordAwkText);
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
        var record = AwkText.FromSourceLiteralString(new string('a', original));
        var budget = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.EstimateFieldTextListBytes(ManyFields),
            2 * AwkRuntimeMemory.EstimateTextBytes(record) + 1_024);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters, budget);
        var frame = Frame(counters, governor, () => { });
        Initialize(frame, original);
        var committed = governor.CurrentCommittedBytes;
        var previousRecord = frame.RecordAwkText;

        var error = Assert.Throws<AwkRuntimeException>(() => Grow(frame, operation, ManyFields, AwkText.Empty));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(original, frame.GetFieldCount(AlreadySplit));
        Assert.Same(previousRecord, frame.RecordAwkText);
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
        var governor = Governor(counters);
        var frame = Frame(counters, governor, () => { });
        Initialize(frame, 2);
        var committed = governor.CurrentCommittedBytes;

        Grow(frame, operation, ManyFields, AwkText.Empty, preserveNumericCount: true);

        var expected = committed - AwkRuntimeMemory.EstimateFieldTextListBytes(2) + AwkRuntimeMemory.EstimateFieldTextListBytes(ManyFields);
        if (operation == "field")
            expected += AwkRuntimeMemory.EstimateTextBytes(AwkText.FromSourceLiteralString("tail")) + AwkRuntimeMemory.EstimateDictionaryBufferBytes(1);
        Assert.Equal(expected, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(ManyFields, frame.GetFieldCount(AlreadySplit));
        Assert.Equal(operation == "reference" ? "aa" : operation == "field" ? "aatail" : "aa", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private static void Grow(AwkFieldFrame frame, string operation, int target, AwkText? separator = null, bool preserveNumericCount = false)
    {
        switch (operation)
        {
            case "field": frame.AssignFieldValue(new(target), AwkValue.FromDeferredText(AwkText.FromSourceLiteralString("tail"), AwkNumberParsingMode.Decimal), separator ?? Separator, AlreadySplit); break;
            case "count": frame.AssignFieldCount(new(target), preserveNumericCount ? AwkValue.FromNumber(target) : AwkValue.FromText("0" + target + ".8"), separator ?? Separator, AlreadySplit); break;
            case "reference": frame.ExtendFieldCountForReference(new(target), AlreadySplit); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void Initialize(AwkFieldFrame frame, int count)
    {
        frame.SetRecordText(AwkText.FromSourceLiteralString(new string('a', count)));
        Assert.Equal(count, frame.GetFieldCount(_ => Enumerable.Range(0, count).Select(index => AwkFieldText.FromRecordSlice(index, 1)).ToList()));
    }

    private static List<AwkFieldText> AlreadySplit(AwkText _) => throw new InvalidOperationException("Already split.");

    private static AwkFieldFrame Frame(AwkRuntimeCounters counters, AwkMemoryGovernor governor, Action checkpoint) =>
        new(counters, governor, ManyFields * 2, _ => { }, value => value.ByteText, _ => { }, checkpoint);

    private static AwkMemoryGovernor Governor(AwkRuntimeCounters counters, long budget = 8 << 20) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(budget), counters);

}
