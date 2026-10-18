using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRegexPreparationCancellationTests
{
    [Theory]
    [InlineData(false, "plain")]
    [InlineData(true, "plain")]
    [InlineData(false, "bracket")]
    [InlineData(true, "bracket")]
    [InlineData(false, "literal")]
    [InlineData(true, "literal")]
    public void LongPreparationCanBeCanceledAndRetried(bool utf8, string shape)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(utf8, counters, () =>
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
        Assert.Equal(0, counters.RegexCompiles);
        armed = false;
        var retried = Create();
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Same(retried, Create());
        Assert.Equal(1, counters.RegexCompiles);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CancellationAtFinalPublicationDoesNotCacheTheConstruction(bool utf8, bool literal)
    {
        var total = 0;
        var probe = Runtime(utf8, AwkRuntimeCounters.Disabled, () => total++);
        _ = Create(probe);
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var checks = 0;
        var armed = true;
        var runtime = Runtime(utf8, counters, () =>
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
    [InlineData("analysis", "plain")]
    [InlineData("analysis", "bracket")]
    [InlineData("analysis", "category")]
    [InlineData("analysis", "named")]
    [InlineData("analysis", "options")]
    [InlineData("analysis", "unclosed")]
    [InlineData("scalar", "plain")]
    [InlineData("scalar", "bracket")]
    [InlineData("scalar", "category")]
    [InlineData("scalar", "negated")]
    [InlineData("scalar", "unclosed")]
    [InlineData("scalar", "pairs")]
    [InlineData("translation", "plain")]
    [InlineData("translation", "posix")]
    [InlineData("translation", "collating")]
    [InlineData("translation", "trailingescape")]
    [InlineData("limits", "plain")]
    [InlineData("weight", "plain")]
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
                case "analysis": _ = AwkRegexSelection.Analyze(text, checkpoint); break;
                case "scalar": _ = AwkRegexScalarPattern.Translate(text, checkpoint); break;
                case "translation": _ = AwkRegex.TranslatePattern(new AwkRegexPattern(text),
                    checkpoint: checkpoint); break;
                case "limits": AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text,
                    long.MaxValue, int.MaxValue, checkpoint); break;
                case "weight": _ = AwkRegexResourceFacts.EstimateCacheBytes(text, checkpoint); break;
                default: throw new ArgumentException("Unknown preparation stage.");
            }
        }
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("pair")]
    [InlineData("malformed")]
    [InlineData("combining")]
    [InlineData("nul")]
    public void PatternByteLimitsPreserveUtf8CountingAtChunkBoundaries(string shape)
    {
        var prefix = new string('a', AwkRegexConstruction.ChunkCharacters - 1);
        var atom = shape switch
        {
            "ascii" => "a",
            "pair" => "\U00010400",
            "malformed" => "\uD800!\uDC00\uD800",
            "combining" => "e\u0301",
            "nul" => "\0",
            _ => throw new ArgumentException("Unknown text shape.")
        };
        var text = prefix + atom + new string('b', AwkRegexConstruction.ChunkCharacters * 2);
        var expected = Encoding.UTF8.GetByteCount(text);
        var calls = 0;
        Assert.Equal(expected, AwkRegexConstruction.Utf8ByteCount(text, () => calls++));
        Assert.True(calls >= 7);
        AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, expected, 0, () => { });
        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, expected - 1, 0, () => { }));
        Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, error.Id);
        Assert.Equal("awk: regular expression pattern limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Theory]
    [InlineData("a(b)", 1)]
    [InlineData("[()]\\(a\\)", 0)]
    [InlineData("(?:a)(b)", 2)]
    public void GroupBreadthAdmissionRemainsTheExistingConstructCount(string tail, int groups)
    {
        var text = new string('a', AwkRegexConstruction.ChunkCharacters - 1) + tail;
        AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, long.MaxValue, groups, () => { });
        Assert.Equal(1024L + 4L * Encoding.UTF8.GetByteCount(text) + 64L * groups,
            AwkRegexResourceFacts.EstimateCacheBytes(text, () => { }));
        if (groups > 0)
        {
            var error = Assert.Throws<AwkRuntimeException>(() =>
                AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, long.MaxValue, groups - 1, () => { }));
            Assert.Equal(AwkDiagnosticId.RuntimeResourceLimit, error.Id);
            Assert.Equal("awk: regular expression group limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
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
            Assert.Equal(expected, AwkRegex.TranslatePattern(new AwkRegexPattern(text), mode, () => { }));
            Assert.Equal(expected, AwkRegex.TranslatePattern(new AwkRegexPattern(text), mode));
        }
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    public void CallerCancellationCannotPoisonSharedUnicodeMetadata(int cancelAt)
    {
        const string pattern = @"\p{L}";
        var expected = AwkRegexScalarPattern.Translate(pattern);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var error = Assert.Throws<OperationCanceledException>(() =>
            AwkRegexScalarPattern.Translate(pattern, () =>
            {
                if (++checks == cancelAt) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(expected, AwkRegexScalarPattern.Translate(pattern, () => { }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelingLeasedDecodingReleasesScratchBeforeRetry(bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var governor = AwkMemoryAccounting.Create(limits, counters);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune, () =>
            {
                if (armed && governor.CurrentCommittedBytes > 0 && ++checks == 6) cancellation.Cancel();
                if (armed) cancellation.Token.ThrowIfCancellationRequested();
            }, counters, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            new AwkRuntimeResourceMonitor(limits, counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
        var pattern = new AwkRegexPattern(string.Concat(Enumerable.Repeat(@"\x61", 4096)));
        var error = Assert.Throws<OperationCanceledException>(() => runtime.Create(pattern));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(1, governor.CommitCount);
        armed = false;
        var retried = runtime.Create(pattern);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Same(retried, runtime.Create(pattern));
        Assert.Equal(2, counters.RegexCompiles);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletedRegexDoesNotRetainItsConstructionCheckpoint(bool utf8)
    {
        var armed = false;
        void Checkpoint()
        {
            if (armed) throw new InvalidOperationException("Construction callback was retained.");
        }
        var regex = utf8 ? AwkRegex.CreateProvided("a", AwkRegexCaseSensitivity.CaseSensitive,
            ["0"], checkpoint: Checkpoint) : AwkRegex.CreateDotNet("a", AwkRegexCaseSensitivity.CaseSensitive,
            checkpoint: Checkpoint);
        armed = true;
        switch (regex.Backend)
        {
            case AwkDotNetRegexBackend dotnet: Assert.Matches(dotnet.Value, "a"); break;
            case AwkProvidedRegexBackend provided: Assert.Equal(["0"], provided.GroupNames); break;
            default: throw new InvalidOperationException("Unknown backend.");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupCountingCanBeCanceledAfterByteMeasurement(bool cacheWeight)
    {
        var text = string.Concat(Enumerable.Repeat("(a)", 8192));
        var measured = 0;
        _ = AwkRegexConstruction.Utf8ByteCount(text, () => measured++);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        void Checkpoint()
        {
            if (++checks == measured + 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            if (cacheWeight) _ = AwkRegexResourceFacts.EstimateCacheBytes(text, Checkpoint);
            else AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, long.MaxValue, 8192, Checkpoint);
        });
        Assert.Equal(cancellation.Token, error.CancellationToken);
        AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(text, long.MaxValue, 8192, () => { });
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

    private static AwkRegexRuntime Runtime(bool utf8, AwkRuntimeCounters counters, Action checkpoint) =>
        new(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Utf8Rune,
            checkpoint, counters, utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility, regexEngine: AwkUtf8RegexEngine.Instance);
}
