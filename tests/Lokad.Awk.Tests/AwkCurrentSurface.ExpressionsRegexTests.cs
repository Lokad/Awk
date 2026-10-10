using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceExpressionsRegexTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public void Awk_RegexTranslationLeavesLargeUnclosedBracketPrefixUntouched()
    {
        var pattern = string.Concat(Enumerable.Repeat("[:", 16_384));

        var translated = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern));

        Assert.Same(pattern, translated);
    }

    [Theory]
    [InlineData("[[:alpha:]]", "[\\p{L}]")]
    [InlineData("[[=a=][.b.]]", "[ab]")]
    public void Awk_RegexTranslationRecognizesClosedPosixBracketTokens(
        string pattern,
        string expected)
    {
        var translated = AwkRegex.TranslatePattern(new AwkRegexPattern(pattern));

        Assert.Equal(expected, translated);
    }

    public static IEnumerable<object[]> RegexGoldenCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel == AwkGoldenSupportLevel.CurrentPass &&
                testCase.Tags.Contains(AwkGoldenTag.Regex) &&
                !testCase.Tags.Contains(AwkGoldenTag.CompilerDiagnostic))
            {
                yield return [testCase, false];
                yield return [testCase, true];
            }
        }
    }

    [Fact]
    public void Awk_RegexRuntimeCachesByPatternAndIgnoreCase()
    {
        var caseSensitivity = AwkRegexCaseSensitivity.CaseSensitive;
        var runtime = new AwkRegexRuntime(
            () => caseSensitivity,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            AwkRuntimeCounters.Disabled,
            AwkRegexEngineKind.DotNetCompatibility);

        var first = runtime.Create(new AwkRegexPattern("a+"));
        var second = runtime.Create(new AwkRegexPattern("a+"));
        caseSensitivity = AwkRegexCaseSensitivity.IgnoreCase;
        var ignoreCaseRegex = runtime.Create(new AwkRegexPattern("a+"));
        var ignoreCaseRegexAgain = runtime.Create(new AwkRegexPattern("a+"));

        Assert.Same(first, second);
        Assert.NotSame(first, ignoreCaseRegex);
        Assert.Same(ignoreCaseRegex, ignoreCaseRegexAgain);
    }

    [Fact]
    public void Awk_RegexRuntimeEnumeratesMatchSpansWithoutValueText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            AwkRegexEngineKind.Utf8Adapter);
        var regex = runtime.Create(new AwkRegexPattern("[,;]"));
        var input = AwkText.FromString("alpha,béta;gamma", counters, governor);
        var baselineTextAllocations = counters.AwkTextAllocations;

        using var spans = runtime.EnumerateMatchSpans(regex, input);
        Assert.True(spans.MoveNext(out var first));
        Assert.Equal(5, first.ByteIndex);
        Assert.Equal(1, first.ByteLength);
        Assert.True(spans.MoveNext(out var second));
        Assert.Equal(11, second.ByteIndex);
        Assert.Equal(1, second.ByteLength);
        Assert.False(spans.MoveNext(out _));

        Assert.Equal(baselineTextAllocations, counters.AwkTextAllocations);
        Assert.True(counters.RegexMatches > 0);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Awk_RegexSupplementaryCapturesHaveStrictUtf8AndMeasuredRouting(
        bool useUtf8Adapter,
        bool charactersAsBytes)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            charactersAsBytes ? AwkStringUnitMode.Byte : AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            useUtf8Adapter ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("(.)"));
        var input = AwkText.FromString("😀🦊", counters);
        using var match = runtime.MatchValue(regex, input, AwkMemoryGovernor.Disabled, AwkRegexCapturePlan.All);

        Assert.Equal(4, match.Match.ByteLength);
        Assert.Equal(charactersAsBytes ? 4 : 2, match.Match.Length);
        Assert.All(match.Match.Groups, group =>
            Assert.Equal("😀", new UTF8Encoding(false, true).GetString(group.Value.AsSpan())));
        Assert.Equal(2, counters.RegexGroupsMaterialized);
        Assert.Equal(charactersAsBytes && !useUtf8Adapter ? 2 : 1, counters.RegexCompiles);
        var usesAdapter = useUtf8Adapter || charactersAsBytes;
        Assert.Equal(usesAdapter ? 0 : 1, counters.RegexDotNetTextMatches);
        Assert.Equal(usesAdapter ? 0 : 1, counters.StringDecodes);
        Assert.Equal(0, counters.RegexPatternGapFallbacks);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Awk_RegexRejectsBackendSurrogateSplitsBeforeCaptureAllocation(
        bool useUtf8Adapter,
        bool splitCaptureOnly)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var governor = AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters);
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            useUtf8Adapter ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility);
        // Construct an untranslated backend pattern to exercise the publication guard.
        var pattern = splitCaptureOnly ? @"(\uD83D)(\uDE00)" : @"[\uD800-\uDBFF]";
        var regex = useUtf8Adapter
            ? AwkRegex.CreateUtf8(pattern, AwkRegexCaseSensitivity.CaseSensitive)
            : AwkRegex.CreateDotNet(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        var input = AwkText.FromString("😀");
        var exception = Assert.Throws<AwkRuntimeException>(() =>
            runtime.MatchValue(regex, input, governor, AwkRegexCapturePlan.All));
        if (!splitCaptureOnly)
            Assert.Throws<AwkRuntimeException>(() => runtime.MatchSpan(regex, input));

        Assert.Equal("awk: regular expression splits a Unicode character\n",
            AwkUtf8Codec.Decode(exception.MessageBytes));
        Assert.Equal(0, counters.RegexGroupsMaterialized);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.PeakLiveBytes);
    }

    [Fact]
    public void Awk_DotNetRegexSpanScannerDecodesInputOnce()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("[0-9]"));
        var input = AwkText.FromString(string.Concat(Enumerable.Repeat("é1", 128)), counters);
        var count = 0;

        using var spans = runtime.EnumerateMatchSpans(regex, input);
        while (spans.MoveNext(out var match))
        {
            Assert.Equal((count * 3) + 2, match.ByteIndex);
            Assert.Equal(1, match.ByteLength);
            count++;
        }

        Assert.Equal(128, count);
        Assert.Equal(1, counters.StringDecodes);
        Assert.Equal(1, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public void Awk_RegexPolicyCachesAsciiClassificationOnImmutableText()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Byte,
            static () => { },
            counters,
            AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("z"));
        var input = AwkText.FromString(new string('a', 4_096), counters);

        Assert.False(runtime.IsMatch(regex, input));
        Assert.False(runtime.IsMatch(regex, input));

        Assert.Equal(input.ByteLength, counters.RegexPolicyInputBytesInspected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Awk_CaptureMatchReusesGroupZeroValue(bool useUtf8Adapter)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            useUtf8Adapter
                ? AwkRegexEngineKind.Utf8Adapter
                : AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("(a)"));
        var input = AwkText.FromString("a", counters);
        var baselineTextAllocations = counters.AwkTextAllocations;

        using var leasedMatch = runtime.MatchValue(
            regex,
            input,
            AwkMemoryGovernor.Disabled,
            AwkRegexCapturePlan.All);
        var match = leasedMatch.Match;

        Assert.True(match.Success);
        Assert.Equal(2, match.Groups.Count);
        Assert.Same(match.Groups[0].Value, match.Value);
        Assert.Equal(baselineTextAllocations + 2, counters.AwkTextAllocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Awk_CaptureMatchMaterializesOnlyTheSelectedGroupPrefix(bool useUtf8Adapter)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Utf8Rune,
            static () => { },
            counters,
            useUtf8Adapter
                ? AwkRegexEngineKind.Utf8Adapter
                : AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("(a)(b)(c)(d)"));
        var input = AwkText.FromString("abcd", counters);
        var baselineTextAllocations = counters.AwkTextAllocations;

        using var leasedMatch = runtime.MatchValue(
            regex,
            input,
            AwkMemoryGovernor.Disabled,
            AwkRegexCapturePlan.ThroughGroup(2));
        var match = leasedMatch.Match;

        Assert.True(match.Success);
        Assert.Equal("abcd", match.Value.AsString());
        Assert.Collection(
            match.Groups,
            group => Assert.Equal("abcd", group.Value.AsString()),
            group => Assert.Equal("a", group.Value.AsString()),
            group => Assert.Equal("b", group.Value.AsString()));
        Assert.Equal(3, counters.RegexGroupsMaterialized);
        Assert.Equal(baselineTextAllocations + 3, counters.AwkTextAllocations);
    }

    [Fact]
    public void Awk_DotNetCaptureCoordinatesUseOneByteMappingAcrossLookbehindGroups()
    {
        const string input = "\u00e9x";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var dotNetMatch = Regex.Match(input, "(?<=(\u00e9))x", RegexOptions.CultureInvariant);

        using var leasedMatch = AwkRegexMatch.FromMatch(
            dotNetMatch,
            input,
            byteIndex: 2,
            byteLength: 1,
            AwkRegexCoordinatePolicy.Byte,
            AwkRegexCapturePlan.All,
            AwkRuntimeLimits.Unlimited,
            counters,
            AwkMemoryGovernor.Disabled);
        var match = leasedMatch.Match;

        Assert.True(match.Success);
        Assert.Equal(2, match.Index);
        Assert.Equal(1, match.Length);
        Assert.Collection(
            match.Groups,
            group =>
            {
                Assert.Equal(2, group.Index);
                Assert.Equal(1, group.Length);
                Assert.Equal("x", group.Value.AsString());
            },
            group =>
            {
                Assert.Equal(0, group.Index);
                Assert.Equal(2, group.Length);
                Assert.Equal("\u00e9", group.Value.AsString());
            });
    }

    [Fact]
    public void Awk_RegexLiteralSubstitutionUsesMatchSpans()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new AwkRegexPattern("[[:alpha:]]+"));
        var input = AwkText.FromString("abc123déf", counters, state.MemoryGovernor);
        var replacement = AwkText.FromString("X", counters, state.MemoryGovernor);
        var baselineTextAllocations = counters.AwkTextAllocations;
        var baselineUtf8Encodes = counters.Utf8Encodes;

        var result = AwkRegexSubstitution.ReplaceAll(
            state,
            regex,
            input,
            replacement,
            AwkReplacementMode.Substitution,
            maxReplacements: 0,
            out var replacementCount);

        Assert.Equal(2, replacementCount);
        Assert.Equal("X123X", result.AsString());
        Assert.Equal(baselineTextAllocations + 1, counters.AwkTextAllocations);
        Assert.Equal(baselineUtf8Encodes, counters.Utf8Encodes);
        Assert.True(counters.RegexMatches > 0);
    }

    [Fact]
    public void Awk_DeletionHeavySubstitutionDoesNotReserveInputSizedOutput()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(512);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            limits,
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString(new string('a', 4_096), counters);

        var result = AwkRegexSubstitution.ReplaceAll(
            state,
            regex,
            input,
            AwkText.Empty,
            AwkReplacementMode.Substitution,
            maxReplacements: 0,
            out var replacementCount);

        Assert.Same(AwkText.Empty, result);
        Assert.Equal(4_096, replacementCount);
        Assert.True(state.MemoryGovernor.PeakLiveBytes <= 512);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Awk_MissingSubstitutionOccurrenceReturnsInputWithoutBuildingOutput(
        bool useGensubReplacement)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(4),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var input = AwkText.FromString("a1234", counters, state.MemoryGovernor);
        var replacement = AwkText.FromString("X", counters, state.MemoryGovernor);
        var baselineTextAllocations = counters.AwkTextAllocations;

        var result = AwkRegexSubstitution.ReplaceOccurrence(
            state,
            regex,
            input,
            replacement,
            useGensubReplacement
                ? AwkReplacementMode.Gensub
                : AwkReplacementMode.Substitution,
            occurrence: 2,
            out var replacementCount);

        Assert.Same(input, result);
        Assert.Equal(0, replacementCount);
        Assert.Equal(baselineTextAllocations, counters.AwkTextAllocations);
    }

    [Theory]
    [MemberData(nameof(RegexGoldenCases))]
    public async Task Awk_Utf8RegexAdapterMatchesRegexGoldenCases(AwkGoldenCase testCase, bool useExecutable)
    {
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            useExecutable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter);

        var result = await AwkTestHarness
            .RunGoldenCaseAsync(testCase, CancellationToken.None, options);

        Assert.Equal(testCase.ExpectedExitCode, result.ExitCode);
        Assert.Equal(testCase.ExpectedStdOut, result.StdOut);
        Assert.Equal(testCase.ExpectedStdErr, result.StdErr);
    }

    public static TheoryData<string, string> Utf8RegexAdapterMatrixCases => new()
    {
        {
            "BEGIN{print (\"A9\" ~ /^[[:alnum:]]+$/), (\"a\" ~ /[[=a=]]/), (\"a\" ~ /[[.a.]]/), (\"b\" ~ /[[.a.]]/)}",
            "1 1 1 0\n"
        },
        {
            "BEGIN{start=\"\\\\\" sprintf(\"%c\",96) \"abc\"; end=\"abc\\\\\" sprintf(\"%c\",39); print (\"ab cd\" ~ /\\<cd/), (\"ab\" ~ /ab\\>/), (\"abc\" ~ start), (\"abcx\" ~ end)}",
            "1 1 1 0\n"
        },
        {
            "BEGIN{print match(\"abc\", //), RSTART, RLENGTH; IGNORECASE=1; print (\"Ab\" ~ /^aB$/); IGNORECASE=0; print (\"Ab\" ~ /^aB$/)}",
            "1 1 0\n1\n0\n"
        },
        {
            "BEGIN{if(match(\"caf\u00e912\", /([[:alpha:]]+)([[:digit:]]+)/, m)) print RSTART, RLENGTH, m[1], m[2], m[2,\"start\"], m[2,\"length\"]}",
            "1 6 caf\u00e9 12 5 2\n"
        },
        {
            "BEGIN{s=\"a12 b34\"; gsub(/([[:alpha:]]+)([[:digit:]]+)/,\"<&>\",s); print s}",
            "<a12> <b34>\n"
        }
    };

    [Theory]
    [MemberData(nameof(Utf8RegexAdapterMatrixCases))]
    public async Task Awk_Utf8RegexAdapterCoversTranslationMatrix(string program, string expectedOutput)
    {
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", program),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Awk_IgnoreCaseRegexIsIndependentOfCurrentCulture(bool useUtf8Adapter)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var options = new AwkRuntimeOptions(
                () => new AwkUnixTimestampSeconds(0),
                TimeZoneInfo.Utc,
                new AwkRandomGenerator(),
                AwkRuntimeCounters.Disabled,
                AwkRuntimeLimits.CreateDefault(),
                useUtf8Adapter
                    ? AwkRegexEngineKind.Utf8Adapter
                    : AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter);
            var fileSystem = new MockFileSystem();
            var compiled = await AwkExecutor.CompileAsync(
                fileSystem,
                BuildInvocation("awk", "BEGIN{IGNORECASE=1; print (\"I\" ~ /i/), (\"i\" ~ /I/)}"),
                CancellationToken.None);

            var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

            Assert.Equal(AwkExitCode.Success, exitCode);
            Assert.Equal("1 1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public async Task Awk_CharactersAsBytesRegexUsesByteOffsetsForValidUtf8()
    {
        var fileSystem = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "--characters-as-bytes",
            "BEGIN{s=\"éa\"; print (s ~ /éa/), match(s,/a/), RSTART, RLENGTH; print match(s,/é/,m), RSTART, RLENGTH, m[0], m[0,\"start\"], m[0,\"length\"]}"));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("1 3 3 1\n1 1 2 é 1 2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_RegexPatternRangeSwitchAndDynamicMatchUseAwkTextPolicy()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation(
                "awk",
                "BEGIN{switch(\"éclair\"){case /écl/: print \"switch\"}; p=\"fé\"; print (\"café\" ~ p)} /é/{print \"action\", $0} /dé/,/fin/{print \"range\", $0}",
                "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "abc\ndébut\nmilieu\nfin\néclair\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(
            "switch\n1\naction début\nrange début\nrange milieu\nrange fin\naction éclair\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.RegexMatches > 0);
        Assert.Equal(0, counters.RegexDotNetTextMatches);
        Assert.Equal(1, counters.StringDecodes);
    }

    [Fact]
    public async Task Awk_RangePatternStateDoesNotLeakAcrossRuntimeExecutions()
    {
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation(
            "awk",
            ["/start/,/end/ {print $0}", "/file.txt"],
            []));
        var binding = AwkBinder.Bind(AwkParser.Parse("/start/,/end/ {print $0}"));
        Assert.True(binding.Program.HasProgram);
        var program = binding.Program.Program;

        var firstRun = new MockFileSystem();
        firstRun.AddFile("/file.txt", "start\nstill-active\n");
        var firstExitCode = await AwkRuntime.ExecuteAsync(firstRun, invocation, program, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, firstExitCode);
        Assert.Equal("start\nstill-active\n", firstRun.GetOutput(AwkFileDescriptor.StdOut));

        var secondRun = new MockFileSystem();
        secondRun.AddFile("/file.txt", "before\nend\n");
        var secondExitCode = await AwkRuntime.ExecuteAsync(secondRun, invocation, program, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, secondExitCode);
        Assert.Equal(string.Empty, secondRun.GetOutput(AwkFileDescriptor.StdOut));
    }
}
