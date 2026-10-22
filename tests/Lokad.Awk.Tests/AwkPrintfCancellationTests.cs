namespace Lokad.Awk.Tests;

public sealed class AwkPrintfCancellationTests
{
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
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        PrintfDirective Read(string format, Action checkpoint)
        {
            var cursor = new PrintfArgumentCursor<TestArguments>(new TestArguments("x", 1),
                PrintfMissingArgumentPolicy.Throw, format, counters, governor, checkpoint);
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
        Assert.Equal(shortCalls, calls);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var readingValue = false;
        var callsDuringValue = 0;
        void Checkpoint()
        {
            if (readingValue && ++callsDuringValue == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var arguments = new TestArguments(value, 0, onString: () => readingValue = true);
        var failure = Assert.Throws<OperationCanceledException>(() => AwkFormattingRuntime.FormatPrintfStrictByteText(
            format, arguments, counters, governor, checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(4, callsDuringValue);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, counters.Utf8Encodes);

        var completed = AwkFormattingRuntime.FormatPrintfStrictByteText(format, new TestArguments(value, 0), counters, governor);
        var expected = conversion switch
        {
            "precision" => "prefixa" + string.Concat(Enumerable.Repeat(scalar, 8999)),
            "width" => "prefix" + new string(' ', 7999) + value,
            _ => "prefix" + value
        };
        Assert.Equal(expected, completed.AsString());
        Assert.Equal(conversion == "precision" ? 9006 : conversion == "width" ? 20006 : 12007, completed.RuneLength);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(4, callsDuringValue);
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
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        var arguments = new TestArguments("x", 65, onNumber: cancellation.Cancel, numeric: true);
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, counters, governor, checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        var arguments = new TestArguments(text, 0, onString: cancellation.Cancel);
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, counters, governor, checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PublicationBoundaryCancellationReleasesTheBuilder(bool afterCopy, bool warmArgument)
    {
        const string format = "prefix%20000s";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        var total = 0;
        var encodesAtCheckpoint = new List<long>();
        TestArguments Arguments()
        {
            var value = new TestArguments("界", 0);
            if (warmArgument)
                value.GetByteText(0).AsString();
            return value;
        }
        var arguments = Arguments();
        Assert.Equal(20006, PrintfEvaluation.FormatByteText(format, arguments, PrintfMissingArgumentPolicy.Throw, counters, governor, checkpoint: () =>
            {
                total++;
                encodesAtCheckpoint.Add(counters.Utf8Encodes);
            }).RuneLength);
        counters = AwkRuntimeCounters.CreateEnabled();
        using var secondMemory = new CooperativeTestMemory(1 << 20, counters);
        governor = secondMemory.Accounting;
        var cancelAt = afterCopy ? total : encodesAtCheckpoint.FindLastIndex(static encodes => encodes == 0) + 1;
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == cancelAt)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(format, Arguments(), PrintfMissingArgumentPolicy.Throw, counters, governor, checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(afterCopy ? 1 : 0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void EmptyFormatObservesAnAlreadyCanceledCaller()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(string.Empty, new TestArguments("x", 0), PrintfMissingArgumentPolicy.Throw, counters, AwkMemoryAccounting.Disabled, checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormattingCheckpointsPreserveAdmission(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = AwkRuntimeLimits.CreateDefault();
        var resources = new AwkRuntimeResourceMonitor(limits, counters);
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        resources.Checkpoint();
        var output = AwkFormattingRuntime.FormatPrintfStrictByteText("%" + new string('0', 24_000) + "1$s", new TestArguments("界", 0), counters, governor, checkpoint: () => resources.ThrowIfCancellationRequested(CancellationToken.None));
        Assert.Equal("界", output.AsString());
        if (observed)
            Assert.True(counters.CancellationChecks > 1);
        resources.Checkpoint();
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
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
