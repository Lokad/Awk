namespace Lokad.Awk.Tests;

public sealed class AwkRegexDialectMemoryTests
{
    [Fact]
    public void QuotedUnmatchedRightParenthesisRequiresAdmittedTranslation()
    {
        using var memoryForGovernor = Governor(1);
        var governor = memoryForGovernor.Accounting;
        var runtime = Runtime(governor,() => { });
        var error = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern(")")));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        using var memoryForAdmitted = Governor(1 << 24);
        var admitted = memoryForAdmitted.Accounting;
        var retry = Runtime(admitted,() => { });
        var regex = retry.Create(new AwkRegexPattern(")"));
        Assert.True(retry.IsMatch(regex, AwkText.FromString(")")));
        Assert.Same(regex, retry.Create(new AwkRegexPattern(")")));
        Assert.Equal(0, admitted.CurrentCommittedBytes);
    }

    [Fact]
    public void DecodedEreStaysAdmittedThroughFinalWrapperPublication()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 24, counters);
        var governor = memoryForGovernor.Accounting;
        long heldAtLastCheck = -1;
        var runtime = Runtime(governor,() => heldAtLastCheck = governor.CurrentCommittedBytes);
        var regex = runtime.Create(new AwkRegexPattern(@"\q"));
        Assert.True(heldAtLastCheck >= 26, $"Final preparation check held {heldAtLastCheck} bytes.");
        Assert.True(runtime.IsMatch(regex, AwkText.FromString("q")));
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
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
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 24, counters);
        var governor = memoryForGovernor.Accounting;
        using (var results = governor.CreateTransientLease())
        {
            results.ReserveAdditional(43);
            Assert.Equal(expected, AwkRegex.TranslatePattern(pattern, mode, () => { }, results, governor));
            Assert.True(governor.CurrentCommittedBytes >= 43 + AwkRuntimeMemory.EstimateTextBytes(expected));
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        var required = counters.MemoryPeakLiveBytes - 43;
        using var memoryForExact = Governor(required);
        var exact = memoryForExact.Accounting;
        Assert.Equal(expected, AwkRegex.TranslatePattern(pattern, mode, () => { }, governor: exact));
        Assert.Equal(0, exact.CurrentCommittedBytes);
        using var memoryForShortGovernor = Governor(required - 1);
        var shortGovernor = memoryForShortGovernor.Accounting;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkRegex.TranslatePattern(pattern, mode, () => { }, governor: shortGovernor));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        Assert.Equal(0, shortGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void LeaseMoveKeepsOneLiveChargeAndReleasesEachOwnerExactlyOnce()
    {
        using var memoryForGovernor = Governor(64);
        var governor = memoryForGovernor.Accounting;
        using var retained = governor.CreateTransientLease();
        using (var scratch = governor.ReserveTransient(64))
        {
            scratch.MoveTo(retained, 24);
            Assert.Equal(64, governor.CurrentCommittedBytes);
        }
        Assert.Equal(24, governor.CurrentCommittedBytes);
        retained.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public void InvalidLeaseMovePreservesBothOwners()
    {
        using var memoryForGovernor = Governor(64);
        var governor = memoryForGovernor.Accounting;
        using var source = governor.ReserveTransient(64);
        using var foreignMemory = Governor(100);
        using var foreign = foreignMemory.Accounting.CreateTransientLease();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(foreign, 24));
        using var target = governor.CreateTransientLease();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(target, 65));
        Assert.Equal(64, governor.CurrentCommittedBytes);
        target.Dispose();
        Assert.Throws<InvalidOperationException>(() => source.MoveTo(target, 24));
        Assert.Equal(64, governor.CurrentCommittedBytes);
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes, AwkRuntimeCounters.Disabled);

    private static AwkRegexRuntime Runtime(AwkMemoryAccounting governor,Action checkpoint) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune, checkpoint,
            AwkRuntimeCounters.Disabled,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), AwkRuntimeCounters.Disabled), governor, regexEngine: AwkUtf8RegexEngine.Instance);
}
