using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceCommandLineTests : AwkCurrentSurfaceTestBase
{
    [Theory]
    [InlineData("awk")]
    [InlineData("/usr/bin/awk")]
    [InlineData(@"C:\tools\awk")]
    public void Awk_TryParseAcceptsAwkCommandBasename(string command)
    {
        var tool = Awk.TryParse(BuildInvocation(command, "BEGIN{print 1}"));

        Assert.IsType<Awk>(tool);
    }

    [Theory]
    [InlineData("gawk")]
    [InlineData("/usr/bin/gawk")]
    [InlineData(@"C:\tools\gawk")]
    public void Awk_TryParseRejectsOtherCommandBasenames(string command)
    {
        var tool = Awk.TryParse(BuildInvocation(command, "BEGIN{print 1}"));

        Assert.Null(tool);
    }

    [Theory]
    [InlineData("field-sep", "field-separator")]
    [InlineData("assi", "assign")]
    [InlineData("ver", "version")]
    public void AwkOptions_ResolvesUniqueLongOptionAbbreviations(string abbreviation, string expected)
    {
        Assert.True(AwkOptions.TryResolveLongOptionName(abbreviation, out var option));

        Assert.Equal(expected, option.LongName);
    }

    [Theory]
    [InlineData("p")]
    [InlineData("l")]
    public void AwkOptions_RejectsAmbiguousLongOptionAbbreviations(string abbreviation)
    {
        Assert.False(AwkOptions.TryResolveLongOptionName(abbreviation, out _));
    }

    [Theory]
    [InlineData('F', "FieldSeparator", "Required", "AttachedOrRaw")]
    [InlineData('f', "AddSourceFile", "Required", "Raw")]
    [InlineData('L', "Lint", "Optional", "AttachedOrRaw")]
    [InlineData('W', "ExpandWOption", "Required", "AttachedOrRaw")]
    public void AwkOptions_ResolvesShortOptionMetadata(
        char optionName,
        string expectedAction,
        string expectedCardinality,
        string expectedPlacement)
    {
        Assert.True(AwkOptions.TryResolveShortOption(optionName, out var option));

        Assert.Equal(expectedAction, option.Action.ToString());
        Assert.Equal(expectedCardinality, option.ValueCardinality.ToString());
        Assert.Equal(expectedPlacement, option.ShortValuePlacement.ToString());
    }

    [Fact]
    public void AwkOptions_ResolvesWCompatAlias()
    {
        Assert.True(AwkOptions.TryResolveWOptionName("compat", out var option));

        Assert.Equal(AwkOptionAction.Traditional, option.Action);
        Assert.Equal(AwkOptionValueCardinality.None, option.ValueCardinality);
    }

    [Fact]
    public async Task Awk_SourceStringLiteralsArePreEncodedForRepeatedExecution()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print \"café\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("a\nb\nc\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("café\ncafé\ncafé\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void AwkLintMode_OnlyContainsDistinctRuntimeModes()
    {
        Assert.Equal(new[] { "Off", "Normal", "Fatal" }, Enum.GetNames<AwkLintMode>());
    }

    [Fact]
    public void AwkLintCompatibilityAliasesNormalizeToNormal()
    {
        AssertLintMode(AwkLintMode.Normal, "--lint=invalid");
        AssertLintMode(AwkLintMode.Normal, "--lint=no-ext");
        AssertLintMode(AwkLintMode.Normal, "--lint-old");
        AssertLintMode(AwkLintMode.Normal, "-t");
        AssertLintMode(AwkLintMode.Fatal, "--lint=fatal");
    }

    [Fact]
    public void AwkNoOpCompatibilityOptions_DoNotAlterExecutionOptions()
    {
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation(
            "awk",
            "--re-interval",
            "--use-lc-numeric",
            "--no-optimize",
            "--sandbox",
            "BEGIN{print 1}"));

        Assert.Equal(AwkCompatibilityMode.GnuDefault, invocation.ExecutionOptions.CompatibilityProfile.Mode);
        Assert.Equal(
            AwkStringUnitMode.Utf8Rune,
            invocation.ExecutionOptions.CompatibilityProfile.StringUnitMode);
        Assert.False(invocation.ExecutionOptions.CompatibilityProfile.NonDecimalData);
        Assert.Equal(AwkLintMode.Off, invocation.ExecutionOptions.CompatibilityProfile.LintMode);
    }

    [Fact]
    public async Task Awk_SupportsLongOptionsForFieldSeparatorFileAndAssign()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{print prefix} {print $2}");
        fileSystem.AddFile("/file.txt", "a:b\nc:d\n");

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "--field-separator",
            ":",
            "--assign=prefix=SKU",
            "--file",
            "/script.awk",
            "/file.txt")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("SKU\nb\nd\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    private static void AssertLintMode(AwkLintMode expected, params string[] arguments)
    {
        var allArguments = new string[arguments.Length + 1];
        for (var index = 0; index < arguments.Length; index++)
        {
            allArguments[index] = arguments[index];
        }

        allArguments[^1] = "BEGIN{print 1}";
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", allArguments));

        Assert.Equal(expected, invocation.ExecutionOptions.CompatibilityProfile.LintMode);
    }

    [Fact]
    public async Task Awk_SupportsVariableAssignmentOption()
    {
        var fileSystem = new MockFileSystem();

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "-v",
            "prefix=SKU",
            "BEGIN{print prefix}")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("SKU\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_HelpPrintsUsage()
    {
        var fileSystem = new MockFileSystem();

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "--help")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Contains("Usage: awk", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Contains("--assign", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_LoadsScriptFromFile()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/script.awk", "BEGIN{print \"start\"} {print $2}");
        fileSystem.AddFile("/file.txt", "1 one\n2 two\n");

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "-f", "/script.awk", "/file.txt")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("start\none\ntwo\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_LoadsRepeatedScriptFilesInOrder()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/begin.awk", "BEGIN{print \"start\"}");
        fileSystem.AddFile("/main.awk", "{print $2}");
        fileSystem.AddFile("/file.txt", "1 one\n2 two\n");

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation(
            "awk",
            "-f",
            "/begin.awk",
            "-f",
            "/main.awk",
            "/file.txt")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("start\none\ntwo\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_CompiledProgramExecutesAgainstFreshHosts()
    {
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print $2}", "/file.txt"),
            CancellationToken.None);
        var firstRun = new MockFileSystem();
        firstRun.AddFile("/file.txt", "a one\nb two\n");
        var secondRun = new MockFileSystem();
        secondRun.AddFile("/file.txt", "c three\n");

        var firstExitCode = await compiled.ExecuteAsync(firstRun, CancellationToken.None);
        var secondExitCode = await compiled.ExecuteAsync(secondRun, CancellationToken.None);

        Assert.Empty(compiled.Warnings);
        Assert.Equal(AwkExitCode.Success, firstExitCode);
        Assert.Equal("one\ntwo\n", firstRun.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(AwkExitCode.Success, secondExitCode);
        Assert.Equal("three\n", secondRun.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_CompiledProgramSupportsConcurrentExecutions()
    {
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{RS=\"|\"} {sum += $1} END{print sum, NR}", "/file.txt"),
            CancellationToken.None);
        using var readBarrier = new Barrier(participantCount: 2);
        var firstRun = new MockFileSystem
        {
            AfterReadLines = () => Assert.True(readBarrier.SignalAndWait(TimeSpan.FromSeconds(10)))
        };
        firstRun.AddFile("/file.txt", "1|2|");
        var secondRun = new MockFileSystem
        {
            AfterReadLines = () => Assert.True(readBarrier.SignalAndWait(TimeSpan.FromSeconds(10)))
        };
        secondRun.AddFile("/file.txt", "10|20|30|");

        var executions = await Task.WhenAll(
            Task.Run(() => compiled.ExecuteAsync(firstRun, CancellationToken.None)),
            Task.Run(() => compiled.ExecuteAsync(secondRun, CancellationToken.None)));

        Assert.Equal([AwkExitCode.Success, AwkExitCode.Success], executions);
        Assert.Equal("3 2\n", firstRun.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("60 3\n", secondRun.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_RuntimeCountersAreOptInAndTrackCoreRuntimeEvents()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print $2; if ($0 ~ \"a\") print length($0)}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", "alpha beta\ncarrot delta\n");

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.False(AwkRuntimeOptions.CreateSystemDefault().Counters.IsEnabled);
        Assert.Equal(0, AwkRuntimeOptions.CreateSystemDefault().Counters.RecordsRead);
        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("beta\n10\ndelta\n12\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.RecordsRead);
        Assert.Equal(24, counters.BytesRead);
        Assert.Equal(2, counters.FieldSplits);
        Assert.Equal(4, counters.FieldsMaterialized);
        Assert.True(counters.ValuesCreated > 0);
        Assert.True(counters.StringDecodes > 0);
        Assert.True(counters.RecordSeparatorResolutions > 0);
        Assert.Equal(17, counters.OutputBytes);
        Assert.Equal(1, counters.RegexCompiles);
        Assert.True(counters.RegexMatches >= 2);
        Assert.True(counters.RegexDotNetTextMatches >= 2);
        Assert.True(counters.RegexStringAdapterMatches >= 2);
        Assert.True(counters.HostCalls >= 6);
        Assert.True(counters.ExecutionSteps > 0);
        Assert.True(counters.CancellationChecks > 0);
        Assert.True(counters.AwkTextAsStringCalls >= counters.RegexDotNetTextMatches);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
    }

    [Fact]
    public void Awk_RuntimeOptionsDefaultRegexEngineKeepsDotNetCompatibility()
    {
        var options = AwkRuntimeOptions.CreateSystemDefault();

        Assert.Equal(AwkRegexEngineKind.DotNetCompatibility, options.RegexEngineKind);
    }

    [Fact]
    public void Awk_RuntimeOptionsDefaultExecutionEngineKeepsInterpreter()
    {
        var options = AwkRuntimeOptions.CreateSystemDefault();

        Assert.Equal(AwkExecutionEngineKind.Interpreter, options.ExecutionEngineKind);
    }

    [Fact]
    public async Task Awk_RuntimeCountersTrackFormattingFallbacks()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{printf \"%s\", sprintf(\"%04d\", 7); print 3.5}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("00073.5\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.FormattingFallbacks);
        Assert.Equal(3, counters.FormattingStringInteropInputs);
        Assert.Equal(2, counters.Utf8Encodes);
        Assert.True(counters.AwkTextAsStringCalls <= counters.FormattingStringInteropInputs);
    }

    [Fact]
    public async Task Awk_RuntimeCountersTrackByteTextComparisons()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{x=\"café\"; y=\"caff\"; print (x > y)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.ByteTextComparisons);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public async Task Awk_ConcatStaysByteNativeForTextAndIntegerValues()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{x=\"café\"; y=\" résumé\"; print x y 123}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("café résumé123\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_RuntimeCountersTrackStringInteropFallbacks()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation(
                "awk",
                "BEGIN{print tolower(\"CAFÉ\"); print toupper(\"café\"); print strftime(\"%Y\", 0, 1); print mktime(\"1970 01 01 00 00 00\", 1)}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("café\nCAFÉ\n1970\n0\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(4, counters.StringInteropFallbacks);
        Assert.True(counters.AwkTextAsStringCalls >= 4);
    }

    [Fact]
    public async Task Awk_AsciiCasingStaysByteNative()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{print tolower(\"ABC-xyz\"); print toupper(\"abc-XYZ\")}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("abc-xyz\nABC-XYZ\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_RuntimeCountersTrackMemoryGovernorOperations()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "BEGIN{print \"abc\"}"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal("abc\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.MemoryReserveCalls > 0);
        Assert.True(counters.MemoryCommitCalls > 0);
        Assert.True(counters.MemoryReleaseCommittedCalls > 0);
        Assert.True(counters.MemoryPeakLiveBytes > 0);
    }

    [Fact]
    public async Task Awk_PrintOutputStaysByteNativeForLargeAndManySmallRecords()
    {
        var inputBuilder = new StringBuilder();
        inputBuilder.Append(new string('x', 4096));
        inputBuilder.Append('\n');
        inputBuilder.Append("café résumé\n");
        for (var index = 0; index < 64; index++)
        {
            inputBuilder.Append("small-");
            inputBuilder.Append(index);
            inputBuilder.Append('\n');
        }

        var input = inputBuilder.ToString();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            BuildInvocation("awk", "{print $0}", "/file.txt"),
            CancellationToken.None);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/file.txt", input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exitCode);
        Assert.Equal(input, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(66, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(Encoding.UTF8.GetByteCount(input), counters.OutputBytes);
    }

    [Fact]
    public async Task Awk_MissingScriptFileReturnsTwo()
    {
        var fileSystem = new MockFileSystem();

        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", "-f", "/missing.awk")));
        var exitCode = await tool.ExecuteAsync(fileSystem, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal("awk: /missing.awk: No such file or directory\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

}
