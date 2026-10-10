namespace Lokad.Awk.Tests;

public sealed class AwkRegexScalarMemoryTests
{
    [Theory]
    [InlineData(false, ".")]
    [InlineData(true, ".")]
    [InlineData(false, "()")]
    [InlineData(true, "()")]
    [InlineData(false, "\U0001F600")]
    [InlineData(true, "\U0001F600")]
    [InlineData(false, "[^a]")]
    [InlineData(true, "[^a]")]
    [InlineData(false, "[A]")]
    [InlineData(true, "[A]")]
    [InlineData(false, "[à]")]
    [InlineData(true, "[à]")]
    public void RuntimeRejectsUnadmittedScalarPreparationAndAllowsFreshRetry(bool utf8, string pattern)
    {
        var governor = Governor(1);
        var runtime = Runtime(utf8, governor);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => runtime.Create(new AwkRegexPattern(pattern)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        var admitted = Governor(1 << 24);
        var retry = Runtime(utf8, admitted);
        var regex = retry.Create(new AwkRegexPattern(pattern));
        Assert.Same(regex, retry.Create(new AwkRegexPattern(pattern)));
        Assert.True(admitted.PeakLiveBytes > 0);
        Assert.Equal(0, admitted.CurrentCommittedBytes);
        Assert.Equal(0, admitted.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("()")]
    [InlineData("[A]")]
    [InlineData("[aA]")]
    [InlineData("[à]")]
    [InlineData("[^à]")]
    [InlineData("[\U00010001-\U00010FFE]")]
    [InlineData(@"\p{L}")]
    [InlineData(@"\P{Lu}")]
    [InlineData(@"[\p{L}\p{N}]")]
    [InlineData(@"[^\w\d]")]
    [InlineData(@"[\p{Unknown}]")]
    [InlineData(".[]")]
    [InlineData(".[")]
    public void AdmittedTranslationPreservesResultsAndRetainsOnlyExactOutput(string pattern)
    {
        var expected = AwkRegexScalarPattern.Translate(pattern);
        var governor = Governor(1 << 24);
        using (var resultText = governor.CreateTransientLease())
        {
            resultText.ReserveAdditional(43);
            var actual = AwkRegexScalarPattern.Translate(pattern, () => { }, governor, resultText);
            Assert.Equal(expected, actual);
            var outputBytes = ReferenceEquals(actual, pattern) ? 0 : 24L + 2L * actual.Length;
            Assert.Equal(43 + outputBytes, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
        // Warm/cold metadata uses the same conservative admission, so an exact
        // peak budget succeeds and a one-byte-short budget fails before publication.
        var peak = governor.PeakLiveBytes - 43;
        var exact = Governor(peak);
        Assert.Equal(expected, AwkRegexScalarPattern.Translate(pattern, () => { }, exact));
        Assert.Equal(0, exact.CurrentCommittedBytes);
        var shortGovernor = Governor(peak - 1);
        Assert.Throws<AwkRuntimeException>(() =>
            AwkRegexScalarPattern.Translate(pattern, () => { }, shortGovernor));
        Assert.Equal(0, shortGovernor.CurrentCommittedBytes);
        Assert.Equal(0, shortGovernor.CurrentReservedBytes);
    }

    [Fact]
    public void EveryCategoryAndMergedScalarRangesStayWithinAdmission()
    {
        string[] categories = ["L", "Lu", "Ll", "Lt", "Lm", "Lo", "M", "Mn", "Mc", "Me",
            "N", "Nd", "Nl", "No", "P", "Pc", "Pd", "Ps", "Pe", "Pi", "Pf", "Po",
            "S", "Sm", "Sc", "So", "Z", "Zs", "Zl", "Zp", "C", "Cc", "Cf", "Cs", "Co", "Cn"];
        foreach (var category in categories)
        {
            foreach (var negated in new[] { false, true })
            {
                var pattern = ".[" + (negated ? "^" : "") + @"\p{" + category +
                    "}\U00010001-\U00010801\U00010400-\U00010FFE\U00010000-\U0010FFFF]$()";
                var governor = Governor(1 << 24);
                Assert.Equal(AwkRegexScalarPattern.Translate(pattern),
                    AwkRegexScalarPattern.Translate(pattern, () => { }, governor));
                Assert.Equal(0, governor.CurrentCommittedBytes);
                Assert.Equal(0, governor.CurrentReservedBytes);
            }
        }
    }

    [Fact]
    public void PlainPatternAndDisabledGovernorDoNotAdmitScalarBuffers()
    {
        var pattern = new string('a', 16384);
        var governor = Governor(1);
        Assert.Same(pattern, AwkRegexScalarPattern.Translate(pattern, () => { }, governor));
        Assert.Equal(0, governor.ReserveCount);
        Assert.Equal(AwkRegexScalarPattern.Translate(@"\p{L}."),
            AwkRegexScalarPattern.Translate(@"\p{L}.", () => { }, AwkMemoryGovernor.Disabled));
        Assert.Equal(0, AwkMemoryGovernor.Disabled.TotalReservedBytes);
    }

    [Theory]
    [InlineData(".")]
    [InlineData(@"[\p{L}\p{N}]")]
    public void CancellationReleasesOwnAdmissionAndPreservesBorrowedText(string pattern)
    {
        var governor = Governor(1 << 24);
        using var resultText = governor.CreateTransientLease();
        resultText.ReserveAdditional(43);
        using var cancellation = new CancellationTokenSource();
        var error = Assert.Throws<OperationCanceledException>(() =>
            AwkRegexScalarPattern.Translate(pattern, () =>
            {
                if (governor.CurrentCommittedBytes > 43) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, governor, resultText));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(43, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(AwkRegexScalarPattern.Translate(pattern),
            AwkRegexScalarPattern.Translate(pattern, () => { }, governor));
        Assert.Equal(43, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RangeSortPreservesCancellationAndAwkDiagnostics(bool resourceFailure)
    {
        var ranges = Enumerable.Range(0, 10000).Reverse()
            .Select(index => new AwkRegexScalarPattern.ScalarRange(0x10000 + index, 0x10000 + index)).ToList();
        using var cancellation = new CancellationTokenSource();
        Exception failure = resourceFailure
            ? new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorMemoryLimit)
            : new OperationCanceledException(cancellation.Token);
        var observed = Record.Exception(() => AwkRegexScalarPattern.SortScalarRanges(ranges, () => throw failure));
        Assert.Same(failure, observed);
        AwkRegexScalarPattern.SortScalarRanges(ranges, () => { });
        Assert.True(ranges.Zip(ranges.Skip(1), (left, right) => left.First <= right.First).All(value => value));
    }

    private static AwkMemoryGovernor Governor(long bytes) =>
        AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(bytes));

    private static AwkRegexRuntime Runtime(bool utf8, AwkMemoryGovernor governor)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        return new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            static () => { }, AwkRuntimeCounters.Disabled,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, AwkRuntimeCounters.Disabled), governor);
    }
}
