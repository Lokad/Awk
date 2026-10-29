namespace Lokad.Awk.Tests;

public sealed class AwkIncrementalFieldTests
{
    [Fact]
    public async Task RefusedRecordReplacementPreservesTheOwnedSmallBuffer()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var (frame, splitter) = Frame(accounting, counters, " ");
        var old = AwkText.FromSourceLiteralString("left right");
        frame.SetRecordText(old, splitter.CaptureRecordPlan());
        Assert.Equal("left", Read(frame, splitter, 1));
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
        var (frame, splitter) = Frame(accounting, counters, separator);
        frame.SetRecordText(AwkText.FromSourceLiteralString("warm" + separator + "row"), splitter.CaptureRecordPlan());
        Assert.Equal("warm", Read(frame, splitter, 1));
        frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + "δ"), splitter.CaptureRecordPlan());
        splitter.AssignFieldSeparator(AwkText.FromSourceLiteralString(","));
        Assert.Equal("café", Read(frame, splitter, 1));
        Assert.Equal("δ", Read(frame, splitter, 2));
        var saved = frame.GetFieldValue(new(2), line => splitter.SplitRecordFields(line, frame.SplitPlan), AwkValue.FromText);
        frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"),
            line => splitter.SplitRecordFields(line, frame.SplitPlan));
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
        var (frame, splitter) = Frame(accounting, counters, separator);
        AwkValue saved = default;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            frame.SetRecordText(AwkText.FromSourceLiteralString("café" + separator + repeat), splitter.CaptureRecordPlan());
            Assert.Equal("café", Read(frame, splitter, 1));
            Assert.Equal(repeat.ToString(System.Globalization.CultureInfo.InvariantCulture), Read(frame, splitter, 2));
            Assert.Equal(2, frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
            Assert.Equal(repeat, counters.FieldListBufferReuses);
            if (repeat == 0) saved = frame.GetFieldValue(new(2), line => splitter.SplitRecordFields(line, frame.SplitPlan), AwkValue.FromText);
        }
        Assert.Equal("0", saved.AsString());
        Assert.Equal(6, counters.FieldsMaterialized);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, budget.Reserved);
        frame.AssignFieldValue(new(1), AwkValue.FromText("fresh"), AwkText.FromSourceLiteralString(" "),
            line => splitter.SplitRecordFields(line, frame.SplitPlan));
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
        var armed = false; var checks = 0;
        void Check() { if (armed && ++checks == 4) throw new OperationCanceledException(); }
        var (frame, splitter) = Frame(accounting, counters, " ", checkpoint: Check);
        frame.SetRecordText(AwkText.FromSourceLiteralString("old value"), splitter.CaptureRecordPlan());
        Assert.Equal("old", Read(frame, splitter, 1));
        var source = AwkText.FromSourceLiteralString("first second " + new string('x', 8192));
        frame.SetRecordText(source, splitter.CaptureRecordPlan());
        var committed = accounting.CurrentCommittedBytes;
        armed = true;
        Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
        armed = false;
        Assert.Equal(2, counters.FieldsMaterialized);
        Assert.Same(source, frame.RecordAwkText);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal("first", Read(frame, splitter, 1));
        Assert.Equal(3, counters.FieldsMaterialized);
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
        var (frame, splitter) = Frame(accounting, counters, " ");
        frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("x", 1024))), splitter.CaptureRecordPlan());
        Assert.Equal(1024, frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
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
        var (frame, splitter) = Frame(accounting, counters, separator);
        var words = new[] { "alpha", "beta", "café" }.Concat(Enumerable.Repeat("tail", 61)).ToArray();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var before = counters.FieldsMaterialized;
            frame.SetRecordText(AwkText.FromSourceLiteralString(string.Join(separator, words)), splitter.CaptureRecordPlan());
            var plan = frame.SplitPlan;
            Assert.Equal("alpha", Read(frame, splitter, 1));
            Assert.Equal(before + 1, counters.FieldsMaterialized);
            Assert.Equal(plan, frame.SplitPlan);
            var cached = frame.GetFieldValue(new(3), Split, AwkValue.FromText);
            Assert.Equal("café", cached.AsString());
            Assert.Equal(before + 3, counters.FieldsMaterialized);
            Assert.Equal("beta", Read(frame, splitter, 2));
            Assert.Equal(64, frame.GetFieldCount(Split));
            Assert.Equal(before + 64, counters.FieldsMaterialized);
            Assert.Equal(cached, frame.GetFieldValue(new(3), Split, AwkValue.FromText));
            Assert.Equal(default, frame.SplitPlan);
            frame.AssignFieldValue(new(2), AwkValue.FromText("changed"), AwkText.FromSourceLiteralString("|"), Split);
            Assert.Equal(string.Join("|", words.Select((s, i) => i == 1 ? "changed" : s)), frame.RecordAwkText.AsString());
            frame.ResetRecord();
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, budget.Reserved);
        }
        Assert.Equal(2, counters.FieldSplits);
        Assert.Equal(0, counters.StringDecodes);
        List<AwkFieldText> Split(AwkText line) => splitter.SplitRecordFields(line, frame.SplitPlan);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void EmptyAndTrailingFieldsAndHugeAbsentIndicesKeepOrdinarySemantics(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var (frame, splitter) = Frame(AwkMemoryAccounting.Disabled, counters, separator);
        var word = new string('x', 160);
        var source = separator == " " ? " \t" + word + " \tbeta \n" : "|" + word + "||";
        frame.SetRecordText(AwkText.FromSourceLiteralString(source), splitter.CaptureRecordPlan());
        Assert.Equal(separator == " " ? word : "", Read(frame, splitter, 1));
        Assert.Equal(1, counters.FieldsMaterialized);
        Assert.Equal(separator == " " ? "beta" : word, Read(frame, splitter, 2));
        Assert.Equal("", Read(frame, splitter, 3));
        Assert.Equal("", Read(frame, splitter, 4));
        var count = separator == " " ? 2 : 4;
        Assert.Equal(count, frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
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
        var (frame, splitter) = Frame(accounting, counters, " ", checkpoint: Checkpoint);
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
        var (frame, splitter) = Frame(accounting, counters, "|");
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
        var (frame, splitter) = Frame(accounting, counters, " ");
        frame.SetRecordText(AwkText.FromSourceLiteralString("first middle café"), splitter.CaptureRecordPlan());
        Assert.Equal(3, frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
        var record = frame.RecordAwkText;
        var committed = accounting.CurrentCommittedBytes;
        var separator = AwkText.FromSourceLiteralString("|");
        budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() => frame.TryCopyInputField(new(1), new(2),
            () => separator, line => splitter.SplitRecordFields(line, frame.SplitPlan)));

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
        var (frame, splitter) = Frame(accounting, counters, " ", () =>
        {
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        frame.SetRecordText(AwkText.FromSourceLiteralString("first middle café"), splitter.CaptureRecordPlan());
        Assert.Equal(3, frame.GetFieldCount(line => splitter.SplitRecordFields(line, frame.SplitPlan)));
        var record = frame.RecordAwkText;
        var committed = accounting.CurrentCommittedBytes;
        var separator = AwkText.FromSourceLiteralString("|");
        cancellation.Cancel();
        armed = true;

        var error = Assert.Throws<OperationCanceledException>(() => frame.TryCopyInputField(new(1), new(2),
            () => separator, line => splitter.SplitRecordFields(line, frame.SplitPlan)));

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

    private static string Read(AwkFieldFrame frame, AwkFieldSplitter splitter, int index)
    {
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(index), line => splitter.SplitRecordFields(line, frame.SplitPlan), out var bytes));
        return AwkUtf8Codec.Decode(bytes);
    }


    private static (AwkFieldFrame, AwkFieldSplitter) Frame(AwkMemoryAccounting accounting, AwkRuntimeCounters counters, string separator, Action? checkpoint = null)
    {
        Action check = checkpoint ?? (() => { });
        var monitor = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        var regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, check, counters, monitor, accounting, regexEngine: AwkUtf8RegexEngine.Instance);
        var splitter = new AwkFieldSplitter(AwkFieldSeparatorSpec.FromText(separator), regex.Create, regex.CreateLiteral, regex.EnumerateMatchSpans, () => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, check, accounting, counters);
        var frame = new AwkFieldFrame(counters, accounting, value => value.ByteText, check, splitter);
        return (frame, splitter);
    }
}
