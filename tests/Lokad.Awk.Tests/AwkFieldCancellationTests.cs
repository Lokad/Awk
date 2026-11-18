namespace Lokad.Awk.Tests;

public sealed class AwkFieldCancellationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void IncrementalSplitChecksCancellationAfterRetiringSeparator(bool reuseBuffer, bool existingPrefix)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: ":");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            if (reuseBuffer)
            {
                frame.SetRecordText(AwkText.FromSourceLiteralString("1:2:3"));
                Assert.Equal(3, frame.GetFieldCount());
            }
            frame.SetRecordText(AwkText.FromSourceLiteralString("4:5"));
            if (existingPrefix) Assert.Equal(4, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var refunded = false;
            var publishedAtRefund = false;
            var countAtRefund = 0d;
            memory.Budget.OnRefund = () =>
            {
                memory.Budget.OnRefund = null;
                refunded = true;
                publishedAtRefund = frame.TryGetFieldCountNumber(out countAtRefund);
                cancellation.Cancel();
            };

            var error = Assert.Throws<OperationCanceledException>(() =>
                frame.GetFieldNumber(new(2), AwkNumberParsingMode.Decimal));
            Assert.True(refunded);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(publishedAtRefund);
            Assert.Equal(2, countAtRefund);
            Assert.True(frame.TryGetFieldCountNumber(out var count));
            Assert.Equal(2, count);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Fact]
    public void FullSplitChecksCancellationAfterRetiringPrefixStorage()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: ":");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("4:5:6"));
            Assert.Equal(4, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var refunded = false;
            var publishedAtRefund = false;
            var countAtRefund = 0d;
            memory.Budget.OnRefund = () =>
            {
                memory.Budget.OnRefund = null;
                refunded = true;
                publishedAtRefund = frame.TryGetFieldCountNumber(out countAtRefund);
                cancellation.Cancel();
            };

            var error = Assert.Throws<OperationCanceledException>(() => frame.GetFieldCountValue());
            Assert.True(refunded);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.True(publishedAtRefund);
            Assert.Equal(3, countAtRefund);
            Assert.True(frame.TryGetFieldCountNumber(out var count));
            Assert.Equal(3, count);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    private const int LongScan = 16_384;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PrefixGrowthChecksCancellationBeforeAndAfterAdmission(bool existingPrefix, bool cancelAtAdmission)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: ":");
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("1:2:3:4:5"));
            if (existingPrefix) Assert.Equal(1, frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            var committed = memory.Accounting.CurrentCommittedBytes;
            var fields = counters[AwkCounter.FieldsPrepared];
            var admitted = false;
            memory.Budget.OnGrowth = _ =>
            {
                admitted = true;
                if (cancelAtAdmission) cancellation.Cancel();
            };
            if (!cancelAtAdmission) cancellation.Cancel();

            var error = Assert.Throws<OperationCanceledException>(() =>
                frame.GetFieldNumber(new(5), AwkNumberParsingMode.Decimal));
            memory.Budget.OnGrowth = null;
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(cancelAtAdmission, admitted);
            Assert.Equal(fields, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(committed, memory.Budget.Reserved);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, separator: ":");
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("1:2:3:4:5"));
            Assert.Equal("5", frame.GetFieldValue(new(5)).AsString());
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, " ", false)]
    [InlineData(true, " ", false)]
    [InlineData(false, ":", false)]
    [InlineData(true, ":", false)]
    [InlineData(false, " ", true)]
    [InlineData(true, " ", true)]
    [InlineData(false, ":", true)]
    [InlineData(true, ":", true)]
    public void CanceledWarmPrefixDoesNotPublishFieldsOrNumericCache(bool governed, string separator, bool longField)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory, separator: separator);
            memoryBudgetBaseline = context.BaselineBudget;
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString("1" + separator + "2"));
            Assert.Equal(2, frame.GetFieldCount());
            var spelling = longField ? "7" + new string('a', 3 * AwkRuntimeLimits.CancellationCheckBytes + 17) : "007";
            var source = AwkText.FromSourceLiteralString(spelling + separator + "tail");
            frame.SetRecordText(source);
            var plan = frame.SplitPlan;
            var committed = memory.Accounting.CurrentCommittedBytes;
            var fields = counters[AwkCounter.FieldsPrepared];
            cancellation.Cancel();

            var error = Assert.Throws<OperationCanceledException>(() =>
                frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Equal(fields, counters[AwkCounter.FieldsPrepared]);

            Assert.False(frame.TryGetFieldCountNumber(out _));
            Assert.Equal(plan, frame.SplitPlan);
            Assert.True(source.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, separator: separator);
            frame.ResetRecord();
            frame = retry.Frame;
            frame.SetRecordText(source);
            Assert.Equal(spelling, frame.GetFieldValue(new(1)).AsString());
            Assert.Equal("tail", frame.GetFieldValue(new(2)).AsString());
            frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void WhitespaceSearchCancellationPreservesPublishedFields(bool governed, bool prefix)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(8 << 20, enabled: governed).InitializeRuntime(counters, cancellation.Token);
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var memoryBudgetBaseline = memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(memory.Budget.Limit);
            using var context = new AwkFieldTestContext(memory);
            memoryBudgetBaseline = context.BaselineBudget;
            var source = AwkText.FromSourceLiteralString(new string('a', 3 * AwkRuntimeLimits.CancellationCheckBytes + 17) + " b");
            context.Frame.SetRecordText(source);
            var committed = memory.Accounting.CurrentCommittedBytes;
            var published = counters[AwkCounter.FieldsPrepared];
            cancellation.Cancel();
            var error = Assert.Throws<OperationCanceledException>(() =>
            {
                if (prefix) context.Frame.GetFieldNumber(new(1), AwkNumberParsingMode.Decimal);
                else context.Frame.GetFieldCount();
            });
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: " ", outputSeparator: " ");
            var retryFrame = retry.Frame;
            retryFrame.SetRecordText(source);

            Assert.Equal(cancellation.Token, error.CancellationToken);

            Assert.Equal(published, counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(committed, memory.Accounting.CurrentCommittedBytes);
            Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
            Assert.True(source.HasSameStorage(context.Frame.RecordAwkText));
            Assert.Equal(2, retryFrame.GetFieldCount());
            Assert.Equal("b", retryFrame.GetFieldValue(new(2)).AsString());
            context.Frame.ResetRecord();
            Assert.Equal(memoryBudgetBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData("record")]
    [InlineData("split")]
    [InlineData("captures")]
    public void WhitespaceFieldSearchesCoverLongRunsWithinExistingChunks(string mode)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var splitter = Splitter(() => { }, counters, AwkMemoryAccounting.Disabled);
        var ascii = new string('a', 3 * AwkRuntimeLimits.CancellationCheckBytes + 17);
        var unicode = string.Concat(Enumerable.Repeat("é长", 1_200));
        var text = AwkText.FromString(" \t" + ascii + "\t" + unicode + " \n");
        if (mode == "record")
        {
            using var context = new AwkFieldTestContext(counters);
            context.Frame.SetRecordText(text);
            Assert.Equal(2, context.Frame.GetFieldCount());
            Assert.True(context.Frame.TryGetUnmaterializedFieldBytes(new(1), out var first));
            Assert.True(context.Frame.TryGetUnmaterializedFieldBytes(new(2), out var second));
            Assert.True(first.SequenceEqual(AwkUtf8Codec.Encode(ascii).Span));
            Assert.True(second.SequenceEqual(AwkUtf8Codec.Encode(unicode).Span));
        }
        else
        {
            var result = splitter.SplitFieldTextsBySeparator(text, AwkFieldSeparatorSpec.Default,
                mode == "captures" ? AwkSeparatorCaptureMode.Capture : AwkSeparatorCaptureMode.Omit);
            Assert.Equal(2, result.Fields.Count);
            Assert.Equal(ascii, result.Fields[0].AsString());
            Assert.Equal(unicode, result.Fields[1].AsString());
            Assert.Equal(mode == "captures" ? 3 : 0, result.Separators.Count);
        }
        // Searches are bounded by the existing 4KiB checkpoints, independent of rune width.

        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

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
        using var memoryForGovernor = Governor();
        var governor = memoryForGovernor.Accounting;
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
        // Keep enough scan regions to cancel within long work independently
        // of how many constant-time setup/accessor checks precede the scanner.
        var scanLength = scenario == "characters" ? LongScan : 8 * LongScan;
        var source = scenario switch
        {
            "whitespace-word" => new string('a', scanLength) + " b",
            "whitespace-leading" => new string(' ', scanLength) + "a b",
            "whitespace-trailing" => "a b" + new string(' ', scanLength),
            "literal" => new string('a', scanLength) + ",b",
            "characters" => new string('a', scanLength),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var separator = AwkFieldSeparatorSpec.FromText(scenario == "literal" ? "," : scenario == "characters" ? "" : " ");
        var text = AwkText.FromString(source);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor();
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        var cancel = CancelOnCheck(cancellation, 4 * LongScan / AwkRuntimeLimits.CancellationCheckBytes);
        Action checkpoint = () => { if (armed) cancel(); };
        var splitter = Splitter(checkpoint, counters, governor);
        splitter.ValidateFieldSeparator(separator.ByteText);
        var mode = captures ? AwkSeparatorCaptureMode.Capture : AwkSeparatorCaptureMode.Omit;
        armed = true;
        var error = Assert.Throws<OperationCanceledException>(() => splitter.SplitFieldTextsBySeparator(text, separator, mode));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        armed = false;
        var retry = splitter.SplitFieldTextsBySeparator(text, separator, mode);
        Assert.Equal(scenario == "characters" ? scanLength : 2, retry.Fields.Count);
        Assert.Equal(scenario == "characters" ? "a" : scenario == "literal" || scenario == "whitespace-word" ? new string('a', scanLength) : "a", retry.Fields[0].AsString());
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
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            var source = AwkText.Concat([AwkText.FromString(new string(operation == "widths" ? '0' : 'a', LongScan)), AwkText.FromString("1")], counters, governor);
            var splitter = Splitter(cancellation.Token.ThrowIfCancellationRequested, counters, governor);
            using var recordContext = new AwkFieldTestContext(memoryForGovernor);
            recordContext.Frame.SetRecordText(source);
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() =>
            {
                switch (operation)
                {
                    case "widths": _ = splitter.PrepareFieldWidths(source); break;
                    case "separator": splitter.ValidateFieldSeparator(source); break;
                    case "pattern": splitter.ValidateFieldPattern(source); break;
                    case "record": _ = recordContext.Frame.GetFieldCount(); break;
                    default: throw new ArgumentOutOfRangeException(nameof(operation));
                }
            });
            Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
            Assert.Equal(new string(operation == "widths" ? '0' : 'a', LongScan) + "1", source.AsString());
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FieldWidthRuneTraversalAndParagraphScanningObserveCancellation(bool paragraph)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
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
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: ",");
            context.Frame.SetRecordText(text, plan);
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => context.Frame.GetFieldCount());
            armed = false;
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, separator: ",");
            context.Frame.ResetRecord();
            retry.Frame.SetRecordText(text, plan);
            Assert.Equal(paragraph ? 2 : 1, retry.Frame.GetFieldCount());
            Assert.Equal(paragraph ? new string('a', LongScan) : source, retry.Frame.GetFieldValue(new(1)).AsString());
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Fact]
    public void CanceledFieldPublicationPreservesLazyFieldsMemoryAndPlan()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: ",");
            var frame = context.Frame;
            var record = AwkText.FromString(new string('a', LongScan));
            var plan = new AwkRecordSplitPlan(AwkFieldSplittingMode.FieldSeparator, AwkFieldSeparatorSpec.FromText(","), AwkText.Empty, [], AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);
            frame.SetRecordText(record, plan);
            var committed = governor.CurrentCommittedBytes;
            memoryForGovernor.Budget.OnGrowth = _ => cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => frame.GetFieldCount());
            Assert.True(record.HasSameStorage(frame.RecordAwkText));
            Assert.Equal(plan, frame.SplitPlan);
            Assert.Equal(committed, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(0, counters[AwkCounter.FieldsPrepared]);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: ",", outputSeparator: " ");
            frame = retry.Frame;
            frame.SetRecordText(record, plan);

            Assert.Equal(1, frame.GetFieldCount());
            Assert.Equal(1, retry.State.Counters[AwkCounter.FieldsPrepared]);
            Assert.Equal(default, frame.SplitPlan);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Fact]
    public void CanceledRecordCopyPreservesDirtyFieldsAndReleasesTransientMemory()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: ",");
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromString("old"));
            var pending = AwkValue.FromDeferredText(AwkText.FromString(new string('a', LongScan)), AwkNumberParsingMode.Decimal);
            frame.AssignFieldValue(new(1), pending, AwkText.FromString("|"));
            var committed = governor.CurrentCommittedBytes;
            memoryForGovernor.Budget.OnGrowth = _ => cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => frame.RecordAwkText);
            Assert.Equal(committed, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(new string('a', LongScan), frame.GetFieldValue(new(1)).AsString());
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.Disabled, separator: ",");
            retry.Frame.SetRecordText(AwkText.FromString("old"));
            retry.Frame.AssignFieldValue(new(1), pending, AwkText.FromString("|"));
            Assert.Equal(new string('a', LongScan), retry.Frame.RecordAwkText.AsString());
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Fact]
    public void InterruptedFieldSliceCopyDoesNotPopulateTheValueCache()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = Governor().InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            memoryForGovernor.Budget.SetAvailableBytes(memoryForGovernor.Budget.Limit);
            var governor = memoryForGovernor.Accounting;
            using var context = new AwkFieldTestContext(memoryForGovernor, separator: ",");
            var frame = context.Frame;
            frame.SetRecordText(AwkText.FromString(new string('a', LongScan) + ",b"));
            Assert.Equal(2, frame.GetFieldCount());
            var committed = governor.CurrentCommittedBytes;
            var allocations = counters[AwkCounter.TextAllocations];
            memoryForGovernor.Budget.OnGrowth = _ => cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => frame.GetFieldValue(new(1)));
            Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
            Assert.Equal(committed, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            using var retry = new AwkFieldTestContext(AwkRuntimeCounters.CreateEnabled(), separator: ",", outputSeparator: " ");
            frame = retry.Frame;
            frame.SetRecordText(AwkText.FromSourceLiteralString(new string('a', LongScan) + ",tail"));

            Assert.Equal(new string('a', LongScan), frame.GetFieldValue(new(1)).AsString());
            Assert.Equal(1, retry.State.Counters[AwkCounter.TextAllocations]);
            _ = frame.GetFieldValue(new(1));
            Assert.Equal(1, retry.State.Counters[AwkCounter.TextAllocations]);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    private static CooperativeTestMemory Governor() =>
        new CooperativeTestMemory(8 << 20);

    private static AwkFieldSplitter Splitter(Action checkpoint, AwkRuntimeCounters counters, AwkMemoryAccounting governor, AwkRuntimeResourceMonitor? resources = null)
    {
        var monitor = resources ?? new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters);
        var regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint, counters, monitor, governor, regexEngine: AwkUtf8RegexEngine.Instance);
        return new(AwkFieldSeparatorSpec.Default, regex.Create, regex.CreateLiteral, regex.EnumerateMatchSpans, () => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint, governor, counters);
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
