namespace Lokad.Awk.Tests;

public sealed class AwkRegexBracketClassTests
{
    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.DotNetCompatibility)]
    [InlineData(AwkCompatibilityMode.GnuDefault, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Posix, AwkRegexEngineKind.Utf8Adapter)]
    [InlineData(AwkCompatibilityMode.Traditional, AwkRegexEngineKind.Utf8Adapter)]
    internal void InvalidNamedClassFailsBeforePublicationAndDoesNotPoisonRetry(
        AwkCompatibilityMode mode, AwkRegexEngineKind engine)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var governor = AwkMemoryAccounting.Create(limits, counters);
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, engine, new AwkRuntimeResourceMonitor(limits, counters), governor, mode);
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
        Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate(pattern));

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
        AwkRegexBracketClasses.Validate(pattern);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryClassesNeedNoScopeStorage(bool invalid)
    {
        var governor = Governor(1);
        using var lease = governor.CreateTransientLease();
        if (invalid)
            Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate("[[:absent:]]", lease: lease));
        else
            AwkRegexBracketClasses.Validate("(a)([[:digit:]])", lease: lease);
        Assert.Equal(0, governor.PeakLiveBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopeStoragePreflightsAndPreservesExistingLease(bool invalid)
    {
        var pattern = "(?x)(a)[[:" + (invalid ? "absent" : "digit") + ":]]";
        var bytes = AwkRuntimeMemory.EstimateRegexBracketModeArrayBytes((pattern.Length + 7) / 8);
        var shortGovernor = Governor(bytes - 1);
        using (var lease = shortGovernor.CreateTransientLease())
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => AwkRegexBracketClasses.Validate(pattern, lease: lease));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        }
        Assert.Equal(0, shortGovernor.PeakLiveBytes);
        var governor = Governor(bytes + 11);
        using var retained = governor.ReserveTransient(11);
        if (invalid)
            Assert.Throws<ArgumentException>(() => AwkRegexBracketClasses.Validate(pattern, lease: retained));
        else
            AwkRegexBracketClasses.Validate(pattern, lease: retained);
        Assert.Equal(bytes + 11, governor.PeakLiveBytes);
        Assert.Equal(11, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationAfterScopeAdmissionReleasesOnlyScopeStorage(bool afterAllocation)
    {
        var governor = Governor(4096);
        using var retained = governor.ReserveTransient(11);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Check()
        {
            if (governor.CurrentCommittedBytes > 11 && ++checks == (afterAllocation ? 2 : 1))
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => AwkRegexBracketClasses.Validate(
            "(?x)(a)[[:digit:]]", Check, retained));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(11, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        AwkRegexBracketClasses.Validate("(?x)(a)[[:digit:]]", lease: retained);
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
        var failure = Assert.Throws<OperationCanceledException>(() => AwkRegexBracketClasses.Validate(pattern, Check));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptureNumbersAndByteNativeSymbolMatchingRemainUnchanged(bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, counters, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters));
        var captured = runtime.Create(new AwkRegexPattern("([[:digit:]])([[:alpha:]])"));
        Assert.Equal(3, captured.GroupCount);
        var punctuation = runtime.Create(new AwkRegexPattern("[[:punct:]]"));
        Assert.Equal(1, punctuation.GroupCount);
        Assert.True(runtime.IsMatch(punctuation, AwkText.FromString("$", counters)));
        Assert.False(runtime.IsMatch(punctuation, AwkText.FromString("A", counters)));
        Assert.Equal(utf8 ? 0 : 2, counters.StringDecodes);
        Assert.Equal(utf8 ? 0 : 2, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
        Assert.Equal(utf8 ? 0 : 2, counters.RegexDotNetTextMatches);
        Assert.Equal(2, counters.RegexMatches);
        Assert.Equal(2, counters.RegexCompiles);
    }

    private static AwkMemoryAccounting Governor(long bytes) =>
        AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));
}
