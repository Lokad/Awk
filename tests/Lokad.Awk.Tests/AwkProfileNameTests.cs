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
    public async Task LocalNamesRemainIndependentOfGlobalSpecialArrays(int modeValue)
    {
        const string source = """
            function f(and,SYMTAB){and++;SYMTAB[1]=and;return and}
            BEGIN{SYMTAB["x"]=3;print f(2,a),a[1],SYMTAB["x"],(x==3)}
            """;
        var arguments = (AwkCompatibilityMode)modeValue switch
        {
            AwkCompatibilityMode.Posix => new[] { "--posix", source },
            AwkCompatibilityMode.Traditional => new[] { "--traditional", source },
            _ => new[] { source }
        };
        var host = new MockFileSystem();
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments));
        Assert.Equal(AwkExitCode.Success, await command.ExecuteAsync(host, CancellationToken.None));
        Assert.Equal(modeValue == 0 ? "3 3 3 1\n" : "3 3 3 0\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
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
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = memory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memory.Configure(options), AwkCompatibilityProfile.Default.WithMode((AwkCompatibilityMode)modeValue))).State;
        var name = AwkArrayName.FromText(text);
        var key = AwkArrayKey.FromOrdinal(1);
        var before = state.MemoryAccounting.LiveBytes;
        Assert.True(state.Arrays.GetAwkValue(name, key).IsUntypedValue);
        Assert.True(state.MemoryAccounting.LiveBytes > before);
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.Arrays.GetElementRuntimeKind(name, key));
        var admitted = state.MemoryAccounting.LiveBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.Arrays.GetAwkValue(name, AwkArrayKey.FromOrdinal(2)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.True(state.TryGetExistingArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(admitted, state.MemoryAccounting.LiveBytes);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
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
        state.Arrays.SetAwkValue(destination, key, AwkValue.FromNumber(7));
        state.Arrays.SetAwkValue(separator, key, AwkValue.FromNumber(8));
        var before = state.MemoryAccounting.LiveBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => state.SplitIntoArray(
            AwkText.FromString("a,b"), destination, AwkFieldSeparatorSpec.FromText(","),
            AwkOptionalIdentifier.Some(separator.Identifier)));
        Assert.Equal("awk: split: fourth argument is a GNU extension\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.Arrays.GetAwkValue(destination, key).AsNumber());
        Assert.Equal(8, state.Arrays.GetAwkValue(separator, key).AsNumber());
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    private static AwkRuntimeState State(AwkCompatibilityMode mode, AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            AwkCompatibilityProfile.Default.WithMode(mode));
    }
}
