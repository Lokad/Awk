namespace Lokad.Awk.Tests;

public sealed class AwkIncrementalFieldTests
{
    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void PreparedPrefixUsesOnlyAdmittedCellsAndCapturedConfiguration(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: separator);
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, "warm", "two", "three", "tail")));
            Assert.Equal(4, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString(string.Join(separator, "one", "two", "three", "tail"));
            frame.SetRecordText(record);
            var materialized = counters[AwkCounter.FieldsPrepared];
            var text = counters[AwkCounter.TextAllocations];
            var growth = 0;
            memory.Budget.SetAvailableBytes(0);
            memory.Budget.OnGrowth = _ => growth++;
            frame.PrepareAdmittedFields(3);
            Assert.Equal(0, growth);
            Assert.Equal(materialized + 3, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(text, counters[AwkCounter.TextAllocations]);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.True(frame.TryGetUnmaterializedFieldBytes(new(3), out var bytes));
            Assert.True(bytes.SequenceEqual("three"u8));
            memory.Budget.OnGrowth = null;
            memory.Budget.SetAvailableBytes(8 << 20);
            context.Splitter.AssignFieldSeparator(AwkText.FromSourceLiteralString(","));
            Assert.Equal(4, frame.GetFieldCount());
            Assert.Equal("tail", frame.GetFieldValue(new(4)).AsString());
            frame.SetRecordText(AwkText.FromSourceLiteralString("next,row"));
            Assert.Equal(2, frame.GetFieldCount());
            Assert.Equal("row", frame.GetFieldValue(new(2)).AsString());
            frame.ResetRecord();
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.Equal(memoryBaseline, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedPrefixNeverMaterializesDeferredRecordBytes(bool alreadyMaterialized)
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
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("warm two three tail"));
            Assert.Equal(4, frame.GetFieldCount());
            var record = AwkText.Concat([AwkText.FromSourceLiteralString("one "), AwkText.FromSourceLiteralString("two three tail")], counters, memory.Accounting);
            if (alreadyMaterialized) _ = record.AsSpan();
            frame.SetRecordText(record);
            var materialized = counters[AwkCounter.FieldsPrepared];
            var texts = counters[AwkCounter.TextAllocations];
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            frame.PrepareAdmittedFields(3);
            Assert.Equal(0, memory.Budget.Refusals);
            Assert.Equal(texts, counters[AwkCounter.TextAllocations]);
            Assert.Equal(materialized + (alreadyMaterialized ? 3 : 0), counters[AwkCounter.FieldsPrepared]);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            var fields = counters[AwkCounter.FieldsPrepared];
            var failure = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(fields, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void CanceledPreparedPrefixRollsBackItsTailAndKeepsCachedValuesFunded()
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
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("warm two three tail"));
            Assert.Equal(4, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString("keep two " + new string('x', 16_384) + " tail");
            frame.SetRecordText(record);
            var kept = frame.GetFieldValue(new(1));
            var committed = memory.Accounting.CurrentCommittedBytes;
            var materialized = counters[AwkCounter.FieldsPrepared];
            cancellation.Cancel();
            var failure = Assert.ThrowsAny<OperationCanceledException>(() => frame.PrepareAdmittedFields(3));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.Equal(kept, frame.GetFieldValue(new(1)));
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: "|");
            context.Frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(record);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            frame.PrepareAdmittedFields(3);
            Assert.Equal("two", frame.GetFieldValue(new(2)).AsString());
            Assert.Equal(16_384, frame.GetFieldValue(new(3)).ByteText.ByteLength);
            Assert.Equal(4, frame.GetFieldCount());
            frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void PreparedPrefixLeavesColdAdmissionToTheActualFieldRead()
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
            var frame = context.Frame;
            var record = AwkText.FromSourceLiteralString("one two three");
            frame.SetRecordText(record);
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            frame.PrepareAdmittedFields(3);

            Assert.Equal(0, memory.Budget.Refusals);
            var failure = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(0, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void RecordKeepsTheSeparatorCapturedBeforeAnAdmissionCallbackChangesConfiguration()
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBaseline = memory.Accounting.CurrentCommittedBytes;
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: "|");
            memoryBaseline = context.BaselineBytes;
            memoryBudgetBaseline = context.BaselineBudget;
            var armed = true;
            memory.Budget.OnGrowth = _ =>
            {
                if (!armed) return;
                armed = false;
                context.Splitter.AssignFieldSeparator(AwkText.FromSourceLiteralString(","));
            };
            context.Frame.SetRecordText(AwkText.FromSourceLiteralString("left|right"));
            Assert.False(armed);
            Assert.Equal(2, context.Frame.GetFieldCount());
            Assert.Equal("right", context.Frame.GetFieldValue(new(2)).AsString());
            context.Frame.SetRecordText(AwkText.FromSourceLiteralString("next,row"));
            Assert.Equal(2, context.Frame.GetFieldCount());
            Assert.Equal("row", context.Frame.GetFieldValue(new(2)).AsString());
            context.Frame.ResetRecord();
            Assert.Equal(memoryBaseline, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void RefusedRecordReplacementPreservesTheOwnedSmallBuffer()
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            var old = AwkText.FromSourceLiteralString("left right");
            frame.SetRecordText(old, splitter.CaptureRecordPlan());
            Assert.Equal("left", Read(frame, 1));
            Assert.Equal("right", Read(frame, 2));
            var committed = accounting.CurrentCommittedBytes;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            budget.OnRefusal = () => Assert.True(old.HasSameStorage(frame.RecordAwkText));
            var error = Assert.Throws<AwkRuntimeException>(() => frame.SetRecordText(
                AwkText.FromSourceLiteralString(new string('x', 256 << 10)), splitter.CaptureRecordPlan()));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(old.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("right", Read(frame, 2));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public void ReusedBuffersKeepCapturedSeparatorsAndMutationSnapshots(bool governed, string separator)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString("warm" + separator + "row"), splitter.CaptureRecordPlan());
            Assert.Equal("warm", Read(frame, 1));
            frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + "δ"), splitter.CaptureRecordPlan());
            splitter.AssignFieldSeparator(AwkText.FromSourceLiteralString(","));
            Assert.Equal("café", Read(frame, 1));
            Assert.Equal("δ", Read(frame, 2));
            var saved = frame.GetFieldValue(new(2));
            frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"));
            Assert.Equal("café|changed", frame.RecordAwkText.AsString());
            frame.SetRecordText(AwkText.FromSourceLiteralString("u,v"), splitter.CaptureRecordPlan());
            Assert.Equal("u", Read(frame, 1));
            Assert.Equal("v", Read(frame, 2));
            Assert.Equal("δ", saved.AsString());

            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public void SmallFieldListsReuseAcrossRecordsAndRefundOnReset(bool governed, string separator)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            AwkValue saved = default;
            for (var repeat = 0; repeat < 3; repeat++)
            {
                frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + repeat), splitter.CaptureRecordPlan());
                Assert.Equal("café", Read(frame, 1));
                Assert.Equal(repeat.ToString(System.Globalization.CultureInfo.InvariantCulture), Read(frame, 2));
                Assert.Equal(2, frame.GetFieldCount());

                if (repeat == 0) saved = frame.GetFieldValue(new(2));
            }
            Assert.Equal("0", saved.AsString());
            Assert.Equal(6, counters[AwkCounter.FieldsPrepared]);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
            frame.AssignFieldValue(new(1), AwkValue.FromText("fresh"), AwkText.FromSourceLiteralString(" "));
            Assert.Equal("fresh", frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReusedFieldListCancellationClearsUnpublishedDescriptors(bool governed)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString("old value"), splitter.CaptureRecordPlan());
            Assert.Equal("old", Read(frame, 1));
            var source = AwkText.FromSourceLiteralString("first second " + new string('x', 8192));
            frame.SetRecordText(source, splitter.CaptureRecordPlan());
            var committed = accounting.CurrentCommittedBytes;
            var materialized = counters[AwkCounter.FieldsPrepared];
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount());

            Assert.Equal(materialized, counters[AwkCounter.FieldsPrepared]);
            Assert.True(source.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: "|");
            context.Frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(source);

            Assert.Equal("first", Read(frame, 1));
            Assert.Equal(1, retry.State.Counters[AwkCounter.FieldsPrepared]);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LargeFieldListsAreDiscardedBeforeSmallReuse(bool governed)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", 1024))), splitter.CaptureRecordPlan());
            Assert.Equal(1024, frame.GetFieldCount());
            frame.SetRecordText(AwkText.FromSourceLiteralString("small row"), splitter.CaptureRecordPlan());
            Assert.Equal("small", Read(frame, 1));

            frame.SetRecordText(AwkText.FromSourceLiteralString("next row"), splitter.CaptureRecordPlan());
            Assert.Equal("next", Read(frame, 1));

            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public void PrefixReadsCompleteWithoutLosingCachedValuesAndRefundOnReset(bool governed, string separator)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            var words = new[] { "alpha", "beta", "café" }.Concat(Enumerable.Repeat("tail", 61)).ToArray();
            for (var repeat = 0; repeat < 2; repeat++)
            {
                var before = counters[AwkCounter.FieldsPrepared];
                frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, words)), splitter.CaptureRecordPlan());
                var plan = frame.SplitPlan;
                Assert.Equal("alpha", Read(frame, 1));
                Assert.Equal(before + 1, counters[AwkCounter.FieldsPrepared]);
                Assert.Equal(plan, frame.SplitPlan);
                var cached = frame.GetFieldValue(new(3));
                Assert.Equal("café", cached.AsString());
                Assert.Equal(before + 3, counters[AwkCounter.FieldsPrepared]);
                Assert.Equal("beta", Read(frame, 2));
                Assert.Equal(64, frame.GetFieldCount());
                Assert.Equal(before + 64, counters[AwkCounter.FieldsPrepared]);
                Assert.Equal(cached, frame.GetFieldValue(new(3)));
                Assert.Equal(default, frame.SplitPlan);
                frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"));
                Assert.Equal(string.Join("|", words.Select((s, i) => i == 1 ? "changed" : s)), frame.RecordAwkText.AsString());
                frame.ResetRecord();
                Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
                Assert.Equal(0, accounting.CurrentReservedBytes);
                Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
            }
            Assert.Equal(2, counters[AwkCounter.FieldSplits]);
            Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void EmptyAndTrailingFieldsAndHugeAbsentIndicesKeepOrdinarySemantics(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var context = new AwkFieldTestContext(counters, separator: separator, outputSeparator: "|");
        var frame = context.Frame;
        var splitter = context.Splitter;
        var word = new string('x', 160);
        var source = separator == " " ? " \t" + word + " \tbeta \n" : "|" + word + "||";
        frame.SetRecordText(AwkText.FromSourceLiteralString(source), splitter.CaptureRecordPlan());
        Assert.Equal(separator == " " ? word : "", Read(frame, 1));
        Assert.Equal(1, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal(separator == " " ? "beta" : word, Read(frame, 2));
        Assert.Equal("", Read(frame, 3));
        Assert.Equal("", Read(frame, 4));
        var count = separator == " " ? 2 : 4;
        Assert.Equal(count, frame.GetFieldCount());
        Assert.Equal(count, counters[AwkCounter.FieldsPrepared]);
        Assert.Equal("", Read(frame, int.MaxValue));
        Assert.Equal(1, counters[AwkCounter.FieldSplits]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledExtensionPreservesPublishedPrefixAndCanRetry(bool governed)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            var third = new string('z', 16_384);
            var record = AwkText.FromSourceLiteralString("first second " + third + " tail");
            frame.SetRecordText(record, splitter.CaptureRecordPlan());
            Assert.Equal("first", Read(frame, 1));
            var committed = accounting.CurrentCommittedBytes;
            var plan = frame.SplitPlan;
            cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() => Read(frame, 3));
            Assert.Equal(cancellation.Token, error.CancellationToken);

            Assert.Equal(1, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: "|");
            context.Frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(record, plan);

            Assert.Equal(plan, frame.SplitPlan);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("first", Read(frame, 1));
            Assert.Equal(third, Read(frame, 3));
            Assert.Equal(3, retry.State.Counters[AwkCounter.FieldsPrepared]);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(true)]
    public void RefusedExtensionLeavesThePrefixAndAccountingReusable(bool governed)
    {
        const long limit = 8 << 20;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBaseline = fieldMemory.Accounting.CurrentCommittedBytes;
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(limit);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory, separator: "|", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString("first|second|third|" + new string('t', 160)), splitter.CaptureRecordPlan());
            Assert.Equal("first", Read(frame, 1));
            var committed = accounting.CurrentCommittedBytes;
            var plan = frame.SplitPlan;
            var pressure = budget.Limit - budget.Reserved - 1;
            accounting.Reserve(pressure);
            var error = Assert.Throws<AwkRuntimeException>(() => Read(frame, 3));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(pressure, accounting.CurrentReservedBytes);
            Assert.Equal(plan, frame.SplitPlan);
            Assert.Equal(1, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal("first", Read(frame, 1));
            accounting.ReleaseReserved(pressure);
            Assert.Equal("third", Read(frame, 3));
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Fact]
    public void RefusedInputSliceCopyPreservesTheRecordWithoutRetryingAdmission()
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString("first middle café"), splitter.CaptureRecordPlan());
            Assert.Equal(3, frame.GetFieldCount());
            var record = frame.RecordAwkText;
            var committed = accounting.CurrentCommittedBytes;
            var separator = AwkText.FromSourceLiteralString("|");
            budget.RefuseNextAdmissionMinimumGrowth = 1;

            var error = Assert.Throws<AwkRuntimeException>(() => frame.TryCopyInputField(new(1), new(2)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("middle", Read(frame, 2));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledInputSliceCopyCannotPublishItsDescriptor(bool governed)
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
            using var context = new AwkFieldTestContext(fieldMemory, separator: " ", outputSeparator: "|");
            fieldMemoryBaseline = context.BaselineBytes;
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var splitter = context.Splitter;
            frame.SetRecordText(AwkText.FromSourceLiteralString("first middle café"), splitter.CaptureRecordPlan());
            Assert.Equal(3, frame.GetFieldCount());
            var record = frame.RecordAwkText;
            var committed = accounting.CurrentCommittedBytes;
            var separator = AwkText.FromSourceLiteralString("|");
            cancellation.Cancel();

            var error = Assert.Throws<OperationCanceledException>(() => frame.TryCopyInputField(new(1), new(2)));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("middle", Read(frame, 2));
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBaseline, accounting.CurrentCommittedBytes);
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void WarmFieldStorageNeedsNoAdmissionForRecordLength(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            var middle = separator == " " ? new string(' ', 16_384) : "|" + new string('x', 16_384) + "|";
            var record = AwkText.FromSourceLiteralString("01" + middle + "02");
            var fields = separator == " " ? 2 : 3;
            frame.SetRecordText(record);
            Assert.Equal(fields, frame.GetFieldCount());
            frame.SetRecordText(record);
            var committed = accounting.CurrentCommittedBytes;
            budget.SetAvailableBytes(0);

            Assert.Equal(fields, frame.GetFieldCount());
            Assert.Equal(1, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            Assert.Equal(2, frame.GetFieldNumber(new(fields), AwkNumberParsingMode.Decimal));
            Assert.InRange(accounting.CurrentCommittedBytes, 0, committed);
            Assert.Equal(0, counters[AwkCounter.TextAllocations]);

            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void RefusedDescriptorGrowthLeavesRecordAndPublishedFieldsUnchanged(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
            Assert.Equal(2, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("x", 128)));
            frame.SetRecordText(record);
            var committed = accounting.CurrentCommittedBytes;
            var publishedFields = counters[AwkCounter.FieldsPrepared];
            budget.SetAvailableBytes(0);

            var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount());

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(publishedFields, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void CancellationDuringStagedGrowthRefundsUnpublishedStorage(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var fieldMemory = new CooperativeTestMemory(8 << 20, enabled: true).InitializeRuntime(counters, cancellation.Token);
        var fieldMemoryBudget = fieldMemory.Budget;
        using (fieldMemory)
        {
            var fieldMemoryBudgetBaseline = fieldMemory.Budget.Reserved;
            fieldMemory.Budget.SetAvailableBytes(8 << 20);
            var budget = fieldMemory.Budget;
            var accounting = fieldMemory.Accounting;
            using var context = new AwkFieldTestContext(fieldMemory, separator: separator, outputSeparator: "|");
            fieldMemoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
            Assert.Equal(2, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("x", 128)));
            frame.SetRecordText(record);
            var committed = accounting.CurrentCommittedBytes;
            var publishedFields = counters[AwkCounter.FieldsPrepared];
            budget.OnGrowth = _ =>
            {
                // Cancel with some construction storage already live, before publication.
                if (accounting.CurrentCommittedBytes > committed) cancellation.Cancel();
            };

            var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount());
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(publishedFields, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(fieldMemoryBudgetBaseline, budget.Reserved);
        }
        Assert.Equal(0, fieldMemoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CanceledAppendRetainsCachedPrefixAndOwnedCapacity(bool governed, bool complete)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: " ", outputSeparator: "|");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("warm", 64))));
            Assert.Equal(64, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString("0007 short " + new string('z', 16_384) + " tail");
            frame.SetRecordText(record);
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var saved = frame.GetFieldValue(new(1));
            var published = counters[AwkCounter.FieldsPrepared];
            var committed = memory.Accounting.CurrentCommittedBytes;
            cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() =>
            {
                if (complete) frame.GetFieldCount();
                else Read(frame, 3);
            });
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(published, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("0007", saved.AsString());
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: "|");
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(record);
            Assert.Equal("short", Read(frame, 2));
            Assert.Equal(2, retry.State.Counters[AwkCounter.FieldsPrepared]);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void AdmittedCapacityFundsPrefixExtensionWithoutAnotherReservation(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: separator, outputSeparator: "|");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("warm", 64))));
            Assert.Equal(64, frame.GetFieldCount());
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, "0007", "0008", "0009", new string('t', 256))));
            var committed = memory.Accounting.CurrentCommittedBytes;

            memory.Budget.SetAvailableBytes(0);
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            Assert.Equal(9, frame.GetFieldNumber(new(3), AwkNumberParsingMode.Decimal));
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);

            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void AdmittedCapacityFundsCompletionWithoutAnotherReservation(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: separator, outputSeparator: "|");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("warm", 64))));
            Assert.Equal(64, frame.GetFieldCount());
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, "0007", "0008", "0009", new string('t', 256))));
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var committed = memory.Accounting.CurrentCommittedBytes;
            memory.Budget.SetAvailableBytes(0);
            Assert.Equal(4, frame.GetFieldCount());
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            // Completion can relinquish its captured separator, but needs no new funding.
            Assert.InRange(memory.Accounting.CurrentCommittedBytes, 1, committed);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void RefusedCompletionGrowthKeepsThePreviouslyPublishedPrefix(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: separator, outputSeparator: "|");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
            Assert.Equal(2, frame.GetFieldCount());
            var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("000007", 128)));
            frame.SetRecordText(record);
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var saved = frame.GetFieldValue(new(1));
            var committed = memory.Accounting.CurrentCommittedBytes;
            var published = counters[AwkCounter.FieldsPrepared];
            memory.Budget.SetAvailableBytes(0);
            Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount());
            Assert.Equal(published, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal("000007", saved.AsString());
            Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    private static string Read(AwkFieldFrame frame, int index)
    {
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(index), out var bytes));
        return AwkUtf8Codec.Decode(bytes);
    }
}
