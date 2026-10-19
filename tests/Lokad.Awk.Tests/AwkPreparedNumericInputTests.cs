namespace Lokad.Awk.Tests;

public sealed class AwkPreparedNumericInputTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocalArgumentTypeProbeDoesNotReadOrAdmitTheValue(bool numericInput)
    {
        using var memoryForState = CreateState();
        var state = memoryForState.State;
        var value = numericInput ? AwkValue.FromNumericString("12345") : AwkValue.FromText("12345");
        using var frame = Store(state, value, 2);
        var created = state.Counters.ValuesCreated;
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(state.HasPreparedLocalScalarArgument(new AwkFunctionLocalSlotId(0)));
        Assert.Equal(1, state.Counters.BoundLocalScalarArguments);
        Assert.Equal(created, state.Counters.ValuesCreated);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("123.5", 123.5, 0)]
    [InlineData("123.5", 123.5, 1)]
    [InlineData("123.5", 123.5, 2)]
    [InlineData("1e2", 100, 0)]
    [InlineData("1e2", 100, 1)]
    [InlineData("1e2", 100, 2)]
    [InlineData("1.5 ", 1.5, 0)]
    [InlineData("1.5 ", 1.5, 1)]
    [InlineData("1.5 ", 1.5, 2)]
    public void StoredInputPreservesTextClassificationAndReadAccounting(string source, double number, int storage)
    {
        using var memoryForState = CreateState();
        var state = memoryForState.State;
        var text = AwkText.FromSourceLiteralString(source);
        var input = state.CreateNumericStringValue(text);
        using var frame = Store(state, input, storage);
        var before = state.Counters.ValuesCreated;
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var result = Read(state, storage);
        Assert.Same(text, result.ByteText);
        Assert.Equal(AwkRuntimeValueKind.NumericString, result.RuntimeKind);
        Assert.Equal(number, result.AsNumber());
        Assert.Equal(0, AwkValue.Compare(result, AwkValue.FromNumber(number)));
        Assert.Equal(before + 1, state.Counters.ValuesCreated);
        Assert.Equal(1, state.Counters.PreparedNumericSlotReads);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void ModeSensitiveInputStillUsesTheReadingMode(bool nonDecimal, int storage)
    {
        using var memoryForState = CreateState(nonDecimal: nonDecimal);
        var state = memoryForState.State;
        var originalMode = nonDecimal ? AwkNumberParsingMode.Decimal : AwkNumberParsingMode.NonDecimalData;
        using var frame = Store(state, AwkValue.FromNumericString("010", originalMode), storage);
        var result = Read(state, storage);
        Assert.Equal(nonDecimal ? 8 : 10, result.AsNumber());
        Assert.Equal("010", result.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, result.RuntimeKind);
        Assert.Equal(0, state.Counters.PreparedNumericSlotReads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MissingHexExponentRemainsDeferredUntilNumericConversion(int storage)
    {
        using var memoryForState = CreateState(nonDecimal: true);
        var state = memoryForState.State;
        var input = state.CreateNumericStringValue("0x1.5");
        using var frame = Store(state, input, storage);
        var result = Read(state, storage);
        Assert.Equal("0x1.5", result.AsString());
        Assert.Throws<AwkRuntimeException>(() => result.AsNumber());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void LongStoredInputStillObservesCancellationOnRead(int storage)
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = CreateState(cancellationToken: cancellation.Token);
        var state = memoryForState.State;
        var input = state.CreateNumericStringValue("1" + new string('0', 8000));
        using var frame = Store(state, input, storage);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => Read(state, storage));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, state.Counters.PreparedNumericSlotReads);
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void RejectedStoredInputPreservesTheOldElementAndCharge()
    {
        using var memoryForState = CreateState(maxMemoryBytes: 1024);
        var state = memoryForState.State;
        Store(state, state.CreateNumericStringValue("123"), storage: 1);
        var retained = state.MemoryAccounting.CurrentCommittedBytes;
        var oversized = AwkValue.FromNumericString("1" + new string('0', 2000));
        Assert.Throws<AwkRuntimeException>(() => Store(state, oversized, storage: 1));
        Assert.Equal(123, Read(state, storage: 1).AsNumber());
        Assert.Equal(retained, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static StoredLocalFrame Store(AwkRuntimeState state, AwkValue value, int storage)
    {
        if (storage == 2)
        {
            state.PushFunctionFrame(new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]),
                [AwkFunctionArgument.Scalar(value)]);
            return new(state);
        }
        if (storage == 1) state.SetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1), value);
        else state.AssignVariableValue(AwkVariableName.FromText("x"), value);
        return default;
    }

    private static AwkValue Read(AwkRuntimeState state, int storage) => storage switch
    {
        2 => state.ResolveFunctionLocalValue(new AwkFunctionLocalSlotId(0), AwkVariableName.FromText("x")),
        1 => state.GetArrayAwkValue(AwkArrayName.FromText("a"), AwkArrayKey.FromOrdinal(1)),
        _ => state.GetGlobalScalarValue(state.GetOrCreateGlobalScalarSlot(AwkVariableName.FromText("x")))
    };

    private readonly struct StoredLocalFrame(AwkRuntimeState? state) : IDisposable
    {
        public void Dispose() => state?.PopFunctionFrame();
    }

    private static CooperativeTestMemory CreateState(bool nonDecimal = false, CancellationToken cancellationToken = default,
        long maxMemoryBytes = 1 << 20)
    {

        var testMemory = new CooperativeTestMemory(maxMemoryBytes, enabled: true && (long)(maxMemoryBytes) != long.MaxValue);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken, testMemory.Configure(options),
            new AwkRuntimeResourceMonitor(limits, counters, cancellationToken),
            new AwkCompatibilityProfile(AwkCompatibilityMode.GnuDefault, AwkStringUnitMode.Utf8Rune, nonDecimal, AwkLintMode.Off)));
    }
}
