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
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: "");
            memoryForGovernorBaseline = context.BaselineBytes;
            var frame = context.Frame;
            Initialize(frame, 2);
            frame.AssignFieldCount(new(2), AwkValue.FromText("02.8"), Separator);
            var previousRecord = frame.RecordAwkText;
            var first = frame.GetFieldValue(new(1));
            var committed = governor.LiveBytes;
            var materialized = counters[AwkCounter.FieldsPrepared];
            memoryForGovernor.Budget.OnGrowth = _ => cancellation.Cancel();

            var error = Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, target));

            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(committed, governor.LiveBytes);
            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(2, frame.GetFieldCount());
            Assert.Equal("02.8", frame.GetFieldCountValue().AsString());
            Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(first, frame.GetFieldValue(new(1)));

            // The frame's callback is operation-local; retry uses a fresh, uncanceled token.
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: "", outputSeparator: " ");
            frame.ResetRecord();
            frame = retry.Frame;
            Initialize(frame, 2);
            frame.AssignFieldCount(new(2), AwkValue.FromText("02.8"), Separator);


            Grow(frame, operation, target);
            Assert.Equal(target, frame.GetFieldCount());
            Assert.Equal(operation == "count" ? "0" + target + ".8" : target.ToString(), frame.GetFieldCountValue().AsString());
            Assert.Equal(operation == "field" ? "tail" : "", frame.GetFieldValue(new(target)).AsString());
            Assert.Equal(operation == "reference" ? "a|a" : "a|a" + new string('|', target - 2) + (operation == "field" ? "tail" : ""), frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CanceledTruncationPreservesAllFieldsAndCachedValues(bool atAdmission, bool cached)
    {
        const int retained = 8_193;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            long committed = 0;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: "");
            memoryForGovernorBaseline = context.BaselineBytes;
            var frame = context.Frame;
            Initialize(frame, ManyFields);
            if (cached)
                for (var index = 1; index <= ManyFields; index++)
                    _ = frame.GetFieldValue(new(index));
            committed = governor.LiveBytes;
            var previousRecord = frame.RecordAwkText;
            var allocations = counters[AwkCounter.TextAllocations];
            if (atAdmission) memoryForGovernor.Budget.OnGrowth = _ => cancellation.Cancel();
            else cancellation.Cancel();

            var error = Assert.Throws<OperationCanceledException>(() => frame.AssignFieldCount(new(retained), Separator));

            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(committed, governor.LiveBytes);
            Assert.Equal(ManyFields, frame.GetFieldCount());
            Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
            if (cached)
                Assert.Equal("a", frame.GetFieldValue(new(ManyFields)).AsString());

            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: "", outputSeparator: " ");
            frame.ResetRecord();
            frame = retry.Frame;
            Initialize(frame, ManyFields);
            if (cached) for (var index = 1; index <= ManyFields; index++) _ = frame.GetFieldValue(new(index));
            allocations = counters[AwkCounter.TextAllocations];

            frame.AssignFieldCount(new(retained), Separator);
            Assert.Equal(retained, frame.GetFieldCount());
            if (cached)
            {
                Assert.Equal("a", frame.GetFieldValue(new(retained)).AsString());
                Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
            }
            Assert.Equal(string.Join("|", Enumerable.Repeat("a", retained)), frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void BoundedMutationChecksBeforeChangingLiveState(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: "");
            var frame = context.Frame;
            Initialize(frame, 2);
            var previousRecord = frame.RecordAwkText;
            var committed = governor.LiveBytes;
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => Grow(frame, operation, 33));

            Assert.Equal(2, frame.GetFieldCount());
            Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void StagingPeakMustBeAdmittedBeforeGrowth(string operation)
    {
        const int original = 8_193;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = Governor().InitializeRuntime(counters);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: "");
            memoryForGovernorBaseline = context.BaselineBytes;
            var frame = context.Frame;
            Initialize(frame, original);
            // Admit the actual splitter capacity first. The final size fits; its
            // overlapping staged replacement does not fit the remaining allowance.
            memoryForGovernor.Budget.SetAvailableBytes(1_024);
            var committed = governor.LiveBytes;
            var previousRecord = frame.RecordAwkText;

            var error = Assert.Throws<AwkRuntimeException>(() => Grow(frame, operation, ManyFields, AwkText.Empty));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(original, frame.GetFieldCount());
            Assert.True(previousRecord.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, governor.LiveBytes);
            frame.ResetRecord();
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void SuccessfulGrowthTransfersTheStagingChargeExactlyOnce(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = Governor().InitializeRuntime(counters);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: "");
            memoryForGovernorBaseline = context.BaselineBytes;
            var frame = context.Frame;
            Initialize(frame, 2);
            var committed = governor.LiveBytes;

            Grow(frame, operation, ManyFields, AwkText.Empty, preserveNumericCount: true);

            var expected = committed - AwkRuntimeMemory.EstimateRecordFieldListBytes(2) + AwkRuntimeMemory.EstimateRecordFieldListBytes(ManyFields);
            if (operation == "field")
            {
                expected += AwkRuntimeMemory.EstimateTextBytes(AwkText.FromSourceLiteralString("tail"));
                expected += AwkRuntimeMemory.EstimateFieldValueListBytes(4);
                Assert.Equal(expected, governor.LiveBytes);
            }
            else
                Assert.Equal(expected, governor.LiveBytes);
            Assert.Equal(ManyFields, frame.GetFieldCount());
            Assert.Equal(operation == "reference" ? "aa" : operation == "field" ? "aatail" : "aa", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("count")]
    [InlineData("reference")]
    public void GrowthPublishesFieldCountBeforeRefundingReplacedStorage(string operation)
    {
        var memory = Governor().InitializeRuntime(AwkRuntimeCounters.Disabled);
        var budget = memory.Budget;
        using (memory)
        {
            using var context = new AwkFieldTestContext(memory, separator: "");
            var frame = context.Frame;
            Initialize(frame, 2);
            (int Fields, double Count)? observed = null;
            budget.OnRefund = () =>
            {
                if (observed is null && frame.TryGetFieldCountNumber(out var count))
                    observed = (frame.GetFieldCount(), count);
            };

            Grow(frame, operation, ManyFields, AwkText.Empty, preserveNumericCount: true);

            budget.OnRefund = null;
            Assert.Equal((ManyFields, (double)ManyFields), observed);
        }
        Assert.Equal(0, budget.Reserved);
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

    private static CooperativeTestMemory Governor(long budget = 8 << 20) =>
        new CooperativeTestMemory(budget);

}
