using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexPreparationCancellationTests
{
    [Theory]
    [InlineData("plain")]
    [InlineData("bracket")]
    [InlineData("literal")]
    public void LongPreparationCanBeCanceledAndRetried(string shape)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(counters,() =>
        {
            if (armed && ++checks == 3) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var text = new string('a', 8192);
        if (shape == "bracket") text = "[" + text + "]";
        AwkRegex Create() => shape == "literal"
            ? runtime.CreateLiteral(new AwkRegexLiteralText(text))
            : runtime.Create(new AwkRegexPattern(text));
        var error = Assert.Throws<OperationCanceledException>(() => Create());
        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        var retried = Create();
        Assert.Same(retried, Create());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationAtFinalPublicationDoesNotCacheTheConstruction(bool literal)
    {
        var total = 0;
        var probe = Runtime(AwkRuntimeCounters.Disabled,() => total++);
        _ = Create(probe);
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(counters,() =>
        {
            if (armed && ++checks == total) cancellation.Cancel();
            if (armed) cancellation.Token.ThrowIfCancellationRequested();
        });
        var error = Assert.Throws<OperationCanceledException>(() => Create(runtime));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, counters.RegexCompiles);
        armed = false;
        var retried = Create(runtime);
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Same(retried, Create(runtime));
        Assert.Equal(2, counters.RegexCompiles);

        AwkRegex Create(AwkRegexRuntime target) => literal
            ? target.CreateLiteral(new AwkRegexLiteralText("a"))
            : target.Create(new AwkRegexPattern("a"));
    }

    [Theory]
    [InlineData("translation", "plain")]
    [InlineData("translation", "posix")]
    [InlineData("translation", "collating")]
    [InlineData("translation", "trailingescape")]
    public void OwnedPreparationWalksObserveCancellation(string stage, string shape)
    {
        var text = Pattern(shape);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var error = Assert.Throws<OperationCanceledException>(() => Prepare(() =>
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Prepare(null);

        void Prepare(Action? checkpoint)
        {
            switch (stage)
            {
                case "translation": _ = AwkRegex.TranslatePattern(new AwkRegexPattern(text),
                    checkpoint: checkpoint); break;
                default: throw new ArgumentException("Unknown preparation stage.");
            }
        }
    }

    [Theory]
    [InlineData("[:alnum:]", @"\p{L}\p{Nd}")]
    [InlineData("[:alpha:]", @"\p{L}")]
    [InlineData("[:blank:]", @" \t")]
    [InlineData("[:cntrl:]", @"\p{Cc}")]
    [InlineData("[:digit:]", "0-9")]
    [InlineData("[:graph:]", @"\p{L}\p{M}\p{N}\p{P}\p{S}\p{Cf}\p{Co}")]
    [InlineData("[:lower:]", @"\p{Ll}")]
    [InlineData("[:print:]", @"\p{L}\p{M}\p{N}\p{P}\p{S}\p{Cf}\p{Co}\p{Zs}")]
    [InlineData("[:punct:]", @"\p{P}\p{S}")]
    [InlineData("[:space:]", @"\s")]
    [InlineData("[:upper:]", @"\p{Lu}")]
    [InlineData("[:xdigit:]", "0-9A-Fa-f")]
    public void PosixClassTranslationPreservesTokensCrossingSearchWindows(string token, string replacement)
    {
        var prefix = new string('a', AwkRegexConstruction.ChunkCharacters - 2);
        var suffix = new string('b', AwkRegexConstruction.ChunkCharacters * 2);
        var text = "[" + prefix + token + token + suffix + "]";
        var expected = "[" + prefix + replacement + replacement + suffix + "]";
        foreach (var mode in new[] { AwkCompatibilityMode.GnuDefault, AwkCompatibilityMode.Posix,
            AwkCompatibilityMode.Traditional })
        {
            Assert.Equal(expected, AwkRegex.TranslatePattern(new AwkRegexPattern(text), mode, () => { }).AsString());
            Assert.Equal(expected, AwkRegex.TranslatePattern(new AwkRegexPattern(text), mode).AsString());
        }
    }

    [Fact]
    public void CancelingLeasedDecodingReleasesScratchBeforeRetry()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, () =>
            {
                if (armed && governor.CurrentCommittedBytes > 0 && ++checks == 6) cancellation.Cancel();
                if (armed) cancellation.Token.ThrowIfCancellationRequested();
            }, counters,             new AwkRuntimeResourceMonitor(limits, counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
        var pattern = new AwkRegexPattern(string.Concat(Enumerable.Repeat(@"\x61", 4096)));
        var error = Assert.Throws<OperationCanceledException>(() => runtime.Create(pattern));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        armed = false;
        var retried = runtime.Create(pattern);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Same(retried, runtime.Create(pattern));
        Assert.Equal(2, counters.RegexCompiles);
    }

    [Fact]
    public void CompletedRegexDoesNotRetainItsConstructionCheckpoint()
    {
        var armed = false;
        void Checkpoint()
        {
            if (armed) throw new InvalidOperationException("Construction callback was retained.");
        }
        var regex = AwkRegex.Create(new AwkRegexPattern("a"), AwkRegexCaseSensitivity.CaseSensitive,
            checkpoint: Checkpoint);
        armed = true;
        using var runtime = Runtime(AwkRuntimeCounters.Disabled,static () => { });
        Assert.True(runtime.IsMatch(regex, AwkText.FromString("a")));
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("negated")]
    [InlineData("posix")]
    public void FinalTranslationCancellationReturnsNoCompletedPattern(string shape)
    {
        var text = Pattern(shape);
        var total = 0;
        var expected = AwkRegex.TranslatePattern(new AwkRegexPattern(text), checkpoint: () => total++);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var error = Assert.Throws<OperationCanceledException>(() =>
            AwkRegex.TranslatePattern(new AwkRegexPattern(text), checkpoint: () =>
            {
                if (++checks == total) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(expected, AwkRegex.TranslatePattern(new AwkRegexPattern(text), checkpoint: () => { }));
    }

    private static string Pattern(string shape)
    {
        var text = new string('a', 16384);
        return shape switch
        {
            "plain" => text,
            "bracket" => "[" + text + "]",
            "negated" => "[^" + text + "]",
            "unclosed" => "[" + text,
            "category" => @"\p{" + text + "}",
            "named" => "(?<" + text + ">a)",
            "options" => "(?" + new string('i', 16384) + ":a)",
            "pairs" => string.Concat(Enumerable.Repeat("\U00010400", 8192)),
            "posix" => "[" + text + "[:digit:]]",
            "collating" => "[" + text + "[.x.]]",
            "trailingescape" => text + @"\q",
            _ => throw new ArgumentException("Unknown pattern shape.")
        };
    }

    private static AwkRegexRuntime Runtime(AwkRuntimeCounters counters,Action checkpoint) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint, counters, regexEngine: AwkUtf8RegexEngine.Instance);
}
