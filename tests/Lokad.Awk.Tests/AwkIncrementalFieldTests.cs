namespace Lokad.Awk.Tests;

public sealed class AwkIncrementalFieldTests
{
    [Theory]
    [InlineData(0, " ")]
    [InlineData(1, " ")]
    [InlineData(2, " ")]
    [InlineData(0, "|")]
    [InlineData(1, "|")]
    [InlineData(2, "|")]
    public async Task PrefixReadsCompleteWithoutLosingCachedValuesAndRefundOnReset(int mode, string separator)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode == 2 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = Accounting(mode, owner, counters);
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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CanceledExtensionPreservesPublishedPrefixAndCanRetry(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode == 2 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = Accounting(mode, owner, counters);
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
    [InlineData(1)]
    [InlineData(2)]
    public async Task RefusedExtensionLeavesThePrefixAndAccountingReusable(int mode)
    {
        const long limit = 8 << 20;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(mode == 2 ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = Accounting(mode, owner, counters);
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

    [Theory]
    [InlineData(" ")]
    [InlineData("|")]
    public void FieldQuotaStillRejectsTheFirstReadBeforePublishingAPrefix(string separator)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20), counters);
        var (frame, splitter) = Frame(accounting, counters, separator, maxFields: 2);
        var record = AwkText.FromSourceLiteralString(string.Join(separator, new[] { "one", "two", new string('x', 160) }));
        frame.SetRecordText(record, splitter.CaptureRecordPlan());
        var committed = accounting.CurrentCommittedBytes;
        var plan = frame.SplitPlan;
        var error = Assert.Throws<AwkRuntimeException>(() => Read(frame, splitter, 1));
        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, counters.FieldsMaterialized);
        Assert.Equal(plan, frame.SplitPlan);
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(committed, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        frame.ResetRecord();
        Assert.Equal(0, accounting.CurrentCommittedBytes);
    }

    private static string Read(AwkFieldFrame frame, AwkFieldSplitter splitter, int index)
    {
        Assert.True(frame.TryGetUnmaterializedFieldBytes(new(index), line => splitter.SplitRecordFields(line, frame.SplitPlan), out var bytes));
        return AwkUtf8Codec.Decode(bytes);
    }

    private static AwkMemoryAccounting Accounting(int mode, AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters) =>
        AwkMemoryAccounting.Create(mode == 1 ? AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20)
            : AwkRuntimeLimits.CreateDefault(), counters, owner);

    private static (AwkFieldFrame, AwkFieldSplitter) Frame(AwkMemoryAccounting accounting, AwkRuntimeCounters counters,
        string separator, int maxFields = 1 << 20, Action? checkpoint = null)
    {
        Action check = checkpoint ?? (() => { });
        var monitor = new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        var regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            check, counters, AwkRegexEngineKind.DotNetCompatibility, monitor, accounting);
        var splitter = new AwkFieldSplitter(AwkFieldSeparatorSpec.FromText(separator), regex.Create, regex.CreateLiteral,
            regex.EnumerateMatchSpans, () => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            check, accounting, counters, monitor.RecordByteWork);
        var frame = new AwkFieldFrame(counters, accounting, maxFields, _ => { }, value => value.ByteText,
            monitor.RecordByteWork, check, splitter);
        return (frame, splitter);
    }
}
