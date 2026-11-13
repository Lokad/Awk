namespace Lokad.Awk.Tests;

public sealed class AwkArrayNumericInputTests
{
    [Theory]
    [InlineData(false, (int)AwkNumberParsingMode.Decimal, 29)]
    [InlineData(true, (int)AwkNumberParsingMode.Decimal, 29)]
    [InlineData(false, (int)AwkNumberParsingMode.HexadecimalInput, 45)]
    [InlineData(true, (int)AwkNumberParsingMode.HexadecimalInput, 45)]
    [InlineData(false, (int)AwkNumberParsingMode.NonDecimalData, 43)]
    [InlineData(true, (int)AwkNumberParsingMode.NonDecimalData, 43)]
    public void ArithmeticReadsKeepInputSpellingAndFullValueOrigins(bool indexed,
        int readingMode, double expected)
    {
        var mode = (AwkNumberParsingMode)readingMode;
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var variables = new AwkVariables(memory.Accounting, counters, static (_, _) => { }, numberParsingMode: mode);
        var name = AwkArrayName.FromText("a");
        Initialize(variables, name, ["0007", "010", "0x10", "12tail"], indexed);
        var array = variables.ResolveOrdinaryArray(name);
        var before = counters.TextValueClassifications;

        var sum = 0d;
        for (var index = 1; index <= 4; index++) sum += variables.GetArrayNumber(array, AwkArrayKey.FromOrdinal(index));

        Assert.Equal(expected, sum);
        Assert.Equal(before, counters.TextValueClassifications);
        var first = variables.GetArrayValue(array, AwkArrayKey.FromOrdinal(1));
        var second = variables.GetArrayValue(array, AwkArrayKey.FromOrdinal(2));
        var last = variables.GetArrayValue(array, AwkArrayKey.FromOrdinal(4));
        Assert.Equal("0007", first.AsString());
        Assert.Equal("010", second.AsString());
        Assert.Equal(AwkRuntimeValueKind.NumericString, first.RuntimeKind);
        Assert.Equal(AwkRuntimeValueKind.NumericString, second.RuntimeKind);
        Assert.Equal(AwkRuntimeValueKind.String, last.RuntimeKind);
        Assert.Equal("12tail", last.AsString());
        Assert.True(counters.TextValueClassifications > before);
        variables.ClearArray(array);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ArithmeticReadRetainsDeferredHexadecimalErrorAndInput(bool indexed)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, numberParsingMode: AwkNumberParsingMode.NonDecimalData);
        var name = AwkArrayName.FromText("a");
        Initialize(variables, name, ["0x1.8"], indexed);
        var array = variables.ResolveOrdinaryArray(name);

        var error = Assert.Throws<AwkRuntimeException>(() => variables.GetArrayNumber(array, AwkArrayKey.FromOrdinal(1)));

        Assert.Equal("awk: hexadecimal floating point constants require an exponent\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.True(array.ByteLookup.TryGetValue("1"u8, out var stored));
        Assert.Equal("0x1.8", stored.ByteText.AsString());
        Assert.Single(array);
        variables.ClearArray(array);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static void Initialize(AwkVariables variables, AwkArrayName name, string[] values, bool indexed)
    {
        if (indexed) variables.InitializeIndexedArrayExternalStringValues(name, values, firstIndex: 1);
        else variables.InitializeArrayExternalStringValues(name,
            values.Select((text, index) => new KeyValuePair<string, string>(
                (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), text)).ToArray(),
            static pair => pair.Key, static pair => pair.Value);
    }
}
