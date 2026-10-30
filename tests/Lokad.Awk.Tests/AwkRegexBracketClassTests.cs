namespace Lokad.Awk.Tests;

public sealed class AwkRegexBracketClassTests
{
    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault)]
    [InlineData(AwkCompatibilityMode.Posix)]
    [InlineData(AwkCompatibilityMode.Traditional)]
    internal void InvalidNamedClassFailsBeforePublicationAndDoesNotPoisonRetry(
        AwkCompatibilityMode mode)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var governor = AwkMemoryAccounting.Disabled;
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, new AwkRuntimeResourceMonitor(limits, counters), governor, mode, regexEngine: AwkUtf8RegexEngine.Instance);
        for (var run = 0; run < 2; run++)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern("[[:word:]]")));
            Assert.Equal(AwkDiagnosticId.RuntimeInvalidRegex, failure.Id);
            Assert.Equal("awk: invalid regular expression\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        var valid = runtime.Create(new AwkRegexPattern("[[:digit:]]"));
        Assert.Same(valid, runtime.Create(new AwkRegexPattern("[[:digit:]]")));
        Assert.True(runtime.IsMatch(valid, AwkText.FromString("3", counters)));
    }

    [Theory]
    [InlineData("[[:ascii:]]")]
    [InlineData("[[:word:]]")]
    [InlineData("[[:Alpha:]]")]
    [InlineData("[[:alpha:][:absent:]]")]
    [InlineData("[^[:absent:]]")]
    [InlineData("[[:]]")]
    [InlineData("[[:alpha]]")]
    [InlineData("(?x:(a))#[[:absent:]]")]
    [InlineData("(?x)(?-x:#[[:absent:]])")]
    public void InvalidClassesRespectLexicalAndScopedCommentContext(string pattern) =>
        Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString(pattern)));

    [Theory]
    [InlineData("[:word:]")]
    [InlineData(@"\[[:word:]]")]
    [InlineData("(?# [[:word:]] )[[:digit:]]")]
    [InlineData("(?x)# [[:word:]]\n[[:digit:]]")]
    [InlineData("(?x:(a)# [[:word:]]\n)[[:digit:]]")]
    [InlineData("(?x:(?-x:(?# [[:word:]] )[[:digit:]]))")]
    [InlineData("(?x:(?-x:[#]))#[[:digit:]]")]
    [InlineData("[][:digit:]]")]
    [InlineData("[[=a=][:digit:]]")]
    [InlineData("[[.a.][:digit:]]")]
    public void ClassLookingTextDoesNotRejectValidContexts(string pattern) =>
        AwkRegexBracketClasses.Validate(AwkText.FromString(pattern));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryClassesNeedNoScopeStorage(bool invalid)
    {
        using var memoryForGovernor = Governor(1);
        var governor = memoryForGovernor.Accounting;
        using var lease = governor.CreateTransientLease();
        if (invalid)
            Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString("[[:absent:]]"), lease: lease));
        else
            AwkRegexBracketClasses.Validate(AwkText.FromString("(a)([[:digit:]])"), lease: lease);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopeStoragePreflightsAndPreservesExistingLease(bool invalid)
    {
        var pattern = "(?x)(a)[[:" + (invalid ? "absent" : "digit") + ":]]";
        var bytes = AwkRuntimeMemory.EstimateRegexBracketModeArrayBytes((pattern.Length + 7) / 8);
        using var memoryForShortGovernor = Governor(bytes - 1);
        var shortGovernor = memoryForShortGovernor.Accounting;
        using (var lease = shortGovernor.CreateTransientLease())
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString(pattern), lease: lease));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        }
        using var memoryForGovernor = Governor(bytes + 11);
        var governor = memoryForGovernor.Accounting;
        using var retained = governor.ReserveTransient(11);
        if (invalid)
            Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString(pattern), lease: retained));
        else
            AwkRegexBracketClasses.Validate(AwkText.FromString(pattern), lease: retained);
        Assert.Equal(11, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationAfterScopeAdmissionReleasesOnlyScopeStorage(bool afterAllocation)
    {
        using var memoryForGovernor = Governor(4096);
        var governor = memoryForGovernor.Accounting;
        using var retained = governor.ReserveTransient(11);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (governor.CurrentCommittedBytes > 11 && ++checks == (afterAllocation ? 2 : 1))
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString(
            "(?x)(a)[[:digit:]]"), Check, retained));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(11, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        AwkRegexBracketClasses.Validate(AwkText.FromString("(?x)(a)[[:digit:]]"), lease: retained);
        Assert.Equal(11, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("prefix")]
    [InlineData("comment")]
    public void LongAdmissionScansObserveCancellation(string shape)
    {
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var text = new string('a', 8192);
        var pattern = shape switch
        {
            "name" => "[[:" + text + ":]]",
            "prefix" => text + "[[:digit:]]",
            _ => "(?x)#" + text + "\n[[:digit:]]"
        };
        void Check()
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => AwkRegexBracketClasses.Validate(AwkText.FromString(pattern), Check));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    [Fact]
    public void CaptureNumbersAndByteNativeSymbolMatchingRemainUnchanged()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters,             new AwkRuntimeResourceMonitor(limits, counters), regexEngine: AwkUtf8RegexEngine.Instance);
        var captured = runtime.Create(new AwkRegexPattern("([[:digit:]])([[:alpha:]])"));
        using var captures = runtime.MatchCaptureValue(captured, AwkText.FromString("7A"), AwkMemoryAccounting.Disabled);
        Assert.Equal(3, captures.Match.Groups.Count);
        Assert.Equal("7", captures.Match.Groups[1].Value.AsString());
        Assert.Equal("A", captures.Match.Groups[2].Value.AsString());
        var punctuation = runtime.Create(new AwkRegexPattern("[[:punct:]]"));
        Assert.True(runtime.IsMatch(punctuation, AwkText.FromString("$", counters)));
        Assert.False(runtime.IsMatch(punctuation, AwkText.FromString("A", counters)));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(3, counters.RegexMatches);
        Assert.Equal(2, counters.RegexCompiles);
    }

    private static CooperativeTestMemory Governor(long bytes) =>
        new CooperativeTestMemory(bytes, AwkRuntimeCounters.Disabled);
}
