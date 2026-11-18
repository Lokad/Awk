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
            static () => { }, AwkTestHarness.CreateRuntimeOptions(counters));
        const string pattern = "^(SKU)([[:alpha:]]+)([0-9]+)$";
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var equalText = new string(pattern.ToCharArray());
        Assert.Same(regex, runtime.Create(new AwkRegexPattern(equalText)));
        var input = AwkText.FromString("skuItem007");
        for (var pass = 0; pass < 4; pass++)
        {
            foreach (var ignoreCase in new[] { false, true })
            {
                sensitivity = ignoreCase ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;
                using var match = runtime.MatchCaptureValue(regex, input);
                Assert.Equal(ignoreCase, match.Match.Success);
                if (ignoreCase)
                {
                    Assert.Equal("sku", match.Match.Groups[1].Value.AsString());
                    Assert.Equal("Item", match.Match.Groups[2].Value.AsString());
                    Assert.Equal("007", match.Match.Groups[3].Value.AsString());
                }
            }
        }
    }

    [Fact]
    internal void RepeatedMatchingUsesThePreparedProviderHandle()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(counters);
        var regex = runtime.Create(new AwkRegexPattern("a+"));
        var input = AwkText.FromString("zaaa");
        var allocations = counters[AwkCounter.TextAllocations];

        Assert.True(runtime.IsMatch(regex, input));

        var first = runtime.MatchSpan(regex, input);
        var second = runtime.MatchSpan(regex, input);

        Assert.Equal(1, first.ByteIndex);
        Assert.Equal(3, first.ByteLength);
        Assert.Equal(first, second);

        Assert.Equal(allocations, counters[AwkCounter.TextAllocations]);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
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
        var runtime = Runtime(counters);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("SKU12abc;,😀😀");

        Assert.True(runtime.MatchSpan(regex, input).Success);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    [Theory]
    [InlineData(AwkStringUnitMode.Utf8Rune, Skip = AwkRegexProviderGaps.SkipReason)]
    [InlineData(AwkStringUnitMode.Byte, Skip = AwkRegexProviderGaps.SkipReason)]
    internal void PublicCapturesKeepProviderCoordinatesAndFundedText(
AwkStringUnitMode units)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20);
        var governor = memoryForGovernor.Accounting;
        using var runtime = Runtime(counters, units, governor);
        var regex = runtime.Create(new AwkRegexPattern("(😀|😀😀)"));
        var committed = governor.CurrentCommittedBytes;
        var input = AwkText.FromString(new string('z', 2000) + "😀😀");

        using (var leased = runtime.MatchCaptureValue(regex, input))
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
            Assert.True(governor.CurrentCommittedBytes > committed);
        }
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        runtime.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);

    }

    [Fact]
    internal void CaptureFailurePreflightsAllPublicGroups()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20);
        var governor = memoryForGovernor.Accounting;
        using var runtime = Runtime(counters, accounting: governor);
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));
        using var search = runtime.OpenSearch(regex, AwkText.FromString("aaa"));
        var committed = governor.CurrentCommittedBytes;
        var allocated = counters[AwkCounter.TextAllocations];

        memoryForGovernor.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            search.MatchValue(0, AwkRegexCapturePlan.All, out _));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(committed, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(allocated, counters[AwkCounter.TextAllocations]);

    }

    [Fact(Skip = AwkRegexProviderGaps.SkipReason)]
    public void ProviderSelectionKeepsPublicGroupNumbers()
    {
        var runtime = Runtime(AwkRuntimeCounters.Disabled);
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));
        using var match = runtime.MatchValue(regex, AwkText.FromString("aa"), AwkRegexCapturePlan.All);

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
            AwkTestHarness.CreateRuntimeOptions(counters));
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => runtime.MatchSpan(regex, AwkText.FromString("aa")));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    private static AwkRegexRuntime Runtime(AwkRuntimeCounters counters, AwkStringUnitMode units = AwkStringUnitMode.Utf8Rune,
        AwkMemoryAccounting? accounting = null) =>
        new(static () => AwkRegexCaseSensitivity.CaseSensitive, units, static () => { },
            AwkTestHarness.CreateRuntimeOptions(counters), accounting);
}
