using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldFrameTests
{
    [Fact]
    public void AwkFieldFrame_UsesItsBoundSplitter()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var context = CreateFrame(counters);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromString("alpha beta"));
        var count = frame.GetFieldCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public void AwkFieldFrame_CachesRecordSliceFieldValues()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryAccounting.Disabled;
        using var context = CreateFrame(counters);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromString("alpha beta"));

        var first = frame.GetFieldValue(new AwkFieldIndex(1));
        var allocationsAfterFirstRead = counters[AwkCounter.TextAllocations];
        var second = frame.GetFieldValue(new AwkFieldIndex(1));

        Assert.Equal("alpha", first.AsString());
        Assert.Equal("alpha", second.AsString());
        Assert.Equal(allocationsAfterFirstRead, counters[AwkCounter.TextAllocations]);

    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void AwkFieldFrame_RecordPlanMemoryFailurePreservesRecordAndPriorPlan(int mode, bool assignValue)
    {
        var memoryForGovernor = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(500);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor);
            var frame = context.Frame;
            var priorPlan = CreatePlan(0, ",");
            frame.SetRecordText(AwkText.FromSourceLiteralString("keep"), priorPlan);
            var committedBefore = governor.CurrentCommittedBytes;
            var replacementPlan = CreatePlan(mode, new string('x', 200));

            var exception = Assert.Throws<AwkRuntimeException>(() =>
            {
                if (assignValue)
                    frame.SetRecordValue(AwkValue.FromText("new"), replacementPlan);
                else
                    frame.SetRecordText(AwkText.FromSourceLiteralString("new"), replacementPlan);
            });

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
            Assert.Equal("keep", frame.RecordAwkText.AsString());
            Assert.Equal(priorPlan, frame.SplitPlan);
            Assert.Equal(committedBefore, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AwkFieldFrame_MaterializationReleasesRetainedPlan(int mode)
    {
        var memoryForGovernor = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using (var context = new AwkFieldTestContext(memoryForGovernor))
            {
                var frame = context.Frame;
                var plan = CreatePlan(mode, new string('x', 200));
                var record = AwkText.FromSourceLiteralString("keep");
                var expected = mode switch { 1 => 0, 2 => 4, _ => 1 };
                // Cache owners remain alive after the record's captured plan retires.
                frame.SetRecordText(record, plan);
                Assert.Equal(expected, frame.GetFieldCount());
                frame.ResetRecord();
                var fieldBytes = AwkRuntimeMemory.EstimateRecordFieldListBytes(expected);
                var serviceBytes = governor.CurrentCommittedBytes;
                frame.SetRecordText(record, plan);
                var committedBefore = governor.CurrentCommittedBytes;
                Assert.Equal(expected, frame.GetFieldCount());
                Assert.Equal(default, frame.SplitPlan);
                Assert.Equal(plan.RetainedMemoryBytes,
                    committedBefore + fieldBytes - governor.CurrentCommittedBytes);
                Assert.Equal(0, governor.CurrentReservedBytes);
                Assert.Equal("keep", frame.RecordAwkText.AsString());
                frame.ResetRecord();
                Assert.Equal(serviceBytes, governor.CurrentCommittedBytes);
                Assert.Equal(0, frame.GetFieldCount());
                Assert.Equal(string.Empty, frame.RecordAwkText.AsString());
            }
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    public void BufferedFieldsOwnOnlyDecodedEscapesOrRequiredRegexSubjects(int mode, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20, governed).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using (var context = new AwkFieldTestContext(memory))
            {
                var source = mode switch { 0 => "é,007", 1 => "a,\"b\"\"c\",007", 2 => "é7", 3 => "é127", _ => "é::007" };
                var expected = mode switch
                {
                    0 => new[] { "é", "007" },
                    1 => ["a", "b\"c", "007"],
                    2 => ["é", "7"],
                    3 => ["é1", "27"],
                    _ => ["é", "007"]
                };
                var separator = AwkText.FromSourceLiteralString(mode == 4 ? ":+" : "");
                var plan = new AwkRecordSplitPlan(mode < 2 ? AwkFieldSplittingMode.Csv : mode == 3
                    ? AwkFieldSplittingMode.FieldWidths : AwkFieldSplittingMode.FieldSeparator,
                    AwkFieldSeparatorSpec.FromByteText(separator), AwkText.Empty,
                    mode == 3 ? [new(new(0), new(2)), new(new(0), new(2))] : [],
                    AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
                if (plan.RequiresRegexSubject) context.Splitter.ValidateFieldSeparator(separator);
                var bytes = System.Text.Encoding.UTF8.GetBytes(source);
                for (var repeat = 0; repeat < 2; repeat++)
                {
                    var allocations = counters[AwkCounter.TextAllocations];
                    var encodes = counters[AwkCounter.Utf8Encodes];
                    var decodes = counters[AwkCounter.StringDecodes];
                    var storage = context.Frame.InputRecordStorage;
                    storage.StageValidated(bytes);
                    context.Frame.SetBufferedRecord(storage, storage.CurrentSlot,
                        context.Frame.PrepareRecordPublication(AwkText.Empty, plan, bytes.Length));
                    if (governed && repeat > 0 && mode is 0 or 2 or 3)
                        memory.Budget.SetAvailableBytes(0);

                    Assert.Equal(expected.Length, context.Frame.GetFieldCount());
                    Assert.Equal(mode is 1 or 4 ? 1 : 0, counters[AwkCounter.TextAllocations] - allocations);
                    Assert.Equal(encodes, counters[AwkCounter.Utf8Encodes]);
                    Assert.Equal(decodes, counters[AwkCounter.StringDecodes]);
                    memory.Budget.SetAvailableBytes(8 << 20);
                    for (var field = 0; field < expected.Length; field++)
                        Assert.Equal(expected[field], context.Frame.GetFieldValue(new(field + 1)).AsString());
                    Assert.Equal(source, context.Frame.RecordAwkText.AsString());
                }
                var beforeReset = memory.Accounting.CurrentCommittedBytes;
                context.Frame.ResetRecord();
                Assert.Equal(0, context.Frame.GetFieldCount());
                Assert.True(!governed || memory.Accounting.CurrentCommittedBytes < beforeReset);
                // Pattern/cache owners remain funded until the runtime is retired.
            }
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void RefusedCsvPreparationCannotAllocateDecodedFieldsOrPublishDescriptors()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory);
            memoryBudgetBaseline = context.BaselineBudget;
            var bytes = System.Text.Encoding.UTF8.GetBytes("\"" + new string('x', 16_384) + "\"\"y\",z");
            var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.Csv, default, AwkText.Empty, [],
                AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
            var storage = context.Frame.InputRecordStorage;
            storage.StageValidated(bytes);
            context.Frame.SetBufferedRecord(storage, storage.CurrentSlot,
                context.Frame.PrepareRecordPublication(AwkText.Empty, plan, bytes.Length));
            memory.Budget.SetAvailableBytes(0);

            var error = Assert.Throws<AwkRuntimeException>(() => context.Frame.GetFieldCount());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, counters[AwkCounter.TextAllocations]);
            Assert.Equal(0, counters[AwkCounter.FieldsPrepared]);
            Assert.True(context.Frame.TryGetBufferedRecordBytes(out var unchanged));
            Assert.True(unchanged.SequenceEqual(bytes));
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            context.Frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 5)]
    [InlineData(true, 1)]
    [InlineData(true, 5)]
    public void InterruptedEscapedCsvStagingRetiresUnpublishedTextAndCanRetry(bool cancel, int decodedFields)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);

            using var context = new AwkFieldTestContext(memory);
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.Csv, default, AwkText.Empty, [],
                AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(',', Enumerable.Repeat("\"a\"\"b\"", 4))), plan);
            Assert.Equal(4, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString(string.Join(',', Enumerable.Repeat("\"c\"\"d\"", 6)));
            frame.SetRecordText(record, plan);
            var committed = memory.Accounting.CurrentCommittedBytes;
            var allocations = counters[AwkCounter.TextAllocations];
            var materialized = counters[AwkCounter.FieldsPrepared];
            memory.Budget.OnGrowth = _ =>
            {
                if (counters[AwkCounter.TextAllocations] - allocations < decodedFields) return;
                memory.Budget.OnGrowth = null;
                if (cancel) cancellation.Cancel();
                else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            };

            if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => frame.GetFieldCount());
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount());
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            }

            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));

            memory.Budget.OnGrowth = null;
            using var retry = cancel ? new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled()) : null;
            if (retry is not null)
            {
                context.Frame.ResetRecord();
                frame = retry.Frame;
                frame.SetRecordText(record, plan);
            }
            Assert.Equal(6, frame.GetFieldCount());

            for (var index = 1; index <= 6; index++)
            {
                Assert.True(frame.TryGetFieldStringLength(new(index), false, out var length));
                Assert.Equal(3, length);
            }

            for (var index = 1; index <= 6; index++)
                Assert.Equal("c\"d", frame.GetFieldValue(new(index)).AsString());

            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconstructedCsvClassificationAndCopyKeepOnlyTheirOwnedPayload(bool classify)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, outputSeparator: "");
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.Csv, default, AwkText.Empty, [],
                AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
            frame.SetRecordText(AwkText.FromSourceLiteralString("\"a\"\"b\",\"c\"\"d\",007"), plan);
            Assert.Equal(3, frame.GetFieldCount());
            frame.AssignFieldCount(new(3), AwkText.Empty);
            Assert.Equal("a\"bc\"d007", frame.RecordAwkText.AsString());
            var committed = memory.Accounting.CurrentCommittedBytes;
            if (classify)
            {
                Assert.True(frame.GetFieldComparison(new(2), AwkNumberParsingMode.Decimal).IsBorrowed);
                Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            }
            Assert.True(frame.TryCopyInputField(new(1), new(2)));
            Assert.Equal(AwkRuntimeMemory.EstimateTextBytes(3), committed - memory.Accounting.CurrentCommittedBytes);
            Assert.Equal("a\"ba\"b007", frame.RecordAwkText.AsString());
            Assert.Equal("a\"b", frame.GetFieldValue(new(2)).AsString());
            Assert.Equal(7, frame.GetFieldNumber(new(3), AwkNumberParsingMode.Decimal));
            Assert.Equal("007", frame.GetFieldValue(new(3)).AsString());
            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    private static AwkRecordSplitPlan CreatePlan(int mode, string pattern)
    {
        var text = AwkText.FromSourceLiteralString(pattern);
        return new AwkRecordSplitPlan(
            (AwkFieldSplittingMode)mode,
            mode == 0 ? AwkFieldSeparatorSpec.FromByteText(text) : default,
            mode == 1 ? text : AwkText.Empty,
            mode == 2 ? Enumerable.Repeat(new AwkFieldWidth(new(0), new(1)), 64).ToList() : [],
            AwkRegexCaseSensitivity.CaseSensitive,
            AwkRecordFieldSplittingMode.Ordinary);
    }
    private static AwkFieldTestContext CreateFrame(AwkRuntimeCounters counters) => new(counters);

}
