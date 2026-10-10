namespace Lokad.Awk.Tests;

public sealed class AwkScanCancellationTests
{
    private const int LongScan = 16_384;

    [Theory]
    [InlineData("integer")]
    [InlineData("fraction")]
    [InlineData("exponent")]
    [InlineData("leading")]
    [InlineData("special")]
    [InlineData("payload")]
    [InlineData("hexadecimal")]
    [InlineData("hex-exponent")]
    public void NumericScansObserveCancellationWithinTheScan(string scenario)
    {
        var zeros = new string('0', LongScan);
        var (source, mode) = scenario switch
        {
            "integer" => (zeros + "1", AwkNumberParsingMode.Decimal),
            "fraction" => ("0." + zeros + "1", AwkNumberParsingMode.Decimal),
            "exponent" => ("1e" + zeros + "1", AwkNumberParsingMode.Decimal),
            "leading" => (new string(' ', LongScan) + "1", AwkNumberParsingMode.Decimal),
            "special" => ("+inf" + new string(' ', LongScan), AwkNumberParsingMode.Decimal),
            "payload" => ("-nan(" + new string('_', LongScan) + ")", AwkNumberParsingMode.HexadecimalInput),
            "hexadecimal" => ("0x1." + zeros + "1p-1075", AwkNumberParsingMode.HexadecimalInput),
            "hex-exponent" => ("0x1p" + zeros + "1", AwkNumberParsingMode.HexadecimalInput),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var bytes = AwkUtf8Codec.Encode(source).ToArray();
        using var cancellation = new CancellationTokenSource();
        var checkpoint = CancelDuringScan(cancellation);

        var error = Assert.Throws<OperationCanceledException>(() => AwkNumberParser.Parse(bytes, mode, checkpoint));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        var result = AwkNumberParser.Parse(bytes, mode);
        Assert.Equal(AwkNumberParseStatus.Number, result.Status);
        Assert.Equal(scenario == "special" ? 4 : bytes.Length, result.End);
        Assert.Equal(source, AwkUtf8Codec.Decode(bytes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NondecimalDigitsAndExponentSearchObserveCancellation(bool exponentSearch)
    {
        var source = exponentSearch
            ? "0x0." + new string('0', LongScan) + "p0"
            : "0x" + new string('0', LongScan) + "1";
        var bytes = AwkUtf8Codec.Encode(source).ToArray();
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() =>
            AwkNumberParser.ParseNonDecimal(bytes, 0, 16, CancelDuringScan(cancellation)));
        Assert.Equal(AwkNumberParseStatus.Number, AwkNumberParser.ParseNonDecimal(bytes, 0, 16).Status);
    }

    [Fact]
    public void BaseSelectionAndTrailingWhitespaceObserveCancellation()
    {
        var bytes = AwkUtf8Codec.Encode(new string('0', LongScan)).ToArray();
        using var baseCancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() =>
            AwkNumberParser.GetBase(bytes, CancelDuringScan(baseCancellation)));
        Assert.Equal(8, AwkNumberParser.GetBase(bytes));

        var whitespace = AwkUtf8Codec.Encode("1" + new string(' ', LongScan)).ToArray();
        using var whitespaceCancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() =>
            AwkNumberParser.SkipWhitespace(whitespace, 1, CancelDuringScan(whitespaceCancellation, 1)));
        Assert.Equal(whitespace.Length, AwkNumberParser.SkipWhitespace(whitespace, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScanCheckpointsDoNotChargeOfferedBytesAgain(bool observed)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var number = AwkText.FromString("0x" + new string('0', LongScan) + "1");
        var chunks = AwkExecutionGovernor.ByteWorkSteps(number.ByteLength);
        var resources = new AwkRuntimeResourceMonitor(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks), counters);
        Action checkpoint = () => resources.ThrowIfCancellationRequested(CancellationToken.None);
        resources.RecordByteWork(number.ByteLength);

        var parsed = AwkNumberParser.ParseNonDecimal(number.AsSpan(), 0, 16, checkpoint);

        Assert.Equal(1, parsed.Number);
        Assert.Equal(observed ? chunks : 0, counters.ExecutionSteps);
        if (observed) Assert.True(counters.CancellationChecks > 1);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("three-byte")]
    [InlineData("supplementary")]
    public void InterruptedRuneCountingDoesNotPublishPartialFacts(string scenario)
    {
        var unit = scenario switch
        {
            "ascii" => "a",
            "three-byte" => "\u754c",
            "supplementary" => "\ud83d\ude00",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var text = AwkText.FromString(string.Concat(Enumerable.Repeat(unit, LongScan)));
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => text.GetRuneLength(CancelDuringScan(cancellation)));
        Assert.Throws<OperationCanceledException>(() => text.GetRuneLength(cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(LongScan, text.GetRuneLength());
    }

    [Fact]
    public void InterruptedRuneIndexReleasesReservationAndCanBeRebuilt()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20), counters);
        var text = AwkText.FromString(new string('\u754c', LongScan));
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() =>
            text.CopyRuneSlice(100, 2, counters, governor, CancelDuringScan(cancellation, 3)));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, counters.RuneSliceIndexAllocations);
        Assert.Throws<OperationCanceledException>(() =>
            text.CopyRuneSlice(100, 2, counters, governor, cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal("\u754c\u754c", text.CopyRuneSlice(100, 2, counters, governor).AsString());
        Assert.Equal(1, counters.RuneSliceIndexAllocations);
        Assert.Equal(LongScan, text.RuneLength);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuneScansRetainExplicitFuelAndCounterIndependence(bool observed)
    {
        var source = AwkText.FromString(new string('\u754c', LongScan));
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var chunks = AwkExecutionGovernor.ByteWorkSteps(source.ByteLength);
        var resources = new AwkRuntimeResourceMonitor(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(chunks), counters);
        var strings = new AwkStringRuntime(AwkStringUnitMode.Utf8Rune, resources.RecordByteWork,
            () => resources.ThrowIfCancellationRequested(CancellationToken.None));

        Assert.Equal(LongScan, strings.CountUnits(source));
        Assert.Equal(observed ? chunks : 0, counters.ExecutionSteps);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("strtonum")]
    [InlineData("tolower")]
    [InlineData("toupper")]
    [InlineData("sprintf")]
    public async Task BuiltinsObserveCancellationAfterArgumentEvaluation(string name)
    {
        using var cancellation = new CancellationTokenSource();
        var state = CreateState(cancellation.Token);
        var value = AwkValue.FromDeferredText(
            AwkText.FromString(name is "tolower" or "toupper" ? new string('a', LongScan) : new string('0', LongScan)),
            AwkNumberParsingMode.Decimal);
        var canceling = new CancelAfterValueExpression(cancellation, value);
        Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
        AwkExpression[] arguments = name == "sprintf" ? [AwkExpression.Literal("%d"), canceling] : [canceling];

        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await builtin.EvaluateAsync(new AwkBuiltinContext(arguments, new AwkEvaluationContext(state))));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeClassificationChecksCancellationBeforeAssignment(bool input)
    {
        using var cancellation = new CancellationTokenSource();
        var state = CreateState(cancellation.Token);
        var name = AwkVariableName.FromText("kept");
        state.AssignVariableValue(name, AwkValue.FromNumber(7));
        var source = AwkText.FromString(new string('0', LongScan) + "1");
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => state.AssignVariableValue(name,
            input ? state.CreateNumericStringValue(source) : state.CreateByteTextValue(source)));

        Assert.Equal(7, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    private static Action CancelDuringScan(CancellationTokenSource cancellation, int checkpointNumber = 2)
    {
        var calls = 0;
        return () =>
        {
            if (++calls == checkpointNumber) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
    }

    [Fact]
    public void FormatScanningChecksAfterAScalarCrossesTheChunkBoundary()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(32 << 10);
        var governor = AwkMemoryGovernor.Create(limits, counters);
        var format = new string('a', 4095) + "\ud83d\ude00a";
        using var cancellation = new CancellationTokenSource();

        Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            format, new EmptyPrintfArguments(), PrintfMissingArgumentPolicy.Throw,
            limits, counters, governor, checkpoint: CancelDuringScan(cancellation)));

        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private readonly struct EmptyPrintfArguments : IPrintfArguments
    {
        public int Count => 0;
        public string GetString(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        public double GetNumber(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        public bool IsNumeric(int index) => throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static AwkRuntimeState CreateState(CancellationToken token) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(),
                AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
                AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter));

    private sealed class CancelAfterValueExpression(CancellationTokenSource cancellation, AwkValue value) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            cancellation.Cancel();
            return ValueTask.FromResult(value);
        }
    }
}
