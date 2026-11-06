namespace Lokad.Awk.Tests;

public sealed class AwkIncrementalFieldTests
{
    [Theory]
    [InlineData(" ")] [InlineData("|")]
    public void PreparedPrefixUsesOnlyAdmittedCellsAndCapturedConfiguration(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = new AwkFieldTestContext(counters, memory.Accounting, separator: separator);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, "warm", "two", "three", "tail")));
        Assert.Equal(4, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString(string.Join(separator, "one", "two", "three", "tail"));
        frame.SetRecordText(record);
        var materialized = counters.FieldsMaterialized;
        var text = counters.AwkTextAllocations;
        var growth = 0;
        memory.Budget.SetAvailableBytes(0);
        memory.Budget.OnGrowth = _ => growth++;
        frame.PrepareAdmittedFields(3);
        Assert.Equal(0, growth);
        Assert.Equal(materialized + 3, counters.FieldsMaterialized);
        Assert.Equal(text, counters.AwkTextAllocations);
        Assert.Same(record, frame.RecordAwkText);
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
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PreparedPrefixNeverMaterializesDeferredRecordBytes(bool alreadyMaterialized)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = new AwkFieldTestContext(counters, memory.Accounting);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("warm two three tail"));
        Assert.Equal(4, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString("one ").Concat(
            AwkText.FromSourceLiteralString("two three tail"), counters, memory.Accounting);
        if (alreadyMaterialized) _ = record.AsSpan();
        frame.SetRecordText(record);
        var materialized = counters.FieldsMaterialized;
        var texts = counters.AwkTextAllocations;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        frame.PrepareAdmittedFields(3);
        Assert.Equal(0, memory.Budget.Refusals);
        Assert.Equal(texts, counters.AwkTextAllocations);
        Assert.Equal(materialized + (alreadyMaterialized ? 3 : 0), counters.FieldsMaterialized);
        Assert.Same(record, frame.RecordAwkText);
        var fields = counters.FieldsMaterialized;
        var failure = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(fields, counters.FieldsMaterialized);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CanceledPreparedPrefixRollsBackItsTailAndKeepsCachedValuesFunded()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var armed = false;
        long scanned = 0;
        using var context = new AwkFieldTestContext(counters, memory.Accounting, checkpoint: () =>
        {
            if (armed && counters.WhitespaceFieldSearchBytes - scanned >= 4096) cancellation.Cancel();
            token.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("warm two three tail"));
        Assert.Equal(4, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString("keep two " + new string('x', 16_384) + " tail");
        frame.SetRecordText(record);
        var kept = frame.GetFieldValue(new(1));
        var committed = memory.Accounting.CurrentCommittedBytes;
        var materialized = counters.FieldsMaterialized;
        scanned = counters.WhitespaceFieldSearchBytes;
        armed = true;
        var failure = Assert.ThrowsAny<OperationCanceledException>(() => frame.PrepareAdmittedFields(3));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(materialized, counters.FieldsMaterialized);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Equal(kept, frame.GetFieldValue(new(1)));
        armed = false;
        token = CancellationToken.None;
        Assert.Same(record, frame.RecordAwkText);
        frame.PrepareAdmittedFields(3);
        Assert.Equal("two", frame.GetFieldValue(new(2)).AsString());
        Assert.Equal(16_384, frame.GetFieldValue(new(3)).ByteText.ByteLength);
        Assert.Equal(4, frame.GetFieldCount());
        frame.ResetRecord();
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void PreparedPrefixLeavesColdAdmissionToTheActualFieldRead()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = new AwkFieldTestContext(counters, memory.Accounting);
        var frame = context.Frame;
        var record = AwkText.FromSourceLiteralString("one two three");
        frame.SetRecordText(record);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        frame.PrepareAdmittedFields(3);
        Assert.Equal(0, counters.FieldPrefixExtensions);
        Assert.Equal(0, memory.Budget.Refusals);
        var failure = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldValue(new(1)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void RecordKeepsTheSeparatorCapturedBeforeAnAdmissionCallbackChangesConfiguration()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var context = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, memory.Accounting, separator: "|");
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
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public async Task RefusedRecordReplacementPreservesTheOwnedSmallBuffer()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, " ");
        var frame = context.Frame;
        var splitter = context.Splitter;
        var old = AwkText.FromSourceLiteralString("left right");
        frame.SetRecordText(old, splitter.CaptureRecordPlan());
        Assert.Equal("left", Read(frame, splitter, 1));
        Assert.Equal("right", Read(frame, splitter, 2));
        var committed = accounting.CurrentCommittedBytes;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        budget.OnRefusal = () => Assert.Same(old, frame.RecordAwkText);
        var error = Assert.Throws<AwkRuntimeException>(() => frame.SetRecordText(
            AwkText.FromSourceLiteralString(new string('x', 256 << 10)), splitter.CaptureRecordPlan()));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Same(old, frame.RecordAwkText);
        Assert.Equal("right", Read(frame, splitter, 2));
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public async Task ReusedBuffersKeepCapturedSeparatorsAndMutationSnapshots(bool governed, string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, separator);
        var frame = context.Frame;
        var splitter = context.Splitter;
        frame.SetRecordText(AwkText.FromSourceLiteralString("warm" + separator + "row"), splitter.CaptureRecordPlan());
        Assert.Equal("warm", Read(frame, splitter, 1));
        frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + "δ"), splitter.CaptureRecordPlan());
        splitter.AssignFieldSeparator(AwkText.FromSourceLiteralString(","));
        Assert.Equal("café", Read(frame, splitter, 1));
        Assert.Equal("δ", Read(frame, splitter, 2));
        var saved = frame.GetFieldValue(new(2));
        frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"));
        Assert.Equal("café|changed", frame.RecordAwkText.AsString());
        frame.SetRecordText(AwkText.FromSourceLiteralString("u,v"), splitter.CaptureRecordPlan());
        Assert.Equal("u", Read(frame, splitter, 1));
        Assert.Equal("v", Read(frame, splitter, 2));
        Assert.Equal("δ", saved.AsString());
        Assert.Equal(2, counters.FieldListBufferReuses);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public async Task SmallFieldListsReuseAcrossRecordsAndRefundOnReset(bool governed, string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, separator);
        var frame = context.Frame;
        var splitter = context.Splitter;
        AwkValue saved = default;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + repeat), splitter.CaptureRecordPlan());
            Assert.Equal("café", Read(frame, splitter, 1));
            Assert.Equal(repeat.ToString(System.Globalization.CultureInfo.InvariantCulture), Read(frame, splitter, 2));
            Assert.Equal(2, frame.GetFieldCount());
            Assert.Equal(repeat, counters.FieldListBufferReuses);
            if (repeat == 0) saved = frame.GetFieldValue(new(2));
        }
        Assert.Equal("0", saved.AsString());
        Assert.Equal(6, counters.FieldsMaterialized);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
        frame.AssignFieldValue(new(1), AwkValue.FromText("fresh"), AwkText.FromSourceLiteralString(" "));
        Assert.Equal("fresh", frame.RecordAwkText.AsString());
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReusedFieldListCancellationClearsUnpublishedDescriptors(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var armed = false;
        long searched = 0;
        void Check()
        {
            if (armed && counters.WhitespaceFieldSearchBytes > searched) throw new OperationCanceledException();
        }
        using var context = Frame(accounting, counters, " ", checkpoint: Check);
        var frame = context.Frame;
        var splitter = context.Splitter;
        frame.SetRecordText(AwkText.FromSourceLiteralString("old value"), splitter.CaptureRecordPlan());
        Assert.Equal("old", Read(frame, splitter, 1));
        var source = AwkText.FromSourceLiteralString("first second " + new string('x', 8192));
        frame.SetRecordText(source, splitter.CaptureRecordPlan());
        var committed = accounting.CurrentCommittedBytes;
        var materialized = counters.FieldsMaterialized;
        searched = counters.WhitespaceFieldSearchBytes;
        armed = true;
        Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount());
        armed = false;
        Assert.Equal(materialized, counters.FieldsMaterialized);
        Assert.Same(source, frame.RecordAwkText);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal("first", Read(frame, splitter, 1));
        Assert.Equal(materialized + 1, counters.FieldsMaterialized);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LargeFieldListsAreDiscardedBeforeSmallReuse(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, " ");
        var frame = context.Frame;
        var splitter = context.Splitter;
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", 1024))), splitter.CaptureRecordPlan());
        Assert.Equal(1024, frame.GetFieldCount());
        frame.SetRecordText(AwkText.FromSourceLiteralString("small row"), splitter.CaptureRecordPlan());
        Assert.Equal("small", Read(frame, splitter, 1));
        Assert.Equal(0, counters.FieldListBufferReuses);
        frame.SetRecordText(AwkText.FromSourceLiteralString("next row"), splitter.CaptureRecordPlan());
        Assert.Equal("next", Read(frame, splitter, 1));
        Assert.Equal(1, counters.FieldListBufferReuses);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "|")]
    [InlineData(true, "|")]
    public async Task PrefixReadsCompleteWithoutLosingCachedValuesAndRefundOnReset(bool governed, string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, separator);
        var frame = context.Frame;
        var splitter = context.Splitter;
        var words = new[] { "alpha", "beta", "café" }.Concat(Enumerable.Repeat("tail", 61)).ToArray();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var before = counters.FieldsMaterialized;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, words)), splitter.CaptureRecordPlan());
            var plan = frame.SplitPlan;
            Assert.Equal("alpha", Read(frame, splitter, 1));
            Assert.Equal(before + 1, counters.FieldsMaterialized);
            Assert.Equal(plan, frame.SplitPlan);
            var cached = frame.GetFieldValue(new(3));
            Assert.Equal("café", cached.AsString());
            Assert.Equal(before + 3, counters.FieldsMaterialized);
            Assert.Equal("beta", Read(frame, splitter, 2));
            Assert.Equal(64, frame.GetFieldCount());
            Assert.Equal(before + 64, counters.FieldsMaterialized);
            Assert.Equal(cached, frame.GetFieldValue(new(3)));
            Assert.Equal(default, frame.SplitPlan);
            frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"));
            Assert.Equal(string.Join("|", words.Select((s, i) => i == 1 ? "changed" : s)), frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
        Assert.Equal(2, counters.FieldSplits);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void EmptyAndTrailingFieldsAndHugeAbsentIndicesKeepOrdinarySemantics(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var context = Frame(AwkMemoryAccounting.Disabled, counters, separator);
        var frame = context.Frame;
        var splitter = context.Splitter;
        var word = new string('x', 160);
        var source = separator == " " ? " \t" + word + " \tbeta \n" : "|" + word + "||";
        frame.SetRecordText(AwkText.FromSourceLiteralString(source), splitter.CaptureRecordPlan());
        Assert.Equal(separator == " " ? word : "", Read(frame, splitter, 1));
        Assert.Equal(1, counters.FieldsMaterialized);
        Assert.Equal(separator == " " ? "beta" : word, Read(frame, splitter, 2));
        Assert.Equal("", Read(frame, splitter, 3));
        Assert.Equal("", Read(frame, splitter, 4));
        var count = separator == " " ? 2 : 4;
        Assert.Equal(count, frame.GetFieldCount());
        Assert.Equal(count, counters.FieldsMaterialized);
        Assert.Equal("", Read(frame, splitter, int.MaxValue));
        Assert.Equal(1, counters.FieldSplits);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledExtensionPreservesPublishedPrefixAndCanRetry(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var checks = 0;
        void Checkpoint()
        {
            if (!armed) return;
            if (++checks == 4) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        using var context = Frame(accounting, counters, " ", checkpoint: Checkpoint);
        var frame = context.Frame;
        var splitter = context.Splitter;
        var third = new string('z', 16_384);
        var record = AwkText.FromSourceLiteralString("first second " + third + " tail");
        frame.SetRecordText(record, splitter.CaptureRecordPlan());
        Assert.Equal("first", Read(frame, splitter, 1));
        var committed = accounting.CurrentCommittedBytes;
        var plan = frame.SplitPlan;
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() => Read(frame, splitter, 3));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        Assert.Equal(1, counters.FieldsMaterialized);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(plan, frame.SplitPlan);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal("first", Read(frame, splitter, 1));
        Assert.Equal(third, Read(frame, splitter, 3));
        Assert.Equal(3, counters.FieldsMaterialized);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(true)]
    public async Task RefusedExtensionLeavesThePrefixAndAccountingReusable(bool governed)
    {
        const long limit = 8 << 20;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, "|");
        var frame = context.Frame;
        var splitter = context.Splitter;
        frame.SetRecordText(AwkText.FromSourceLiteralString("first|second|third|" + new string('t', 160)), splitter.CaptureRecordPlan());
        Assert.Equal("first", Read(frame, splitter, 1));
        var committed = accounting.CurrentCommittedBytes;
        var plan = frame.SplitPlan;
        var pressure = limit - committed - 1;
        accounting.Reserve(pressure);
        var error = Assert.Throws<AwkRuntimeException>(() => Read(frame, splitter, 3));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(pressure, accounting.CurrentReservedBytes);
        Assert.Equal(plan, frame.SplitPlan);
        Assert.Equal(1, counters.FieldsMaterialized);
        Assert.Equal("first", Read(frame, splitter, 1));
        accounting.ReleaseReserved(pressure);
        Assert.Equal("third", Read(frame, splitter, 3));
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedInputSliceCopyPreservesTheRecordWithoutRetryingAdmission()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, " ");
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
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal("middle", Read(frame, splitter, 2));
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledInputSliceCopyCannotPublishItsDescriptor(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        using var context = Frame(accounting, counters, " ", () =>
        {
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        var splitter = context.Splitter;
        frame.SetRecordText(AwkText.FromSourceLiteralString("first middle café"), splitter.CaptureRecordPlan());
        Assert.Equal(3, frame.GetFieldCount());
        var record = frame.RecordAwkText;
        var committed = accounting.CurrentCommittedBytes;
        var separator = AwkText.FromSourceLiteralString("|");
        cancellation.Cancel();
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => frame.TryCopyInputField(new(1), new(2)));

        armed = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal("middle", Read(frame, splitter, 2));
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public async Task WarmFieldStorageNeedsNoAdmissionForRecordLength(string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, separator);
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
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(1, counters.FieldListBufferReuses);
        Assert.Same(record, frame.RecordAwkText);
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public async Task RefusedDescriptorGrowthLeavesRecordAndPublishedFieldsUnchanged(string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var context = Frame(accounting, counters, separator);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
        Assert.Equal(2, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("x", 128)));
        frame.SetRecordText(record);
        var committed = accounting.CurrentCommittedBytes;
        var publishedFields = counters.FieldsMaterialized;
        budget.SetAvailableBytes(0);

        var error = Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(publishedFields, counters.FieldsMaterialized);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public async Task CancellationDuringStagedGrowthRefundsUnpublishedStorage(string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using var cancellation = new CancellationTokenSource();
        var observeCancellation = true;
        using var context = Frame(accounting, counters, separator, () =>
        {
            if (observeCancellation) cancellation.Token.ThrowIfCancellationRequested();
        });
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
        Assert.Equal(2, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("x", 128)));
        frame.SetRecordText(record);
        var committed = accounting.CurrentCommittedBytes;
        var publishedFields = counters.FieldsMaterialized;
        budget.OnGrowth = _ =>
        {
            // Cancel with some construction storage already live, before publication.
            if (accounting.CurrentCommittedBytes > committed) cancellation.Cancel();
        };

        var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount());

        observeCancellation = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(publishedFields, counters.FieldsMaterialized);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void CanceledAppendRetainsCachedPrefixAndOwnedCapacity(bool governed, bool complete)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        long searched = 0;
        void Checkpoint()
        {
            if (armed && counters.WhitespaceFieldSearchBytes > searched) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        }
        using var context = Frame(memory.Accounting, counters, " ", Checkpoint);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("warm", 64))));
        Assert.Equal(64, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString("0007 short " + new string('z', 16_384) + " tail");
        frame.SetRecordText(record);
        Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        var saved = frame.GetFieldValue(new(1));
        var published = counters.FieldsMaterialized;
        var committed = memory.Accounting.CurrentCommittedBytes;
        searched = counters.WhitespaceFieldSearchBytes;
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (complete) frame.GetFieldCount();
            else Read(frame, context.Splitter, 3);
        });
        armed = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(published, counters.FieldsMaterialized);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal("0007", saved.AsString());
        Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        Assert.Equal("short", Read(frame, context.Splitter, 2));
        Assert.Equal(published + 1, counters.FieldsMaterialized);
        frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void AdmittedCapacityFundsPrefixExtensionWithoutAnotherReservation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = Frame(memory.Accounting, counters, " ");
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("warm", 64))));
        Assert.Equal(64, frame.GetFieldCount());
        frame.SetRecordText(AwkText.FromSourceLiteralString("0007 0008 0009 " + new string('t', 256)));
        var committed = memory.Accounting.CurrentCommittedBytes;
        var copies = counters.FieldDescriptorCopies;
        memory.Budget.SetAvailableBytes(0);
        Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        Assert.Equal(9, frame.GetFieldNumber(new(3), AwkNumberParsingMode.Decimal));
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(copies, counters.FieldDescriptorCopies);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(" ")] [InlineData("|")]
    public void AdmittedCapacityFundsCompletionWithoutAnotherReservation(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = Frame(memory.Accounting, counters, separator);
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
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(" ")] [InlineData("|")]
    public void RefusedCompletionGrowthKeepsThePreviouslyPublishedPrefix(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var context = Frame(memory.Accounting, counters, separator);
        var frame = context.Frame;
        frame.SetRecordText(AwkText.FromSourceLiteralString("old" + separator + "value"));
        Assert.Equal(2, frame.GetFieldCount());
        var record = AwkText.FromSourceLiteralString(string.Join(separator, Enumerable.Repeat("000007", 128)));
        frame.SetRecordText(record);
        Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        var saved = frame.GetFieldValue(new(1));
        var committed = memory.Accounting.CurrentCommittedBytes;
        var published = counters.FieldsMaterialized;
        memory.Budget.SetAvailableBytes(0);
        Assert.Throws<AwkRuntimeException>(() => frame.GetFieldCount());
        Assert.Equal(published, counters.FieldsMaterialized);
        Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal("000007", saved.AsString());
        Assert.Equal(7, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
        frame.ResetRecord();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static string Read(AwkFieldFrame frame, AwkFieldSplitter splitter, int index)
    {
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(index), out var bytes));
        return AwkUtf8Codec.Decode(bytes);
    }


    private static AwkFieldTestContext Frame(AwkMemoryAccounting accounting, AwkRuntimeCounters counters,
        string separator, Action? checkpoint = null) =>
        new(counters, accounting, checkpoint: checkpoint, separator: separator,
            resolveOutputFieldSeparator: static () => AwkText.FromSourceLiteralString("|"));
}
