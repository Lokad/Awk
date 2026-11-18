namespace Lokad.Awk.Tests;

public sealed class AwkPrintfCancellationTests
{
    [Theory]
    [InlineData(false, "x")]
    [InlineData(true, "x")]
    [InlineData(false, "é")]
    [InlineData(true, "é")]
    [InlineData(false, "😀")]
    [InlineData(true, "😀")]
    public void LongLiteralRunsCancelDuringFormattingAndRefund(bool governed, string scalar)
    {
        var literal = "a" + string.Concat(Enumerable.Repeat(scalar, 12_000));
        var format = literal + "%d";
        using var cancellation = new CancellationTokenSource();
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var budget = memory.Budget;
        using (memory)
        {
            using var builder = new AwkUtf8Builder(memory.Accounting, capacity: 0);
            budget.OnGrowth = _ => { if (builder.Length > 0) cancellation.Cancel(); };
            // Ungoverned work has no admission callbacks. The argument callback cancels
            // after its long literal prefix, before the pending numeric append.
            var arguments = new TestArguments("x", 7, onNumber: cancellation.Cancel, numeric: true);
            var error = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatToBuilder(
                builder, format, arguments, PrintfMissingArgumentPolicy.Throw, memory.State));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            var literalBytes = AwkUtf8Codec.GetEncodedByteCount(literal);
            if (governed) Assert.InRange(builder.Length, 1, literalBytes - 1);
            else Assert.Equal(literalBytes, builder.Length);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("flags")]
    [InlineData("width")]
    [InlineData("precision")]
    [InlineData("control")]
    public void LongDirectiveScansObserveCancellationBeforeCompleting(string kind)
    {
        string Format(int count) => kind switch
        {
            "flags" => "%" + new string('+', count) + "s",
            "width" => "%" + new string('0', count) + "1$s",
            "precision" => "%." + new string('0', count) + "1s",
            _ => "%*" + new string('0', count) + "1$s"
        };
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var governor = memoryForGovernor.Accounting;
            PrintfDirective Read(string format, Action checkpoint)
            {
                var cursor = new PrintfArgumentCursor<TestArguments>(new TestArguments("x", 1),
                    PrintfMissingArgumentPolicy.Throw, format, memoryForGovernor.State);
                return PrintfDirectiveParser.Read(format, 0, AwkCompatibilityMode.GnuDefault, ref cursor, checkpoint);
            }
            var shortCalls = 0;
            var shortResult = Read(Format(1), () => shortCalls++);
            var longCalls = 0;
            var longFormat = Format(24_000);
            var result = Read(longFormat, () => longCalls++);
            Assert.Equal(shortResult.Conversion, result.Conversion);
            Assert.Equal(longFormat.Length - 1, result.EndIndex);
            Assert.True(longCalls > shortCalls);
            using var cancellation = new CancellationTokenSource();
            var calls = 0;
            void Checkpoint()
            {
                if (++calls == shortCalls)
                    cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }
            var failure = Assert.Throws<OperationCanceledException>(() => Read(longFormat, Checkpoint));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.True(calls > 0);
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("ascii", "width")]
    [InlineData("ascii", "precision")]
    [InlineData("bmp", "width")]
    [InlineData("bmp", "precision")]
    [InlineData("supplementary", "width")]
    [InlineData("supplementary", "precision")]
    [InlineData("ascii", "plain")]
    [InlineData("bmp", "plain")]
    [InlineData("supplementary", "plain")]
    public void RuneWidthAndPrecisionScansCancelAndDiscardStagedOutput(string kind, string conversion)
    {
        var scalar = kind switch { "bmp" => "界", "supplementary" => "😀", _ => "x" };
        // The supplementary form crosses the first 4 KiB boundary on a surrogate pair.
        var value = "a" + string.Concat(Enumerable.Repeat(scalar, 12_000));
        var format = conversion switch
        {
            "precision" => "prefix%.9000s",
            "width" => "prefix%20000s",
            "plain" => "prefix%s",
            _ => throw new ArgumentOutOfRangeException(nameof(conversion))
        };
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var memoryForGovernor = new CooperativeTestMemory(1 << 20)
            .InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var governor = memoryForGovernor.Accounting;
            var readingValue = false;
            memoryForGovernor.Budget.OnGrowth = _ => { if (readingValue) cancellation.Cancel(); };
            var arguments = new TestArguments(value, 0, onString: () => readingValue = true);
            var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(format, arguments,
                PrintfMissingArgumentPolicy.Throw, memoryForGovernor.State));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.True(readingValue);
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
            memoryForGovernor.Budget.OnGrowth = null;
            var completedMemory = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
            var completedMemoryBaseline = completedMemory.Accounting.LiveBytes;
            var completedMemoryBudget = completedMemory.Budget;
            using (completedMemory)
            {
                var completed = PrintfEvaluation.FormatByteText(format, new TestArguments(value, 0),
                    PrintfMissingArgumentPolicy.Throw, completedMemory.State);
                var expected = conversion switch
                {
                    "precision" => "prefixa" + string.Concat(Enumerable.Repeat(scalar, 8999)),
                    "width" => "prefix" + new string(' ', 7999) + value,
                    _ => "prefix" + value
                };
                Assert.Equal(expected, completed.AsString());
                Assert.Equal(conversion == "precision" ? 9006 : conversion == "width" ? 20006 : 12007, completed.RuneLength);
                Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
                Assert.Equal(completedMemoryBaseline, completedMemory.Accounting.LiveBytes);
            }
            Assert.Equal(0, completedMemoryBudget.Reserved);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("%d")]
    [InlineData("%f")]
    [InlineData("%a")]
    [InlineData("%c")]
    public void CancellationAfterNumericArgumentConversionPrecedesPendingOutput(string conversion)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var governor = memoryForGovernor.Accounting;
            var arguments = new TestArguments("x", 65, onNumber: cancellation.Cancel, numeric: true);
            var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
                "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, memoryForGovernor.State));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData("%s", "界")]
    [InlineData("%.0s", "界")]
    [InlineData("%.1s", "界")]
    [InlineData("%6.1s", "界")]
    [InlineData("%c", "界")]
    [InlineData("%c", "")]
    public void CancellationAfterStringConversionDiscardsThePendingPrintf(string conversion, string text)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var governor = memoryForGovernor.Accounting;
            var arguments = new TestArguments(text, 0, onString: cancellation.Cancel);
            var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
                "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, memoryForGovernor.State));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PublicationBoundaryCancellationReleasesTheBuilder(bool afterCopy, bool warmArgument)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        using var cancellation = new CancellationTokenSource();
        var boundaryCanceled = false;
        var initialAllocations = 0L;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            initialAllocations = counters[AwkCounter.TextAllocations];
            var initialFunding = budget.Reserved;
            if (afterCopy)
                budget.OnRefund = () =>
                {
                    if (counters[AwkCounter.TextAllocations] <= initialAllocations) return;
                    boundaryCanceled = true;
                    cancellation.Cancel();
                };
            else
                budget.OnGrowth = _ =>
                {
                    if (budget.Reserved < initialFunding + 20_000) return;
                    boundaryCanceled = true;
                    cancellation.Cancel();
                };
        };
        var warm = warmArgument ? "value=sprintf(\"%s\",value);" : "";
        var script = "BEGIN{value=\"界\";" + warm + "print \"before\";" +
            "result=sprintf(\"prefix%20000s\",value);print result;print \"after\"}";
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, null,
            counters, AwkRuntimeLimits.CreateDefault(), memoryBudget: budget)
        {
            InternalFailureObserver = AwkInternalFailuresAttribute.CreateObserver()
        };
        try
        {
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                AwkExecutor.ExecuteAsync(host, command, cancellation.Token, options));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
        }
        finally { budget.OnGrowth = null; budget.OnRefund = null; }
        Assert.True(boundaryCanceled);
        Assert.Equal(afterCopy, counters[AwkCounter.TextAllocations] > initialAllocations);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void EmptyFormatObservesAnAlreadyCanceledCaller()
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memoryForGovernor = new CooperativeTestMemory(1 << 20, enabled: false)
            .InitializeRuntime(counters, cancellation.Token);
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            cancellation.Cancel();
            var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
                string.Empty, new TestArguments("x", 0), PrintfMissingArgumentPolicy.Throw, memoryForGovernor.State));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormattingCheckpointsPreserveAdmission(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var memoryForGovernor = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters);
        var memoryForGovernorBaseline = memoryForGovernor.Accounting.LiveBytes;
        var memoryForGovernorBudget = memoryForGovernor.Budget;
        using (memoryForGovernor)
        {
            var governor = memoryForGovernor.Accounting;
            memoryForGovernor.State.ThrowIfCancellationRequested();
            var output = PrintfEvaluation.FormatByteText("%" + new string('0', 24_000) + "1$s", new TestArguments("界", 0), PrintfMissingArgumentPolicy.Throw, memoryForGovernor.State);
            Assert.Equal("界", output.AsString());

            memoryForGovernor.State.ThrowIfCancellationRequested();
            Assert.Equal(memoryForGovernorBaseline, governor.LiveBytes);
        }
        Assert.Equal(0, memoryForGovernorBudget.Reserved);
    }

    private readonly struct TestArguments(string text, double number, Action? onString = null, Action? onNumber = null, bool numeric = false) : IPrintfArguments
    {
        private readonly AwkText _text = AwkText.FromSourceLiteralString(text);

        public int Count => 1;

        public AwkText GetByteText(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
            onString?.Invoke();
            return _text;
        }

        public double GetNumber(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
            onNumber?.Invoke();
            return number;
        }

        public bool IsNumeric(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
            return numeric;
        }
    }
}
