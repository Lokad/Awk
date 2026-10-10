namespace Lokad.Awk.Tests;

public sealed class AwkFieldCancellationTests
{
    private const int LongScan = 16_384;

    [Theory]
    [InlineData("digits")]
    [InlineData("blanks")]
    [InlineData("strtoul-whitespace")]
    [InlineData("remainder")]
    [InlineData("punctuation")]
    [InlineData("many-widths")]
    public void FieldWidthParsingObservesCancellationWithinOwnedScans(string scenario)
    {
        var source = scenario switch
        {
            "digits" => new string('0', LongScan) + "1",
            "blanks" => new string(' ', LongScan) + "1",
            "strtoul-whitespace" => new string('\r', LongScan) + "1",
            "remainder" => "*" + new string(' ', LongScan),
            "punctuation" => new string('0', 4_095) + "1:+1",
            "many-widths" => string.Concat(Enumerable.Repeat("1 ", LongScan)),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var bytes = AwkUtf8Codec.Encode(source).ToArray();
        using var cancellation = new CancellationTokenSource();
        var error = Assert.Throws<OperationCanceledException>(() => AwkFieldWidthsParser.Parse(bytes, null, CancelOnCheck(cancellation, 4)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        var expected = scenario == "many-widths" ? LongScan : 1;
        var widths = new List<AwkFieldWidth>();
        Assert.Equal(expected, AwkFieldWidthsParser.Parse(bytes, widths));
        Assert.Equal(expected, widths.Count);
        Assert.Equal(scenario == "punctuation" ? 1 : 0, widths[0].Skip.Value);
        Assert.Equal(scenario == "remainder" ? int.MaxValue : 1, widths[0].Width.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledFieldWidthPreparationLeavesThePreviousPlanUntouched(bool estimate)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 6);
        Action checkpoint = () => { if (armed) cancel(); };
        var splitter = Splitter(checkpoint, counters, governor);
        splitter.AssignFieldWidths(splitter.PrepareFieldWidths(AwkText.FromString("2 3")));
        var previous = splitter.CaptureRecordPlan();
        var committed = governor.CurrentCommittedBytes;
        var pending = AwkText.FromString(new string('0', LongScan) + "1");
        armed = true;
        Assert.Throws<OperationCanceledException>(() =>
        {
            if (estimate) _ = splitter.EstimateFieldWidthsMemory(pending);
            else _ = splitter.PrepareFieldWidths(pending);
        });
        Assert.Equal(previous, splitter.CaptureRecordPlan());
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retry = splitter.PrepareFieldWidths(pending);
        Assert.Single(retry);
        Assert.Equal(1, retry[0].Width.Value);
    }

    [Theory]
    [InlineData("whitespace-word", false)]
    [InlineData("whitespace-word", true)]
    [InlineData("whitespace-leading", false)]
    [InlineData("whitespace-leading", true)]
    [InlineData("whitespace-trailing", false)]
    [InlineData("whitespace-trailing", true)]
    [InlineData("literal", false)]
    [InlineData("literal", true)]
    [InlineData("characters", false)]
    [InlineData("characters", true)]
    public void SplittingChecksLongWhitespaceLiteralAndCharacterWork(string scenario, bool captures)
    {
        var source = scenario switch
        {
            "whitespace-word" => new string('a', LongScan) + " b",
            "whitespace-leading" => new string(' ', LongScan) + "a b",
            "whitespace-trailing" => "a b" + new string(' ', LongScan),
            "literal" => new string('a', LongScan) + ",b",
            "characters" => new string('a', LongScan),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var separator = AwkFieldSeparatorSpec.FromText(scenario == "literal" ? "," : scenario == "characters" ? "" : " ");
        var text = AwkText.FromString(source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var check = scenario switch
        {
            "whitespace-word" => 5,
            "whitespace-leading" => 6,
            "whitespace-trailing" => captures ? 15 : 19,
            "literal" => captures ? 5 : 4,
            "characters" => 1_000,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var cancel = CancelOnCheck(cancellation, check);
        Action checkpoint = () => { if (armed) cancel(); };
        var splitter = Splitter(checkpoint, counters, governor);
        splitter.ValidateFieldSeparator(separator.ByteText);
        var mode = captures ? AwkSeparatorCaptureMode.Capture : AwkSeparatorCaptureMode.Omit;
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() => splitter.SplitFieldTextsBySeparator(text, separator, mode, counters));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        armed = false;
        var retry = splitter.SplitFieldTextsBySeparator(text, separator, mode, counters);
        Assert.Equal(scenario == "characters" ? LongScan : 2, retry.Fields.Count);
        Assert.Equal(scenario == "characters" ? "a" : scenario == "literal" || scenario == "whitespace-word" ? new string('a', LongScan) : "a", retry.Fields[0].AsString());
        Assert.Equal(scenario == "characters" ? "a" : "b", retry.Fields[^1].AsString());
    }

    [Theory]
    [InlineData("widths")]
    [InlineData("separator")]
    [InlineData("pattern")]
    [InlineData("record")]
    public void SplitterCallersCheckBeforeHiddenMaterialization(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        var source = AwkText.FromString(new string(operation == "widths" ? '0' : 'a', LongScan))
            .Concat(AwkText.FromString("1"), counters, governor);
        using var cancellation = new CancellationTokenSource();
        var splitter = Splitter(cancellation.Token.ThrowIfCancellationRequested, counters, governor);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "widths": _ = splitter.PrepareFieldWidths(source); break;
                case "separator": splitter.ValidateFieldSeparator(source); break;
                case "pattern": splitter.ValidateFieldPattern(source); break;
                case "record": _ = splitter.SplitRecordFields(source); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(new string(operation == "widths" ? '0' : 'a', LongScan) + "1", source.AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FieldWidthRuneTraversalAndParagraphScanningObserveCancellation(bool paragraph)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 4);
        Action checkpoint = () => { if (armed) cancel(); };
        var splitter = Splitter(checkpoint, counters, governor);
        var source = paragraph ? new string('a', LongScan) + "\nb" : new string('a', 4_095) + string.Concat(Enumerable.Repeat("\ud83d\ude00", LongScan));
        var text = AwkText.FromString(source);
        var plan = paragraph
            ? new AwkRecordSplitPlan(AwkFieldSplittingMode.FieldSeparator, AwkFieldSeparatorSpec.FromText(","), AwkText.Empty, [], AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Paragraph)
            : new AwkRecordSplitPlan(AwkFieldSplittingMode.FieldWidths, default, AwkText.Empty, [new(new(0), new(int.MaxValue))], AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
        if (paragraph) splitter.ValidateFieldSeparator(plan.Separator.ByteText);
        armed = true;
        Assert.Throws<OperationCanceledException>(() => splitter.SplitRecordFields(text, plan));
        armed = false;
        var retry = splitter.SplitRecordFields(text, plan);
        Assert.Equal(paragraph ? 2 : 1, retry.Count);
        Assert.Equal(paragraph ? new string('a', LongScan) : source, retry[0].AsString(text, counters));
    }

    [Fact]
    public void CanceledSplitPayloadSummationPreservesLazyFieldsMemoryAndPlan()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 2);
        var frame = Frame(counters, governor, () => { if (armed) cancel(); });
        var record = AwkText.FromString(new string('a', LongScan));
        var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.FieldSeparator, AwkFieldSeparatorSpec.FromText(","), AwkText.Empty, [], AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
        frame.SetRecordText(record, plan);
        var committed = governor.CurrentCommittedBytes;
        var fields = Enumerable.Range(0, LongScan).Select(index => AwkFieldText.FromRecordSlice(index, 1)).ToList();
        armed = true;
        Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount(_ => fields));
        Assert.Same(record, frame.RecordAwkText);
        Assert.Equal(plan, frame.SplitPlan);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, counters.FieldsMaterialized);
        armed = false;
        Assert.Equal(LongScan, frame.GetFieldCount(_ => fields));
        Assert.Equal(LongScan, counters.FieldsMaterialized);
        Assert.Equal(default, frame.SplitPlan);
    }

    [Fact]
    public void CanceledRecordCopyPreservesDirtyFieldsAndReleasesTransientMemory()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 9);
        var frame = Frame(counters, governor, () => { if (armed) cancel(); });
        frame.SetRecordText(AwkText.FromString("old"));
        var pending = AwkValue.FromDeferredText(AwkText.FromString(new string('a', LongScan)), AwkNumberParsingMode.Decimal);
        frame.AssignFieldValue(new(1), pending, AwkText.FromString("|"), _ => [AwkFieldText.FromRecordSlice(0, 3)]);
        var committed = governor.CurrentCommittedBytes;
        armed = true;
        Assert.Throws<OperationCanceledException>(() => frame.RecordAwkText);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        Assert.Equal(new string('a', LongScan), frame.GetFieldValue(new(1), _ => throw new InvalidOperationException("Already split."), AwkValue.FromText).AsString());
        Assert.Equal(new string('a', LongScan), frame.RecordAwkText.AsString());
    }

    [Fact]
    public void InterruptedFieldSliceCopyDoesNotPopulateTheValueCache()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = Governor(counters);
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 4);
        var frame = Frame(counters, governor, () => { if (armed) cancel(); });
        frame.SetRecordText(AwkText.FromString(new string('a', LongScan) + ",b"));
        List<AwkFieldText> Split(AwkText _) => [AwkFieldText.FromRecordSlice(0, LongScan), AwkFieldText.FromRecordSlice(LongScan + 1, 1)];
        Assert.Equal(2, frame.GetFieldCount(Split));
        var committed = governor.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        armed = true;
        Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1), Split, AwkValue.FromText));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        Assert.Equal(new string('a', LongScan), frame.GetFieldValue(new(1), Split, AwkValue.FromText).AsString());
        Assert.Equal(allocations + 1, counters.AwkTextAllocations);
        _ = frame.GetFieldValue(new(1), Split, AwkValue.FromText);
        Assert.Equal(allocations + 1, counters.AwkTextAllocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FieldCheckpointsPreserveLogicalFuelAndByteNativeCounters(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var text = AwkText.FromString(new string('a', LongScan) + ",b");
        var chunks = AwkExecutionGovernor.ByteWorkSteps(text.ByteLength);
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks);
        var resources = new AwkRuntimeResourceMonitor(limits, counters);
        var splitter = Splitter(() => resources.ThrowIfCancellationRequested(CancellationToken.None), counters, AwkMemoryGovernor.Disabled, resources);
        var fields = splitter.SplitFieldTextsBySeparator(text, AwkFieldSeparatorSpec.FromText(","), AwkSeparatorCaptureMode.Capture, counters);
        Assert.Equal(2, fields.Fields.Count);
        Assert.Equal(observed ? chunks : 0, counters.ExecutionSteps);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        if (observed) Assert.True(counters.CancellationChecks > 4);
    }

    private static AwkFieldFrame Frame(AwkRuntimeCounters counters, AwkMemoryGovernor governor, Action checkpoint) =>
        new(counters, governor, LongScan, _ => { }, value => value.ByteText, _ => { }, checkpoint);

    private static AwkMemoryGovernor Governor(AwkRuntimeCounters counters) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20), counters);

    private static AwkFieldSplitter Splitter(Action checkpoint, AwkRuntimeCounters counters, AwkMemoryGovernor governor, AwkRuntimeResourceMonitor? resources = null)
    {
        var monitor = resources ?? new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        var regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint, counters, AwkRegexEngineKind.DotNetCompatibility, monitor, governor);
        return new(AwkFieldSeparatorSpec.Default, regex.Create, regex.CreateLiteral, regex.EnumerateMatchSpans, () => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint, governor, counters, monitor.RecordByteWork);
    }

    private static Action CancelOnCheck(CancellationTokenSource cancellation, int check)
    {
        var calls = 0;
        return () =>
        {
            if (++calls == check) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
    }
}
