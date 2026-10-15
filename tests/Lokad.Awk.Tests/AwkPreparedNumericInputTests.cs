namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumericInputTests
{
    [Theory]
    [InlineData("123.5", 123.5, false)]
    [InlineData("123.5", 123.5, true)]
    [InlineData("1e2", 100, false)]
    [InlineData("1e2", 100, true)]
    [InlineData("1.5 ", 1.5, false)]
    [InlineData("1.5 ", 1.5, true)]
    public void StoredInputPreservesTextClassificationAndReadAccounting(string source, double number, bool array)
    {
        var state = CreateState();
        var text = AwkText.FromSourceLiteralString(source);
        var input = state.CreateNumericStringValue(text);
        Store(state, input, array);
        var before = state.Counters.ValuesCreated;
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var result = Read(state, array);
        Assert.Same(text, result.ByteText);
        Assert.Equal(AwkRuntimeValueKind.NumericString, result.RuntimeKind);
        Assert.Equal(number, result.AsNumber());
        Assert.Equal(0, AwkValue.Compare(result, AwkValue.FromNumber(number)));
        Assert.Equal(before + 1, state.Counters.ValuesCreated);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ModeSensitiveInputStillUsesTheReadingMode(bool nonDecimal, bool array)
    {
        var state = CreateState(nonDecimal: nonDecimal);
        var originalMode = nonDecimal ? AwkNumberParsingMode.Decimal : AwkNumberParsingMode.NonDecimalData;
        Store(state, AwkValue.FromNumericString("010", originalMode), array);
        var result = Read(state, array);
        Assert.Equal(nonDecimal ? 8 : 10, result.AsNumber());
        Assert.Equal("010", result.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, result.RuntimeKind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingHexExponentRemainsDeferredUntilNumericConversion(bool array)
    {
        var state = CreateState(nonDecimal: true);
        var input = state.CreateNumericStringValue("0x1.5");
        Store(state, input, array);
        var result = Read(state, array);
        Assert.Equal("0x1.5", result.AsString());
        Assert.Throws<AwkRuntimeException>(() => result.AsNumber());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongStoredInputStillObservesCancellationOnRead(bool array)
    {
        using var cancellation = new CancellationTokenSource();
        var state = CreateState(cancellationToken: cancellation.Token);
        var input = state.CreateNumericStringValue("1" + new string('0', 8000));
        Store(state, input, array);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => Read(state, array));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void RejectedStoredInputPreservesTheOldElementAndCharge()
    {
        var state = CreateState(maxMemoryBytes: 512);
        Store(state, state.CreateNumericStringValue("123"), array: true);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var oversized = AwkValue.FromNumericString("1" + new string('0', 2000));
        Assert.Throws<AwkRuntimeException>(() => Store(state, oversized, array: true));
        Assert.Equal(123, Read(state, array: true).AsNumber());
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StoredInputStillPreflightsValueSizeBeforeReadAccounting(bool array)
    {
        var state = CreateState(maxValueBytes: 3);
        Store(state, AwkValue.FromNumericString("12345"), array);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var created = state.Counters.ValuesCreated;
        Assert.Throws<AwkRuntimeException>(() => Read(state, array));
        Assert.Equal(created, state.Counters.ValuesCreated);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static void Store(AwkRuntimeState state, AwkValue value, bool array)
    {
        if (array) state.SetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1), value);
        else state.AssignVariableValue(AwkVariableName.FromText("x"), value);
    }

    private static AwkValue Read(AwkRuntimeState state, bool array) => array
        ? state.GetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1))
        : state.GetGlobalScalarValue(state.GetOrCreateGlobalScalarSlot(AwkVariableName.FromText("x")));

    private static AwkRuntimeState CreateState(bool nonDecimal = false, CancellationToken cancellationToken = default,
        long maxValueBytes = AwkRuntimeLimits.Unlimited, long maxMemoryBytes = 1 << 20)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(maxMemoryBytes).WithMaxValueBytes(maxValueBytes);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        return new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken, options,
            new AwkRuntimeResourceMonitor(limits, counters, cancellationToken),
            new AwkCompatibilityProfile(AwkCompatibilityMode.GnuDefault, AwkStringUnitMode.Utf8Rune, nonDecimal, AwkLintMode.Off));
    }
}
