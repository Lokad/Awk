namespace Lokad.Awk.Tests;

public sealed class AwkProfileNameTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SoftParameterNamesKeepDivisionAndResetAtFunctionBoundary(int modeValue)
    {
        var mode = (AwkCompatibilityMode)modeValue;
        const string source = "function f(switch){return switch/2}BEGIN{switch(1){case 1:print 9}print \"a\"~/a/}";
        var tokens = AwkLexer.Tokenize(source, CancellationToken.None, mode);
        var names = tokens.Where(token => token.Text == "switch").ToArray();
        Assert.Equal(3, names.Length);
        Assert.Equal(AwkTokenKind.Identifier, names[0].Kind);
        Assert.Equal(AwkTokenKind.Identifier, names[1].Kind);
        Assert.Equal(mode == AwkCompatibilityMode.GnuDefault ? AwkTokenKind.Switch : AwkTokenKind.Identifier,
            names[2].Kind);
        Assert.Single(tokens, token => token.Kind == AwkTokenKind.Slash);
        Assert.Single(tokens, token => token.Kind == AwkTokenKind.Regex);
        foreach (var token in names)
            Assert.Equal(token.Text, source.Substring(token.Span.Start, token.Span.Length));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SemanticModelDistinguishesLocalNamesFromGlobalSpecialArrays(int modeValue)
    {
        var mode = (AwkCompatibilityMode)modeValue;
        const string source = "function f(and,SYMTAB){and++;SYMTAB[1]=and;return and}BEGIN{SYMTAB[\"x\"]=3;print f(2,a)}";
        var parse = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue), CancellationToken.None, mode);
        var binding = AwkBinder.Bind(parse, AwkCompatibilityProfile.Default.WithMode(mode));
        Assert.True(binding.Success);
        Assert.Contains(binding.SemanticModel.Usages, symbol =>
            symbol.Name == "and" && symbol.Kind == AwkSemanticSymbolKind.Variable && symbol.IsFunctionLocal);
        Assert.Contains(binding.SemanticModel.Usages, symbol =>
            symbol.Name == "SYMTAB" && symbol.Kind == AwkSemanticSymbolKind.Array && symbol.IsFunctionLocal);
        Assert.Contains(binding.SemanticModel.Usages, symbol => symbol.Name == "SYMTAB" && !symbol.IsFunctionLocal &&
            symbol.Kind == (mode == AwkCompatibilityMode.GnuDefault
                ? AwkSemanticSymbolKind.SpecialArray : AwkSemanticSymbolKind.Array));
    }

    [Theory]
    [InlineData(1, "FUNCTAB")]
    [InlineData(1, "SYMTAB")]
    [InlineData(2, "FUNCTAB")]
    [InlineData(2, "SYMTAB")]
    public void DisabledSpecialArrayNamesRetainOrdinaryMemoryAdmission(int modeValue, string text)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memory = new CooperativeTestMemory(1 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits);
        var state = memory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options), new(limits, counters), AwkCompatibilityProfile.Default.WithMode((AwkCompatibilityMode)modeValue))).State;
        var name = AwkArrayName.FromText(text);
        var key = AwkArrayKey.FromOrdinal(1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(state.GetArrayAwkValue(name, key).IsUntypedValue);
        Assert.True(state.MemoryAccounting.CurrentCommittedBytes > before);
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.GetArrayElementRuntimeKind(name, key));
        var admitted = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayAwkValue(name, AwkArrayKey.FromOrdinal(2)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.True(state.TryGetExistingArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(admitted, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RejectedSeparatorCapturePreservesBothArrays(int modeValue)
    {
        var state = State((AwkCompatibilityMode)modeValue, AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var destination = AwkArrayName.FromText("values");
        var separator = AwkArrayName.FromText("separators");
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(destination, key, AwkValue.FromNumber(7));
        state.SetArrayAwkValue(separator, key, AwkValue.FromNumber(8));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => state.SplitIntoArray(
            AwkText.FromString("a,b"), destination, AwkFieldSeparatorSpec.FromText(","),
            AwkOptionalIdentifier.Some(separator.Identifier)));
        Assert.Equal("awk: split: fourth argument is a GNU extension\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.GetArrayAwkValue(destination, key).AsNumber());
        Assert.Equal(8, state.GetArrayAwkValue(separator, key).AsNumber());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    private static AwkRuntimeState State(AwkCompatibilityMode mode, AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits);
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, counters), AwkCompatibilityProfile.Default.WithMode(mode));
    }
}
