using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkNumericByteEscapeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ScalarBoundariesRoundTripThroughNumericBytes(int profile)
    {
        var mode = (AwkCompatibilityMode)profile;
        int[] values = [0, 1, 31, 32, 92, 127, 128, 255, 256, 0x7ff, 0x800,
            0xd7ff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff];
        Span<byte> bytes = stackalloc byte[4];
        foreach (var value in values)
        {
            var scalar = new Rune(value);
            var length = scalar.EncodeToUtf8(bytes);
            var octal = new StringBuilder();
            var hex = new StringBuilder();
            foreach (var item in bytes[..length])
            {
                octal.Append('\\').Append(Convert.ToString(item, 8).PadLeft(3, '0'));
                hex.Append("\\x").Append(item.ToString("X2"));
            }
            Assert.Equal(scalar.ToString(), AwkEscapes.DecodeSourceString(octal.ToString(), mode));
            if (mode != AwkCompatibilityMode.Posix)
                Assert.Equal(scalar.ToString(), AwkEscapes.DecodeSourceString(hex.ToString(), mode));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void NumericRegexUsesTheExistingCacheAndByteNativeCounters(int profile, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var runtime = Runtime(profile, utf8, counters, limits);
        var pattern = new AwkRegexPattern(@"\303\251");
        var regex = runtime.Create(pattern);
        Assert.Same(regex, runtime.Create(pattern));
        Assert.True(runtime.IsMatch(regex, AwkText.FromString("é", counters)));
        Assert.False(runtime.IsMatch(regex, AwkText.FromString("Ã©", counters)));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(2, counters.RegexMatches);
        Assert.Equal(utf8 ? 0 : 2, counters.StringDecodes);
        Assert.Equal(utf8 ? 0 : 2, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        Assert.Equal(utf8 ? 0 : 2, counters.RegexDotNetTextMatches);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void NumericRegexBufferRejectsBothAttemptsWithoutCachePublication(int profile, bool utf8)
    {
        const string pattern = @"\360\237\230\200";
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(AwkRuntimeMemory.EstimateEscapeDecodingBytes(pattern.Length) - 1, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(profile, utf8, counters, limits, governor);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern(pattern)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(2, counters.RegexCompiles); // Counts attempted compilations, including rejected admission.
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OperandDecodeBufferPreflightsBeforeReplacingTheVariable(bool counters)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memory.Configure(Options(AwkRuntimeLimits.CreateDefault(), counters)))).State;
        var name = AwkIdentifier.FromTrustedName("v");
        state.AssignVariableFromExternalText(name, "keep");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var assignment = AwkVariableAssignment.InputArgument(name,
            @"\303\251" + new string('a', 1 << 18));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => state.AssignVariableFromExternalText(assignment));
        Assert.Equal("keep", state.FormatStringByteText(state.ResolveVariableValue(new AwkVariableName(name))).AsString(state.Counters));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void LongNumericStreamsObserveCancellation(int profile, bool regex)
    {
        var text = string.Concat(Enumerable.Repeat(@"\360\237\230\200", 8192));
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 20) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
        {
            if (regex)
                AwkRegexEscapes.Decode(text, (AwkCompatibilityMode)profile, Check);
            else
                AwkEscapes.DecodeSourceString(text, (AwkCompatibilityMode)profile, Check);
        });
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(20, checks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ByteAggregationAcrossRemovedContinuationsIsBounded(bool crlf)
    {
        var continuation = crlf ? "\\\r\n" : "\\\n";
        var text = @"\303" + string.Concat(Enumerable.Repeat(continuation, 8192)) + @"\251";
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (++checks == 5) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkEscapes.DecodeSourceString(text, AwkCompatibilityMode.GnuDefault, Check));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(5, checks);
        Assert.Equal("é", AwkEscapes.DecodeSourceString(text, AwkCompatibilityMode.GnuDefault));
    }

    private static AwkRegexRuntime Runtime(int profile, bool utf8, AwkRuntimeCounters counters,
        AwkRuntimeLimits limits, AwkMemoryAccounting? governor = null) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters), governor, (AwkCompatibilityMode)profile, regexEngine: AwkUtf8RegexEngine.Instance);

    private static AwkRuntimeOptions Options(AwkRuntimeLimits limits, bool counters) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled, limits,
            AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance);
}
