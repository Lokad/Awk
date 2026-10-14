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
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
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
    [InlineData("ascii", false)]
    [InlineData("ascii", true)]
    [InlineData("bmp", false)]
    [InlineData("bmp", true)]
    [InlineData("supplementary", false)]
    [InlineData("supplementary", true)]
    public void RuneWidthAndPrecisionScansCancelAndDiscardStagedOutput(string kind, bool precision)
    {
        var scalar = kind switch { "bmp" => "界", "supplementary" => "😀", _ => "x" };
        // The supplementary form crosses the first 4 KiB boundary on a surrogate pair.
        var value = "a" + string.Concat(Enumerable.Repeat(scalar, 12_000));
        var format = precision ? "prefix%.9000s" : "prefix%20000s";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
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
            format, arguments, limits, counters, governor, checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(4, callsDuringValue);
        Assert.InRange(governor.PeakCommittedBytes, 1, 1 << 20);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, counters.Utf8Encodes);

        var completed = AwkFormattingRuntime.FormatPrintfStrictByteText(format,
            new TestArguments(value, 0), limits, counters, governor);
        var expected = precision ? "prefixa" + string.Concat(Enumerable.Repeat(scalar, 8999))
            : "prefix" + new string(' ', 7999) + value;
        Assert.Equal(expected, completed.AsString());
        Assert.Equal(precision ? 9006 : 20006, completed.RuneLength);
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
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        var arguments = new TestArguments("x", 65, onNumber: cancellation.Cancel, numeric: true);
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, limits, counters, governor,
            checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(governor.PeakCommittedBytes, 1, 1 << 20);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("%s")]
    [InlineData("%c")]
    public void CancellationAfterStringConversionDiscardsThePendingPrintf(string conversion)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        var arguments = new TestArguments("界", 0, onString: cancellation.Cancel);
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            "prefix" + conversion, arguments, PrintfMissingArgumentPolicy.Throw, limits, counters, governor,
            checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicationBoundaryCancellationReleasesTheBuilder(bool afterCopy)
    {
        const string format = "prefix%20000s";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        var total = 0;
        var encodesAtCheckpoint = new List<long>();
        var arguments = new TestArguments("界", 0);
        Assert.Equal(20006, PrintfEvaluation.FormatByteText(format, arguments, PrintfMissingArgumentPolicy.Throw,
            limits, counters, governor, checkpoint: () =>
            {
                total++;
                encodesAtCheckpoint.Add(counters.Utf8Encodes);
            }).RuneLength);
        counters = AwkRuntimeCounters.CreateEnabled();
        governor = AwkMemoryAccounting.Create(limits, counters);
        var cancelAt = afterCopy ? total : encodesAtCheckpoint.FindLastIndex(static encodes => encodes == 0) + 1;
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == cancelAt)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(format,
            arguments, PrintfMissingArgumentPolicy.Throw, limits, counters, governor, checkpoint: Checkpoint));
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
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(string.Empty,
            new TestArguments("x", 0), PrintfMissingArgumentPolicy.Throw, AwkRuntimeLimits.CreateDefault(),
            counters, AwkMemoryAccounting.Disabled, checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormattingCheckpointsDoNotChargeExtraFuelOrChangeAdmission(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20).WithMaxExecutionSteps(1);
        var resources = new AwkRuntimeResourceMonitor(limits, counters);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        resources.RecordExecutionStep();
        var output = AwkFormattingRuntime.FormatPrintfStrictByteText("%" + new string('0', 24_000) + "1$s",
            new TestArguments("界", 0), limits, counters, governor,
            checkpoint: () => resources.ThrowIfCancellationRequested(CancellationToken.None));
        Assert.Equal("界", output.AsString());
        Assert.Equal(observed ? 1 : 0, counters.ExecutionSteps);
        if (observed)
            Assert.True(counters.CancellationChecks > 1);
        var failure = Assert.Throws<AwkRuntimeException>(() => resources.RecordExecutionStep());
        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private readonly struct TestArguments(string text, double number, Action? onString = null,
        Action? onNumber = null, bool numeric = false) : IPrintfArguments
    {
        public int Count => 1;

        public string GetString(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
            onString?.Invoke();
            return text;
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
