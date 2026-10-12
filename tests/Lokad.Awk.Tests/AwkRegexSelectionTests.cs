using System.Threading;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexSelectionTests
{
    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter)]
    internal void EquivalentPatternsAndCaseChangesKeepCompiledCacheIdentity(AwkRegexEngineKind engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var sensitivity = AwkRegexCaseSensitivity.CaseSensitive;
        var runtime = new AwkRegexRuntime(() => sensitivity, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, engine);
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
                using var match = runtime.MatchCaptureValue(regex, input, AwkMemoryGovernor.Disabled, AwkRuntimeLimits.Unlimited);
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

    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter)]
    internal void SelectionIsCachedAndOnlyObservableMatchesNeedIt(AwkRegexEngineKind engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(engine, counters, AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));
        var input = AwkText.FromString("zaaa", counters);
        var allocations = counters.AwkTextAllocations;

        Assert.True(runtime.IsMatch(regex, input));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        var first = runtime.MatchSpan(regex, input);
        var second = runtime.MatchSpan(regex, input);

        Assert.Equal(1, first.ByteIndex);
        Assert.Equal(2, first.ByteLength);
        Assert.Equal(first, second);
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(3, counters.RegexMatches);
        Assert.Equal(2, counters.RegexPatternGapFallbacks);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(1, counters.StringDecodes);
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
        var runtime = Runtime(AwkRegexEngineKind.Utf8Adapter, counters, AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        var input = AwkText.FromString("SKU12abc;,😀😀", counters);

        Assert.True(runtime.MatchSpan(regex, input).Success);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility, AwkStringUnitMode.Utf8Rune)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter, AwkStringUnitMode.Utf8Rune)]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility, AwkStringUnitMode.Byte)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter, AwkStringUnitMode.Byte)]
    internal void InternalCapturesAreHiddenFromCoordinatesAndMemory(
        AwkRegexEngineKind engine, AwkStringUnitMode units)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096)
            .WithMaxRegexCaptureBytes(16);
        var governor = AwkMemoryGovernor.Create(limits, counters);
        var runtime = Runtime(engine, counters, limits, units);
        var regex = runtime.Create(new AwkRegexPattern("(😀|😀😀)"));
        var input = AwkText.FromString(new string('z', 2000) + "😀😀", counters);

        using (var leased = runtime.MatchCaptureValue(regex, input, governor, 16))
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
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(2, counters.RegexGroupsMaterialized);
    }

    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkRegexEngineKind.Utf8Adapter)]
    internal void CaptureFailurePreflightsAllPublicGroups(AwkRegexEngineKind engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096);
        var governor = AwkMemoryGovernor.Create(limits, counters);
        var runtime = Runtime(engine, counters, limits);
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));

        var error = Assert.Throws<AwkRuntimeException>(() =>
            runtime.MatchCaptureValue(regex, AwkText.FromString("aaa"), governor, 3));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public void SelectionCompilationHonorsTheSharedCompilationCeiling()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkRegexEngineKind.Utf8Adapter, counters,
            AwkRuntimeLimits.CreateDefault().WithMaxRegexCompilations(1));
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));

        var error = Assert.Throws<AwkRuntimeException>(() => runtime.MatchSpan(regex, AwkText.FromString("aa")));

        Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, error.Id);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public void ZeroCacheBudgetDoesNotRetainSelectionCompilations()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkRegexEngineKind.Utf8Adapter, counters,
            AwkRuntimeLimits.CreateDefault().WithMaxRegexCacheBytes(1));
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));
        var input = AwkText.FromString("aa");

        Assert.Equal(2, runtime.MatchSpan(regex, input).ByteLength);
        Assert.Equal(2, runtime.MatchSpan(regex, input).ByteLength);
        Assert.Equal(3, counters.RegexCompiles);
    }

    [Fact]
    public void ImplementationGroupsDoNotConsumeTheUserGroupAllowance()
    {
        var runtime = Runtime(AwkRegexEngineKind.DotNetCompatibility, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxRegexGroups(1).WithMaxRegexPatternBytes(6));
        var regex = runtime.Create(new AwkRegexPattern("(a|aa)"));
        using var match = runtime.MatchValue(regex, AwkText.FromString("aa"),
            AwkMemoryGovernor.Disabled, AwkRegexCapturePlan.All);

        Assert.Equal(2, match.Match.Groups.Count);
        Assert.Equal("aa", match.Match.Value.AsString());
    }

    [Fact]
    public void StructuredSelectionAvoidsCatastrophicBacktracking()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = Runtime(AwkRegexEngineKind.Utf8Adapter, counters, AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern("(a+)+b|c"));
        var compiled = AwkLongestRegex.Create(regex, AwkRegexCaseSensitivity.CaseSensitive);
        Assert.Equal(AwkRegex.MatchTimeout, compiled.Regex.MatchTimeout);

        Assert.False(runtime.MatchSpan(regex, AwkText.FromString(new string('a', 100_000))).Success);
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public void SelectionHasOneOperationDeadlineAndAwkDiagnostic()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var checksDuringMatch = 0;
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, () =>
            {
                if (counters.RegexMatches == 1 && ++checksDuringMatch == 2) Thread.Sleep(2100);
            }, counters, AwkRegexEngineKind.Utf8Adapter);
        var regex = runtime.Create(new AwkRegexPattern("(a+)+b|c"));
        var error = Assert.Throws<AwkRuntimeException>(() =>
            runtime.MatchSpan(regex, AwkText.FromString(new string('a', 100_000))));

        Assert.Equal(AwkDiagnosticId.RuntimeRegexTimeout, error.Id);
        Assert.Equal("awk: regular expression match timed out\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public void CancellationPrecedesSelectionOrInputAdaptation()
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, cancellation.Token.ThrowIfCancellationRequested,
            counters, AwkRegexEngineKind.Utf8Adapter);
        var regex = runtime.Create(new AwkRegexPattern("a|aa"));
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => runtime.MatchSpan(regex, AwkText.FromString("aa")));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public void ExistingNamedGroupsCannotCollideWithSelectionGroups()
    {
        var runtime = Runtime(AwkRegexEngineKind.DotNetCompatibility, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault());
        var regex = runtime.Create(new AwkRegexPattern("(?<LokadWhole>a|aa)(?<LokadPrefix>a*)"));
        using var match = runtime.MatchValue(regex, AwkText.FromString("aaa"),
            AwkMemoryGovernor.Disabled, AwkRegexCapturePlan.All);

        Assert.Equal(3, match.Match.Groups.Count);
        Assert.Equal("aaa", match.Match.Value.AsString());
        Assert.Equal("a", match.Match.Groups[1].Value.AsString());
        Assert.Equal("aa", match.Match.Groups[2].Value.AsString());
    }

    private static AwkRegexRuntime Runtime(AwkRegexEngineKind engine, AwkRuntimeCounters counters,
        AwkRuntimeLimits limits, AwkStringUnitMode units = AwkStringUnitMode.Utf8Rune) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, units, static () => { }, counters, engine,
            new AwkRuntimeResourceMonitor(limits, counters));
}
