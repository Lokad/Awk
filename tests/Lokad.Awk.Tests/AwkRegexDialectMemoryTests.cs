namespace Lokad.Awk.Tests;

public sealed class AwkRegexDialectMemoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QuotedUnmatchedRightParenthesisRequiresAdmittedTranslation(bool utf8)
    {
        var governor = Governor(1);
        var runtime = Runtime(utf8, governor, () => { });
        var error = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern(")")));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var admitted = Governor(1 << 24);
        var retry = Runtime(utf8, admitted, () => { });
        var regex = retry.Create(new AwkRegexPattern(")"));
        Assert.True(retry.IsMatch(regex, AwkText.FromString(")")));
        Assert.Same(regex, retry.Create(new AwkRegexPattern(")")));
        Assert.True(admitted.PeakLiveBytes > 0);
        Assert.Equal(0, admitted.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecodedEreStaysAdmittedThroughFinalWrapperPublication(bool utf8)
    {
        var governor = Governor(1 << 24);
        long heldAtLastCheck = -1;
        var runtime = Runtime(utf8, governor, () => heldAtLastCheck = governor.CurrentCommittedBytes);
        var regex = runtime.Create(new AwkRegexPattern(@"\q"));
        Assert.True(heldAtLastCheck >= 26, $"Final preparation check held {heldAtLastCheck} bytes.");
        Assert.True(runtime.IsMatch(regex, AwkText.FromString("q")));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StructuredPredicateCannotBypassScalarPreparationAdmission(bool ignoreCase)
    {
        const string pattern = @"[\p{L}]";
        var requiredNodes = 2L;
        var oldBudget = 256L + 128L * pattern.Length + 256L + 224L * requiredNodes;
        var governor = Governor(oldBudget);
        var sensitivity = ignoreCase ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkPosixRegex.TryCreate(pattern, sensitivity, governor, () => { }, () => { }, out _));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var retry = Governor(1 << 24);
        Assert.True(AwkPosixRegex.TryCreate(pattern, sensitivity, retry, () => { }, () => { }, out var program));
        Assert.NotNull(program);
        Assert.True(retry.PeakLiveBytes > oldBudget);
        Assert.Equal(0, retry.CurrentCommittedBytes);
        Assert.Equal(0, retry.CurrentReservedBytes);
    }

    public static IEnumerable<object[]> TranslationCases()
    {
        string[] patterns = [")", "a)", @"\q", @"\<a\>", @"\w\W", @"\x61.",
            "[[:alpha:][:digit:]]", "[[:print:][:graph:]]", "[[=x=][.]=.]]", "[[.^.][.=.]]",
            "a[[:print:]]b[[:alpha:]]", @"\134w[[:punct:]]"];
        foreach (var pattern in patterns)
        foreach (var profile in new[] { 0, 1, 2 }) yield return [pattern, profile];
    }

    [Theory]
    [MemberData(nameof(TranslationCases))]
    public void GovernedDialectTranslationPreservesProfilesAndAdmissionAcrossStages(string text, int profile)
    {
        var pattern = new AwkRegexPattern(text);
        var mode = (AwkCompatibilityMode)profile;
        var expected = AwkRegex.TranslatePattern(pattern, mode, () => { });
        var governor = Governor(1 << 24);
        using (var results = governor.CreateTransientLease())
        {
            results.ReserveAdditional(43);
            Assert.Equal(expected, AwkRegex.TranslatePattern(pattern, mode, () => { }, results, governor));
            Assert.True(governor.CurrentCommittedBytes >= 43 + 24 + 2L * expected.Length);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var required = governor.PeakLiveBytes - 43;
        var exact = Governor(required);
        Assert.Equal(expected, AwkRegex.TranslatePattern(pattern, mode, () => { }, governor: exact));
        Assert.Equal(0, exact.CurrentCommittedBytes);
        var shortGovernor = Governor(required - 1);
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkRegex.TranslatePattern(pattern, mode, () => { }, governor: shortGovernor));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        Assert.Equal(0, shortGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistinctEreTextContributesToWrapperCacheWeight(bool utf8)
    {
        const string text = @"\x61.";
        var governor = Governor(1 << 24);
        var probe = Runtime(utf8, governor, () => { }).Create(new AwkRegexPattern(text));
        Assert.NotSame(text, probe.ErePattern);
        Assert.NotSame(probe.Pattern, probe.ErePattern);
        var bytes = AwkRegexResourceFacts.EstimateCacheBytes(text) +
            AwkRegexResourceFacts.EstimateCacheBytes(probe.Pattern) +
            AwkRegexResourceFacts.EstimateCacheBytes(probe.ErePattern);
        AwkRegexRuntime WithCacheBudget(long partition) =>
            new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, () => { },
                AwkRuntimeCounters.Disabled, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
                new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault().WithMaxRegexCacheBytes(partition * 5),
                    AwkRuntimeCounters.Disabled), governor);
        var exact = WithCacheBudget(bytes);
        var cached = exact.Create(new AwkRegexPattern(text));
        Assert.Same(cached, exact.Create(new AwkRegexPattern(text)));
        var shortCache = WithCacheBudget(bytes - 1);
        Assert.NotSame(shortCache.Create(new AwkRegexPattern(text)), shortCache.Create(new AwkRegexPattern(text)));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void LeaseMoveKeepsOneLiveChargeAndReleasesEachOwnerExactlyOnce()
    {
        var governor = Governor(64);
        using var retained = governor.CreateTransientLease();
        using (var scratch = governor.ReserveTransient(64))
        {
            scratch.MoveTo(retained, 24);
            Assert.Equal(64, governor.CurrentCommittedBytes);
            Assert.Equal(64, governor.TotalCommittedBytes);
            Assert.Equal(64, governor.PeakLiveBytes);
            Assert.Equal(1, governor.CommitCount);
        }
        Assert.Equal(24, governor.CurrentCommittedBytes);
        retained.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(64, governor.TotalReleasedCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void InvalidLeaseMovePreservesBothOwners()
    {
        var governor = Governor(64);
        using var source = governor.ReserveTransient(64);
        using var foreign = Governor(100).CreateTransientLease();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(foreign, 24));
        using var target = governor.CreateTransientLease();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(target, 65));
        Assert.Equal(64, governor.CurrentCommittedBytes);
        Assert.Equal(64, governor.TotalCommittedBytes);
        target.Dispose();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(target, 24));
        Assert.Equal(64, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("bracket")]
    public void StructuredEligibilityScanCancelsBeforeAllocatingParsingBuffers(string shape)
    {
        var pattern = new string('a', 8192);
        if (shape == "bracket") pattern = "[" + pattern + "]";
        var governor = Governor(1 << 24);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var error = Assert.Throws<OperationCanceledException>(() =>
            AwkPosixRegex.TryCreate(pattern, AwkRegexCaseSensitivity.CaseSensitive, governor, () => { }, () =>
            {
                if (governor.CurrentCommittedBytes == 0 && ++checks == 3) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, out _));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.ReserveCount);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StructuredProgramRetainsPreparationAndAccountsForExpandedPredicate(bool ignoreCase)
    {
        const string pattern = @"[\p{L}]";
        var governor = Governor(1 << 24);
        var sensitivity = ignoreCase ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;
        using (var results = governor.CreateTransientLease())
        {
            results.ReserveAdditional(43);
            Assert.True(AwkPosixRegex.TryCreate(pattern, sensitivity, governor, () => { }, () => { },
                out var program, results));
            Assert.NotNull(program);
            var scalar = AwkRegexScalarPattern.Translate(pattern);
            Assert.True(program.CacheBytes >= AwkRegexResourceFacts.EstimateCacheBytes(scalar));
            Assert.True(governor.CurrentCommittedBytes >= 43 + 256 + 128L * pattern.Length +
                256 + 224L * 2 + 24 + 2L * scalar.Length);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(governor.TotalCommittedBytes, governor.TotalReleasedCommittedBytes);
    }

    private static AwkMemoryAccounting Governor(long bytes) =>
        AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));

    private static AwkRegexRuntime Runtime(bool utf8, AwkMemoryAccounting governor, Action checkpoint) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint,
            AwkRuntimeCounters.Disabled,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled), governor);
}
