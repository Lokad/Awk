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
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, AwkMemoryAccounting.Disabled));
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
        frame.SetRecordText(AwkText.FromString("alpha beta", counters, governor));

        var first = frame.GetFieldValue(new AwkFieldIndex(1));
        var allocationsAfterFirstRead = counters.AwkTextAllocations;
        var second = frame.GetFieldValue(new AwkFieldIndex(1));

        Assert.Equal("alpha", first.AsString());
        Assert.Equal("alpha", second.AsString());
        Assert.Equal(allocationsAfterFirstRead, counters.AwkTextAllocations);

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
        using var memoryForGovernor = new CooperativeTestMemory(500, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, governor, format: value => value.ByteText);
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AwkFieldFrame_MaterializationReleasesRetainedPlan(int mode)
    {
        using var memoryForGovernor = new CooperativeTestMemory(8 << 20, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using (var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, governor))
        {
            var frame = context.Frame;
            var plan = CreatePlan(mode, new string('x', 200));
            var record = AwkText.FromSourceLiteralString("keep");
            var expected = mode switch { 1 => 0, 2 => 4, _ => 1 };
            // Cache owners remain alive after the record's captured plan retires.
            frame.SetRecordText(record, plan);
            Assert.Equal(expected, frame.GetFieldCount());
            frame.ResetRecord();
            var fieldBytes = AwkRuntimeMemory.EstimateFieldTextListBytes(expected);
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
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
        using var memory = new CooperativeTestMemory(8 << 20, counters, governed);
        var accounting = memory.Accounting;
        using (var context = new AwkFieldTestContext(counters, accounting))
        {
            var source = mode switch { 0 => "é,007", 1 => "a,\"b\"\"c\",007", 2 => "é7", 3 => "é127", _ => "é::007" };
            var expected = mode switch
            {
                0 => new[] { "é", "007" }, 1 => ["a", "b\"c", "007"],
                2 => ["é", "7"], 3 => ["é1", "27"], _ => ["é", "007"]
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
                var allocations = counters.AwkTextAllocations;
                var encodes = counters.Utf8Encodes;
                var decodes = counters.StringDecodes;
                var storage = context.Frame.InputRecordStorage;
                storage.StageValidated(bytes);
                context.Frame.SetBufferedRecord(storage, storage.CurrentSlot,
                    context.Frame.PrepareRecordPublication(AwkText.Empty, plan));
                if (governed && repeat > 0 && mode is 0 or 2 or 3)
                    memory.Budget.SetAvailableBytes(0);

                Assert.Equal(expected.Length, context.Frame.GetFieldCount());
                Assert.Equal(mode is 1 or 4 ? 1 : 0, counters.AwkTextAllocations - allocations);
                Assert.Equal(encodes, counters.Utf8Encodes);
                Assert.Equal(decodes, counters.StringDecodes);
                memory.Budget.SetAvailableBytes(8 << 20);
                for (var field = 0; field < expected.Length; field++)
                    Assert.Equal(expected[field], context.Frame.GetFieldValue(new(field + 1)).AsString());
                Assert.Equal(source, context.Frame.RecordAwkText.AsString());
            }
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void RefusedCsvPreparationCannotAllocateDecodedFieldsOrPublishDescriptors()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = new AwkFieldTestContext(counters, memory.Accounting);
        var bytes = System.Text.Encoding.UTF8.GetBytes("\"" + new string('x', 16_384) + "\"\"y\",z");
        var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.Csv, default, AwkText.Empty, [],
            AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
        var storage = context.Frame.InputRecordStorage;
        storage.StageValidated(bytes);
        context.Frame.SetBufferedRecord(storage, storage.CurrentSlot,
            context.Frame.PrepareRecordPublication(AwkText.Empty, plan));
        memory.Budget.SetAvailableBytes(0);

        var error = Assert.Throws<AwkRuntimeException>(() => context.Frame.GetFieldCount());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.True(context.Frame.TryGetBufferedRecordBytes(out var unchanged));
        Assert.True(unchanged.SequenceEqual(bytes));
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        context.Frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 5)]
    [InlineData(true, 1)]
    [InlineData(true, 5)]
    public void InterruptedEscapedCsvStagingRetiresUnpublishedTextAndCanRetry(bool cancel, int decodedFields)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var creations = 0;
        using var context = new AwkFieldTestContext(counters, memory.Accounting,
            checkpoint: () => token.ThrowIfCancellationRequested(), createInputValue: text =>
            {
                creations++;
                return AwkValue.FromText(text);
            });
        var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.Csv, default, AwkText.Empty, [],
            AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(',', Enumerable.Repeat("\"a\"\"b\"", 4))), plan);
        Assert.Equal(4, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString(string.Join(',', Enumerable.Repeat("\"c\"\"d\"", 6)));
        frame.SetRecordText(record, plan);
        var committed = memory.Accounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        var materialized = counters.FieldsMaterialized;
        memory.Budget.OnGrowth = _ =>
        {
            if (counters.AwkTextAllocations - allocations < decodedFields) return;
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
        Assert.Equal(0, creations);
        Assert.Equal(materialized, counters.FieldsMaterialized);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.True(record.HasSameStorage(frame.RecordAwkText));

        memory.Budget.OnGrowth = null;
        token = CancellationToken.None;
        Assert.Equal(6, frame.GetFieldCount());
        Assert.Equal(0, creations);
        for (var index = 1; index <= 6; index++)
            Assert.Equal("c\"d", frame.GetFieldValue(new(index)).AsString());
        Assert.Equal(6, creations);
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconstructedCsvClassificationAndCopyKeepOnlyTheirOwnedPayload(bool classify)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = new AwkFieldTestContext(counters, memory.Accounting,
            resolveOutputFieldSeparator: static () => AwkText.Empty);
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
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
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

    private static AwkFieldTestContext CreateFrame(AwkRuntimeCounters counters) =>
        new(
            counters, AwkMemoryAccounting.Disabled, value => value.ByteText);

}
