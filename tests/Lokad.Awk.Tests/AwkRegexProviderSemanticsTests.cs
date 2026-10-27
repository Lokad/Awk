using System.Threading;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexProviderSemanticsTests
{
    [Fact]
    internal void EquivalentPatternsAndCaseChangesKeepCompiledCacheIdentity()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var sensitivity = AwkRegexCaseSensitivity.CaseSensitive;
        var runtime = new AwkRegexRuntime(() => sensitivity, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, regexEngine: AwkUtf8RegexEngine.Instance);
        const string pattern = "^(SKU)([[:alpha:]]+)([0-9]+)$";
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var equalText = new string(pattern.ToCharArray());
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(equalText)));
        var input = AwkText.FromString("skuItem007");
        long preparedCompiles = 0;
        for (var pass = 0; pass < 4; pass++)
        {
            foreach (var ignoreCase in new[] { false, true })
            {
                sensitivity = ignoreCase ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;
                using var match = runtime.MatchCaptureValue(regex, input, AwkMemoryAccounting.Disabled);
                Assert.Equal(ignoreCase, match.Match.Success);
                if (ignoreCase)
                {
                    Assert.Equal("sku", match.Match.Groups[1].Value.AsString());
                    Assert.Equal("Item", match.Match.Groups[2].Value.AsString());
                    Assert.Equal("007", match.Match.Groups[3].Value.AsString());
                }
            }
            if (pass == 0)
                preparedCompiles = counters.RegexCompiles;
            else
                Assert.Equal(preparedCompiles, counters.RegexCompiles);
        }
    }

    [Fact]
    internal void RepeatedMatchingUsesThePreparedProviderHandle()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(counters,AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern("a+"));
        var input = AwkText.FromString("zaaa", counters);
        var allocations = counters.AwkTextAllocations;

        Assert.True(runtime.IsMatch(regex, input));
        Assert.Equal(1, counters.RegexCompiles);
        var first = runtime.MatchSpan(regex, input);
        var second = runtime.MatchSpan(regex, input);

        Assert.Equal(1, first.ByteIndex);
        Assert.Equal(3, first.ByteLength);
        Assert.Equal(first, second);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(3, counters.RegexMatches);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData("[a-z]+")]
    [InlineData("SKU[0-9]+")]
    [InlineData("(a|b)+")]
    [InlineData("[;,]+")]
    [InlineData(".")]
    [InlineData("😀+")]
    internal void UnambiguousScalarPatternsKeepByteNativeRoute(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(counters,AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("SKU12abc;,😀😀", counters);

        Assert.True(runtime.MatchSpan(regex, input).Success);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Theory]
    [InlineData(AwkStringUnitMode.Utf8Rune, Skip = AwkRegexProviderGaps.SkipReason)]
    [InlineData(AwkStringUnitMode.Byte, Skip = AwkRegexProviderGaps.SkipReason)]
    internal void PublicCapturesKeepProviderCoordinatesAndFundedText(
AwkStringUnitMode units)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        var runtime = Runtime(counters,limits,units);
        var regex = runtime.Create(new AwkRegexPattern("(😀|😀😀)"));
        var input = AwkText.FromString(new string('z', 2000) + "😀😀", counters);

        using (var leased = runtime.MatchCaptureValue(regex, input, governor))
        {
            var match = leased.Match;
            Assert.Equal(2, match.Groups.Count);
            Assert.Equal(2000, match.Index);
            Assert.Equal(units == AwkStringUnitMode.Byte ? 8 : 4, match.Length);
            Assert.Equal(2000, match.ByteIndex);
            Assert.Equal(8, match.ByteLength);
            Assert.Equal(match.Index, match.Groups[1].Index);
            Assert.Equal(match.Length, match.Groups[1].Length);
            Assert.Equal("😀😀", match.Groups[0].Value.AsString());
            Assert.True(governor.CurrentCommittedBytes > 0);
        }
        runtime.Dispose();
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(2, counters.RegexGroupsMaterialized);
    }

    [Fact]
    internal void CaptureFailurePreflightsAllPublicGroups()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(4096, counters);
        var governor = memoryForGovernor.Accounting;
        var runtime = Runtime(counters,limits);
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));

        memoryForGovernor.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            runtime.MatchCaptureValue(regex, AwkText.FromString("aaa"), governor));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact(Skip = AwkRegexProviderGaps.SkipReason)]
    public void ProviderSelectionKeepsPublicGroupNumbers()
    {
        var runtime = Runtime(AwkRuntimeCounters.Disabled,            AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));
        using var match = runtime.MatchValue(regex, AwkText.FromString("aa"),
            AwkMemoryAccounting.Disabled, AwkRegexCapturePlan.All);

        Assert.Equal(2, match.Match.Groups.Count);
        Assert.Equal("aa", match.Match.Value.AsString());
    }

    [Fact]
    public void CancellationPrecedesProviderCalls()
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, cancellation.Token.ThrowIfCancellationRequested,
            counters, regexEngine: AwkUtf8RegexEngine.Instance);
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => runtime.MatchSpan(regex, AwkText.FromString("aa")));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
    }

    private static AwkRegexRuntime Runtime(AwkRuntimeCounters counters,        AwkRuntimeLimits limits,AwkStringUnitMode units = AwkStringUnitMode.Utf8Rune) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, units, static () => { }, counters,             new AwkRuntimeResourceMonitor(limits, counters), regexEngine: AwkUtf8RegexEngine.Instance);
}
