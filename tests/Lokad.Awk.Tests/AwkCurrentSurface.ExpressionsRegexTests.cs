using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceExpressionsRegexTests : AwkCurrentSurfaceTestBase
{
    public static IEnumerable<object[]> RegexGoldenCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel == AwkGoldenSupportLevel.CurrentPass &&
                testCase.Tags.Contains(AwkGoldenTag.Regex) &&
                !testCase.Tags.Contains(AwkGoldenTag.CompilerDiagnostic))
            {
                yield return [testCase];
            }
        }
    }

    [Fact]
    public void Awk_RegexRuntimeCachesByPatternAndIgnoreCase()
    {
        var ignoreCase = false;
        var runtime = new AwkRegexRuntime(
            () => ignoreCase,
            static () => false,
            static () => { },
            AwkRuntimeCounters.Disabled,
            AwkRegexEngineKind.DotNetCompatibility);

        var first = runtime.Create(new AwkRegexPattern("a+"));
        var second = runtime.Create(new AwkRegexPattern("a+"));
        ignoreCase = true;
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
            static () => false,
            static () => false,
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

    [Fact]
    public void Awk_DotNetRegexSpanScannerDecodesInputOnce()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => false,
            static () => false,
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
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Awk_CaptureMatchReusesGroupZeroValue(bool useUtf8Adapter)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var runtime = new AwkRegexRuntime(
            static () => false,
            static () => false,
            static () => { },
            counters,
            useUtf8Adapter
                ? AwkRegexEngineKind.Utf8Adapter
                : AwkRegexEngineKind.DotNetCompatibility);
        var regex = runtime.Create(new AwkRegexPattern("(a)"));
        var input = AwkText.FromString("a", counters);
        var baselineTextAllocations = counters.AwkTextAllocations;

        var match = runtime.MatchValue(regex, input, AwkMemoryGovernor.Disabled);

        Assert.True(match.Success);
        Assert.Equal(2, match.Groups.Count);
        Assert.Same(match.Groups[0].Value, match.Value);
        Assert.Equal(baselineTextAllocations + 2, counters.AwkTextAllocations);
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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var regex = state.CreateRegex(new AwkRegexPattern("[[:alpha:]]+"));
        var input = AwkText.FromString("abc123déf", counters, state.MemoryGovernor);
        var baselineTextAllocations = counters.AwkTextAllocations;

        var result = AwkRegexSubstitution.ReplaceAll(
            state,
            regex,
            input,
            "X",
            AwkReplacementMode.Substitution,
            maxReplacements: 0,
            out var replacementCount);

        Assert.Equal(2, replacementCount);
        Assert.Equal("X123X", result.AsString());
        Assert.Equal(baselineTextAllocations + 2, counters.AwkTextAllocations);
        Assert.True(counters.RegexMatches > 0);
    }

    [Theory]
    [MemberData(nameof(RegexGoldenCases))]
    public async Task Awk_Utf8RegexAdapterMatchesRegexGoldenCases(AwkGoldenCase testCase)
    {
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter,
            AwkExecutionEngineKind.Interpreter);

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

        Assert.Equal(0, exitCode);
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

            Assert.Equal(0, exitCode);
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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "--characters-as-bytes",
            "BEGIN{s=\"éa\"; print (s ~ /éa/), match(s,/a/), RSTART, RLENGTH; print match(s,/é/,m), RSTART, RLENGTH, m[0], m[0,\"start\"], m[0,\"length\"]}")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(0, exitCode);
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

        Assert.Equal(0, exitCode);
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

        Assert.Equal(0, firstExitCode);
        Assert.Equal("start\nstill-active\n", firstRun.GetOutput(AwkFileDescriptor.StdOut));

        var secondRun = new MockFileSystem();
        secondRun.AddFile("/file.txt", "before\nend\n");
        var secondExitCode = await AwkRuntime.ExecuteAsync(secondRun, invocation, program, CancellationToken.None);

        Assert.Equal(0, secondExitCode);
        Assert.Equal(string.Empty, secondRun.GetOutput(AwkFileDescriptor.StdOut));
    }
}
