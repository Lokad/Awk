namespace Lokad.Awk.Tests;

public sealed class AwkPosixRegexTests
{
    [Fact]
    public void CancellationDuringProgramConstructionReleasesWorkspace()
    {
        using var cancellation = new CancellationTokenSource();
        var accounting = AwkMemoryAccounting.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(64L << 20));
        var compiling = false;
        var checks = 0;
        void CheckCancellation()
        {
            if (compiling && ++checks == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var error = Assert.Throws<OperationCanceledException>(() => AwkPosixRegex.TryCreate(
            "(a|aa){1,4096}", AwkRegexCaseSensitivity.CaseSensitive, accounting,
            () => compiling = true, CheckCancellation, out _));

        Assert.True(compiling);
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, accounting.CurrentCommittedBytes);
    }

    [Fact]
    public void CancellationInsideMatcherReleasesWorkspace()
    {
        Assert.True(AwkPosixRegex.TryCreate("(a|aa)*b", AwkRegexCaseSensitivity.CaseSensitive,
            AwkMemoryAccounting.Disabled, static () => { }, static () => { }, out var program));
        using var cancellation = new CancellationTokenSource();
        var accounting = AwkMemoryAccounting.Create(
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20));
        var checks = 0;
        void CheckCancellation()
        {
            if (++checks == 2) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var error = Assert.Throws<OperationCanceledException>(() => program.Match(
            new string('a', 100_000), 0, 0, accounting, CheckCancellation));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.Equal(0, accounting.CurrentCommittedBytes);
    }
    [Fact]
    public void DeepGroupsCompileWithoutRecursiveParserOrCompilerCalls()
    {
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4 << 20));
        var compilations = 0;
        var pattern = new string('(', 2048) + "a" + new string(')', 2048);
        Assert.True(AwkPosixRegex.TryCreate(pattern, AwkRegexCaseSensitivity.CaseSensitive,
            governor, () => compilations++, static () => { }, out var program));
        Assert.Equal(2049, program.GroupCount);
        Assert.Equal(1, compilations);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("(a){2147483647}")]
    [InlineData("((a){1000000}){1000000}")]
    public void ExpandedIntervalsFailBeforeAllocatingImpossiblePrograms(string pattern)
    {
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096));
        var compilations = 0;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkPosixRegex.TryCreate(pattern, AwkRegexCaseSensitivity.CaseSensitive,
                governor, () => compilations++, static () => { }, out _));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, compilations);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("(?<named>a|aa)")]
    [InlineData("(?:a|aa)")]
    [InlineData("(a+?)")]
    [InlineData("(a\\Z|a+)")]
    [InlineData("(a\\cA|a+)")]
    [InlineData("[a-z-[ae]]+")]
    public void BackendExtensionsKeepTheirExistingEngine(string pattern)
    {
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1));
        Assert.False(AwkPosixRegex.TryCreate(pattern, AwkRegexCaseSensitivity.CaseSensitive,
            governor, static () => throw new InvalidOperationException("Unexpected compilation"), static () => { }, out _));
    }

    [Fact]
    public void ResumedSearchRetainsFullInputAnchorAndWordContext()
    {
        foreach (var pattern in new[] { "(^a)|(a)", @"(\b(?=\w)a)|(a)" })
        {
            Assert.True(AwkPosixRegex.TryCreate(pattern, AwkRegexCaseSensitivity.CaseSensitive,
                AwkMemoryAccounting.Disabled, static () => { }, static () => { }, out var program));
            using var selected = program.Match("aa", 1, 3, AwkMemoryAccounting.Disabled, static () => { });
            Assert.True(selected.Success);
            Assert.Equal(1, selected.Index);
            Assert.False(selected.Capture(1, "aa").Success);
            Assert.Equal("a", selected.Capture(2, "aa").ValueSpan.ToString());
        }
    }

    [Fact]
    public void MidMatchCancellationReleasesGovernedWorkspace()
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8192);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        var checks = 0;
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, () =>
            {
                if (counters.RegexMatches == 1 && ++checks == 2) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, counters, AwkRegexEngineKind.Utf8Adapter, new AwkRuntimeResourceMonitor(limits, counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
        var regex = runtime.Create(new AwkRegexPattern("(a+)+b|c"));
        Assert.Throws<OperationCanceledException>(() => runtime.MatchSpan(regex, AwkText.FromString(new string('a', 100000))));
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void WholeMatchWorkspaceFailurePreflightsBeforeReservation()
    {
        var governor = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1));
        Assert.True(AwkPosixRegex.TryCreate("(a|aa)", AwkRegexCaseSensitivity.CaseSensitive,
            AwkMemoryAccounting.Disabled, static () => { }, static () => { }, out var program));
        var error = Assert.Throws<AwkRuntimeException>(() => program.Match("aaa", 0, 0, governor, static () => { }));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }
}
