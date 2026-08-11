using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkGoldenCorpusTests
{
    public static IEnumerable<object[]> DirectCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel == AwkGoldenSupportLevel.CurrentPass ||
                testCase.SupportLevel == AwkGoldenSupportLevel.KnownUnsupported)
            {
                yield return [testCase];
            }
        }
    }

    public static IEnumerable<object[]> ExecutableDirectCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel != AwkGoldenSupportLevel.CurrentPass ||
                testCase.Tags.Contains(AwkGoldenTag.CompilerDiagnostic) ||
                IsSourceLoadingFailure(testCase))
            {
                continue;
            }

            var invocation = AwkCommandParser.Parse(
                AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment));
            if (invocation.TerminalResult.IsTerminal)
                continue;

            yield return [testCase];
        }
    }

    public static IEnumerable<object[]> ExpectedFailingCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel == AwkGoldenSupportLevel.ExpectedFailing)
            {
                yield return [testCase];
            }
        }
    }

    public static IEnumerable<object[]> ParserAcceptanceCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel == AwkGoldenSupportLevel.CurrentPass ||
                IsParserDiagnosticCase(testCase))
            {
                yield return [testCase];
            }
        }
    }

    public static IEnumerable<object[]> ParserParityCases()
    {
        foreach (var testCase in AwkGoldenCorpus.DirectCases)
        {
            if (testCase.SupportLevel != AwkGoldenSupportLevel.ExpectedFailing ||
                IsParserDiagnosticCase(testCase))
            {
                yield return [testCase];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ExecutableDirectCases))]
    public async Task Awk_ExecutableGoldenCaseMatchesWithCounters(AwkGoldenCase testCase)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var result = await AwkTestHarness.RunGoldenCaseAsync(
            testCase,
            CancellationToken.None,
            CreateExecutableOptions(counters));

        Assert.Equal(testCase.ExpectedExitCode, result.ExitCode);
        AssertGoldenText(testCase, result.StdOut);
        Assert.Equal(testCase.ExpectedStdErr, result.StdErr);
        foreach (var expectedFile in testCase.ExpectedFiles)
        {
            Assert.True(
                result.ExpectedFiles.TryGetValue(expectedFile.Path, out var actualContent) && actualContent != null,
                "Expected file was not created: " + expectedFile.Path);
            Assert.Equal(expectedFile.Content, actualContent);
        }

        Assert.True(
            HasExecutableCounterActivity(counters) || IsExpectedNoVmDispatchCase(testCase),
            "Expected executable VM counters for golden case: " + testCase.Name);
    }

    [Theory]
    [MemberData(nameof(DirectCases))]
    public async Task Awk_DirectGoldenCaseMatches(AwkGoldenCase testCase)
    {
        var result = await AwkTestHarness.RunGoldenCaseAsync(testCase, CancellationToken.None);

        Assert.Equal(testCase.ExpectedExitCode, result.ExitCode);
        AssertGoldenText(testCase, result.StdOut);
        Assert.Equal(testCase.ExpectedStdErr, result.StdErr);
        if (testCase.ExpectedStdErr.Length > 0)
        {
            AwkTestHarness.AssertNoClrExceptionLeak(result.StdErr);
        }

        if (testCase.ExpectedStdErr.StartsWith("awk:", StringComparison.Ordinal) &&
            testCase.ExpectedExitCode != 0)
        {
            AwkTestHarness.AssertFriendlyAwkFailure(
                result,
                testCase.ExpectedExitCode,
                testCase.ExpectedStdOut.Length == 0);
        }
        else if (testCase.ExpectedStdErr.StartsWith("awk:", StringComparison.Ordinal))
        {
            AwkTestHarness.AssertFriendlyAwkDiagnostic(result);
        }

        foreach (var expectedFile in testCase.ExpectedFiles)
        {
            Assert.True(
                result.ExpectedFiles.TryGetValue(expectedFile.Path, out var actualContent) && actualContent != null,
                "Expected file was not created: " + expectedFile.Path);
            Assert.Equal(expectedFile.Content, actualContent);
        }
    }

    [Theory]
    [MemberData(nameof(ParserAcceptanceCases))]
    public async Task Awk_GoldenSourcesHaveExpectedParserAcceptance(AwkGoldenCase testCase)
    {
        var fileSystem = AwkTestHarness.BuildFileSystem(testCase);
        var invocation = AwkCommandParser.Parse(
            AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment));
        if (invocation.TerminalResult.IsTerminal)
        {
            return;
        }

        var runtimeOptions = AwkRuntimeOptions.CreateSystemDefault();
        var sourceLoad = await AwkSourceLoader.LoadAsync(
            fileSystem,
            invocation,
            new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters),
            CancellationToken.None);
        if (sourceLoad is not AwkSourceLoadResult.Loaded loaded)
            return;

        var expectParserFailure = IsParserDiagnosticCase(testCase);
        var source = loaded.CompilationSource;
        var binding = AwkBinder.Bind(
            AwkParser.Parse(source.Text),
            invocation.ExecutionOptions.CompatibilityProfile);
        if (expectParserFailure)
        {
            AssertStructuredParserDiagnostic(testCase, binding);
            return;
        }

        Assert.Empty(binding.Diagnostics);
        Assert.True(binding.Program.HasProgram);
    }

    [Theory]
    [MemberData(nameof(ParserParityCases))]
    public async Task Awk_GoldenSourcesHaveSyntaxOutlinesForExecutableLowering(AwkGoldenCase testCase)
    {
        var fileSystem = AwkTestHarness.BuildFileSystem(testCase);
        var invocation = AwkCommandParser.Parse(
            AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment));
        if (invocation.TerminalResult.IsTerminal)
        {
            return;
        }

        var runtimeOptions = AwkRuntimeOptions.CreateSystemDefault();
        var sourceLoad = await AwkSourceLoader.LoadAsync(
            fileSystem,
            invocation,
            new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters),
            CancellationToken.None);
        if (sourceLoad is not AwkSourceLoadResult.Loaded loaded)
            return;

        var expectParserFailure = IsParserDiagnosticCase(testCase);
        var source = loaded.CompilationSource;
        var parse = AwkParser.Parse(source.Text);
        var binding = AwkBinder.Bind(parse, invocation.ExecutionOptions.CompatibilityProfile);
        if (expectParserFailure)
        {
            AssertStructuredParserDiagnostic(testCase, binding);
            return;
        }

        if (testCase.SupportLevel != AwkGoldenSupportLevel.CurrentPass && !binding.Success)
            return;

        Assert.True(binding.Success, "Expected executable parse success for golden case: " + testCase.Name);
        AssertSyntaxOutlinesForAcceptedSource(testCase.Name, parse.Syntax);
    }

    [Fact]
    public async Task Awk_DirectExpectedFailingCasesDocumentGaps()
    {
        foreach (var item in ExpectedFailingCases())
        {
            var testCase = Assert.IsType<AwkGoldenCase>(Assert.Single(item));
            var result = await AwkTestHarness.RunGoldenCaseAsync(testCase, CancellationToken.None);

            Assert.False(
                GoldenCaseMatches(testCase, result),
                "Expected-failing awk case now matches the oracle expectation and should be promoted: " + testCase.Name);
        }
    }

    [Fact]
    public void Awk_GoldenCorpusHasNoExpectedFailingCases()
    {
        Assert.Empty(ExpectedFailingCases());
    }

    [Fact]
    public void Awk_GoldenCorpusDiagnosticHealthCheck()
    {
        var genericInvalidScriptCases = AwkGoldenCorpus.DirectCases
            .Where(testCase => testCase.ExpectedStdErr == "awk: invalid script\n")
            .Select(testCase => testCase.Name)
            .ToArray();
        var nonAwkDiagnostics = AwkGoldenCorpus.DirectCases
            .Where(testCase => testCase.ExpectedExitCode != 0)
            .Where(testCase => testCase.ExpectedStdErr.Length > 0)
            .Where(testCase => !testCase.ExpectedStdErr.StartsWith("awk:", StringComparison.Ordinal))
            .Select(testCase => testCase.Name)
            .ToArray();
        var clrLeakExpectations = AwkGoldenCorpus.DirectCases
            .Where(testCase => LooksLikeClrLeak(testCase.ExpectedStdErr))
            .Select(testCase => testCase.Name)
            .ToArray();

        Assert.Empty(ExpectedFailingCases());
        Assert.Empty(genericInvalidScriptCases);
        Assert.Empty(nonAwkDiagnostics);
        Assert.Empty(clrLeakExpectations);
    }

    [Fact]
    public void Awk_PosixTaggedGoldenCasesAreCurrentPass()
    {
        var posixCases = AwkGoldenCorpus.DirectCases
            .Where(testCase => testCase.Tags.Contains(AwkGoldenTag.Posix))
            .ToArray();

        Assert.NotEmpty(posixCases);
        Assert.All(posixCases, testCase => Assert.Equal(AwkGoldenSupportLevel.CurrentPass, testCase.SupportLevel));
    }

    [Fact]
    public void Awk_GoldenCorpusCaseNamesAreUnique()
    {
        var duplicates = AwkGoldenCorpus.DirectCases
            .GroupBy(testCase => testCase.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Awk_GoldenCorpusHasNoExactDuplicateCases()
    {
        var duplicates = AwkGoldenCorpus.DirectCases
            .GroupBy(BuildDuplicateDetectionFingerprint, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(", ", group.Select(testCase => testCase.Name)))
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Awk_GoldenCorpusHasNoWhitespaceOnlyNearDuplicateCases()
    {
        var duplicates = AwkGoldenCorpus.DirectCases
            .GroupBy(BuildNearDuplicateDetectionFingerprint, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(", ", group.Select(testCase => testCase.Name)))
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Awk_GoldenCorpusReportsFeatureCoverageByTag()
    {
        var missingCoverage = FeatureCoverageTags
            .Select(tag => new
            {
                Tag = tag,
                Count = AwkGoldenCorpus.DirectCases.Count(testCase => testCase.Tags.Contains(tag))
            })
            .Where(item => item.Count == 0)
            .Select(item => item.Tag + " has no golden cases")
            .ToArray();

        var casesWithoutFeatureTag = AwkGoldenCorpus.DirectCases
            .Where(testCase => !testCase.Tags.Any(tag => FeatureCoverageTags.Contains(tag)))
            .Select(testCase => testCase.Name)
            .ToArray();

        Assert.Empty(missingCoverage);
        Assert.Empty(casesWithoutFeatureTag);
    }

    [Fact]
    public void Awk_GoldenCorpusHasLayerBehaviorAndOracleTags()
    {
        var missingLayer = AwkGoldenCorpus.DirectCases
            .Where(testCase => !testCase.Tags.Contains(AwkGoldenTag.DirectLayer))
            .Select(testCase => testCase.Name)
            .ToArray();
        var missingBehavior = AwkGoldenCorpus.DirectCases
            .Where(testCase => !testCase.Tags.Any(tag => BehaviorTags.Contains(tag)))
            .Select(testCase => testCase.Name)
            .ToArray();
        var missingOracle = AwkGoldenCorpus.DirectCases
            .Where(testCase => !testCase.Tags.Contains(AwkGoldenTag.OracleReviewed))
            .Select(testCase => testCase.Name)
            .ToArray();
        var missingHostBehavior = AwkGoldenCorpus.DirectCases
            .Where(HasObservableHostBehavior)
            .Where(testCase => !testCase.Tags.Contains(AwkGoldenTag.HostBehavior))
            .Select(testCase => testCase.Name)
            .ToArray();
        var missingCaveat = AwkGoldenCorpus.DirectCases
            .Where(NeedsCompatibilityCaveat)
            .Where(testCase => !testCase.Tags.Contains(AwkGoldenTag.CompatibilityCaveat))
            .Select(testCase => testCase.Name)
            .ToArray();

        Assert.Empty(missingLayer);
        Assert.Empty(missingBehavior);
        Assert.Empty(missingOracle);
        Assert.Empty(missingHostBehavior);
        Assert.Empty(missingCaveat);
    }

    [Fact]
    public void Awk_TestLayerReadmeDeclaresGoldenCorpusCanonical()
    {
        var readmeText = File.ReadAllText(LocateAwkTestReadme());

        Assert.Contains("canonical behavior and diagnostic suite", readmeText, StringComparison.Ordinal);
        Assert.Contains("Do not duplicate plain language behavior here unless a host boundary is the point of the test.", readmeText, StringComparison.Ordinal);
        Assert.Contains("Prefer moving ordinary user-visible behavior to the golden corpus.", readmeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Awk_OutputGoldenCorpusPinsFlushOwnershipCases()
    {
        var outputCases = AwkGoldenCorpus.DirectCases
            .Where(testCase => testCase.Tags.Contains(AwkGoldenTag.Output))
            .Select(testCase => testCase.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("begin-main-end-stdout-order", outputCases);
        Assert.Contains("exit-in-main-flushes-output-before-end", outputCases);
        Assert.Contains("stdout-and-file-output-flush-independently", outputCases);
        Assert.Contains("stdout-and-stderr-special-paths-flush-independently", outputCases);
    }

    private static void AssertGoldenText(AwkGoldenCase testCase, string actual)
    {
        switch (testCase.StdOutComparison)
        {
            case AwkGoldenTextComparison.Exact:
                Assert.Equal(testCase.ExpectedStdOut, actual);
                break;
            case AwkGoldenTextComparison.UnorderedNonEmptyLines:
                Assert.Equal(SortedNonEmptyLines(testCase.ExpectedStdOut), SortedNonEmptyLines(actual));
                break;
            default:
                throw new InvalidOperationException("Unknown stdout comparison mode.");
        }
    }

    private static bool GoldenCaseMatches(AwkGoldenCase testCase, AwkTestRunResult result)
    {
        if (testCase.ExpectedExitCode != result.ExitCode ||
            testCase.ExpectedStdErr != result.StdErr ||
            !GoldenTextMatches(testCase, result.StdOut))
        {
            return false;
        }

        foreach (var expectedFile in testCase.ExpectedFiles)
        {
            if (!result.ExpectedFiles.TryGetValue(expectedFile.Path, out var actualContent) ||
                actualContent != expectedFile.Content)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsParserDiagnosticCase(AwkGoldenCase testCase) =>
        testCase.Tags.Contains(AwkGoldenTag.CompilerDiagnostic);

    private static bool IsSourceLoadingFailure(AwkGoldenCase testCase) =>
        testCase.ExpectedExitCode != 0 &&
        testCase.Tags.Contains(AwkGoldenTag.SourceLoading);

    private static bool HasExecutableCounterActivity(AwkRuntimeCounters counters) =>
        counters.VmBasicBlockEntries > 0 ||
        counters.VmInstructions > 0 ||
        counters.VmFallbackCalls > 0 ||
        counters.VmPatternFallbacks > 0;

    private static bool IsExpectedNoVmDispatchCase(AwkGoldenCase testCase) =>
        testCase.Name is
            "missing-input-file-without-beginfile-fails" or
            "input-directory-without-beginfile-fails" or
            "main-input-read-open-error-fails" or
            "empty-inline-script" or
            "empty-script-file" or
            "function-only-program";

    private static AwkRuntimeOptions CreateExecutableOptions(AwkRuntimeCounters counters) =>
        new(
            AwkUnixTimestampSeconds.FromSystemClock,
            TimeZoneInfo.Local,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            executionEngineKind: AwkExecutionEngineKind.Executable);

    private static string LocateAwkTestReadme()
    {
        return Path.Combine(LocateAwkTestDirectory(), "README.md");
    }

    private static string LocateAwkTestDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "Lokad.Awk.Tests");
            if (File.Exists(Path.Combine(candidate, "README.md")))
                return candidate;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Awk test directory.");
    }

    private static void AssertSyntaxOutlinesForAcceptedSource(string caseName, AwkSyntaxProgram syntax)
    {
        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            var function = syntax.Functions[functionIndex];
            AssertBodyOutline(
                caseName + " function " + function.Name,
                function.BodyTokens,
                function.BodyStatements);
        }

        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            var rule = syntax.Rules[ruleIndex];
            if (rule.Kind == AwkRuleKind.Main && HasSignificantTokens(rule.PatternTokens))
            {
                if (rule.PatternSyntax is AwkSinglePatternSyntax singlePattern)
                {
                    Assert.NotNull(singlePattern.Expression);
                    AssertExpressionOutline(caseName + " rule pattern " + ruleIndex, singlePattern.Expression);
                }
                else
                {
                    var rangePattern = Assert.IsType<AwkRangePatternSyntax>(rule.PatternSyntax);
                    AssertExpressionOutline(caseName + " rule range start " + ruleIndex, rangePattern.Start.Expression);
                    AssertExpressionOutline(caseName + " rule range end " + ruleIndex, rangePattern.End.Expression);
                }
            }

            if (rule.HasExplicitAction)
            {
                AssertBodyOutline(
                    caseName + " rule action " + ruleIndex,
                    rule.ActionTokens,
                    rule.ActionStatements);
            }
        }
    }

    private static void AssertStructuredParserDiagnostic(AwkGoldenCase testCase, AwkBindingResult binding)
    {
        Assert.True(
            !binding.Success ||
            binding.Diagnostics.Count > 0 ||
            binding.CompatibilityParseFailed,
            "Expected parser failure for golden case: " + testCase.Name);

        var diagnostic = binding.Diagnostics.FirstOrDefault(diagnostic => !diagnostic.IsWarning);
        Assert.NotEqual(AwkDiagnosticId.Unknown, diagnostic.Id);
        Assert.True(
            diagnostic.Category is
                AwkDiagnosticCategory.Parse or
                AwkDiagnosticCategory.Bind or
                AwkDiagnosticCategory.UnsupportedPolicy,
            "Unexpected diagnostic layer for golden case " + testCase.Name + ": " + diagnostic.Category);

        var failure = AwkDiagnostics.Parse(binding.Diagnostics);
        Assert.Equal(diagnostic.Id, failure.Id);
    }

    private static void AssertBodyOutline(
        string context,
        IReadOnlyList<AwkToken> bodyTokens,
        IReadOnlyList<AwkStatementSyntax> statements)
    {
        if (HasSignificantTokens(bodyTokens))
            Assert.NotEmpty(statements);

        for (var statementIndex = 0; statementIndex < statements.Count; statementIndex++)
        {
            AssertStatementOutline(context + " statement " + statementIndex, statements[statementIndex]);
        }
    }

    private static void AssertStatementOutline(string context, AwkStatementSyntax statement)
    {
        Assert.NotEqual(AwkStatementKind.Unknown, statement.Kind);
        Assert.NotEmpty(statement.Tokens);
        Assert.True(statement.Span.Length > 0, context);

        if (statement.LValue.TryGetValue(out var lValue))
        {
            Assert.True(lValue.Span.Length > 0, context);
        }

        var childExpressions = statement.EnumerateChildExpressions().ToArray();
        for (var expressionIndex = 0; expressionIndex < childExpressions.Length; expressionIndex++)
        {
            AssertExpressionOutline(context + " expression " + expressionIndex, childExpressions[expressionIndex]);
        }

        for (var childIndex = 0; childIndex < statement.BodyStatements.Count; childIndex++)
        {
            AssertStatementOutline(context + " body " + childIndex, statement.BodyStatements[childIndex]);
        }

        for (var childIndex = 0; childIndex < statement.ElseStatements.Count; childIndex++)
        {
            AssertStatementOutline(context + " else " + childIndex, statement.ElseStatements[childIndex]);
        }

        for (var caseIndex = 0; caseIndex < statement.SwitchCases.Count; caseIndex++)
        {
            var switchCase = statement.SwitchCases[caseIndex];
            if (!switchCase.IsDefault)
                AssertExpressionOutline(context + " case " + caseIndex, switchCase.Expression);

            for (var childIndex = 0; childIndex < switchCase.BodyStatements.Count; childIndex++)
            {
                AssertStatementOutline(context + " case body " + childIndex, switchCase.BodyStatements[childIndex]);
            }
        }
    }

    private static void AssertExpressionOutline(
        string context,
        AwkSyntaxOptional<AwkExpressionSyntax> expression)
    {
        Assert.True(expression.TryGetValue(out var syntax), context);
        AssertExpressionOutline(context, syntax);
    }

    private static void AssertExpressionOutline(string context, AwkExpressionSyntax? expression)
    {
        Assert.NotNull(expression);
        Assert.NotEqual(AwkExpressionKind.Unknown, expression.Kind);
        Assert.NotEmpty(expression.Tokens);
        Assert.True(expression.Span.Length > 0, context);

        for (var childIndex = 0; childIndex < expression.Children.Count; childIndex++)
        {
            AssertExpressionOutline(context + " child " + childIndex, expression.Children[childIndex]);
        }
    }

    private static bool HasSignificantTokens(IReadOnlyList<AwkToken> tokens) =>
        tokens.Any(token => token.Kind is not AwkTokenKind.Comment and not AwkTokenKind.NewLine and not AwkTokenKind.Semicolon);

    private static bool LooksLikeClrLeak(string text) =>
        text.Contains("System.", StringComparison.Ordinal) ||
        text.Contains("Exception", StringComparison.Ordinal) ||
        text.Contains("NullReferenceException", StringComparison.Ordinal) ||
        text.Contains("InvalidOperationException", StringComparison.Ordinal) ||
        text.Contains("AggregateException", StringComparison.Ordinal) ||
        text.Contains("\n   at ", StringComparison.Ordinal) ||
        text.Contains("\r\n   at ", StringComparison.Ordinal);

    private static bool GoldenTextMatches(AwkGoldenCase testCase, string actual)
    {
        return testCase.StdOutComparison switch
        {
            AwkGoldenTextComparison.Exact => testCase.ExpectedStdOut == actual,
            AwkGoldenTextComparison.UnorderedNonEmptyLines =>
                SortedNonEmptyLines(testCase.ExpectedStdOut).SequenceEqual(SortedNonEmptyLines(actual)),
            _ => throw new InvalidOperationException("Unknown stdout comparison mode.")
        };
    }

    private static string[] SortedNonEmptyLines(string text)
    {
        return text
            .Split('\n')
            .Where(line => line.Length > 0)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildDuplicateDetectionFingerprint(AwkGoldenCase testCase) =>
        BuildGoldenCaseFingerprint(testCase, PreserveFingerprintText, includeSupport: true);

    private static string BuildNearDuplicateDetectionFingerprint(AwkGoldenCase testCase) =>
        BuildGoldenCaseFingerprint(testCase, NormalizeNearDuplicateText, includeSupport: false);

    private static string BuildGoldenCaseFingerprint(
        AwkGoldenCase testCase,
        Func<string, string> normalizeUserText,
        bool includeSupport)
    {
        var builder = new StringBuilder();
        AppendSequence(builder, "arg", testCase.Arguments, normalizeUserText);
        AppendFiles(builder, "input-file", testCase.InputFiles, normalizeUserText);
        AppendPaths(builder, "input-dir", testCase.InputDirectories);
        AppendReadOpenErrors(builder, testCase.ReadOpenErrors);
        AppendValue(builder, "stdin", normalizeUserText(testCase.StandardInput));
        AppendEnvironment(builder, testCase.Environment, normalizeUserText);
        AppendValue(builder, "exit", testCase.ExpectedExitCode.ToString());
        AppendValue(builder, "stdout", normalizeUserText(testCase.ExpectedStdOut));
        AppendValue(builder, "stderr", normalizeUserText(testCase.ExpectedStdErr));
        AppendFiles(builder, "expected-file", testCase.ExpectedFiles, normalizeUserText);
        AppendValue(builder, "stdout-comparison", testCase.StdOutComparison.ToString());
        if (includeSupport)
            AppendValue(builder, "support", testCase.SupportLevel.ToString());

        return builder.ToString();
    }

    private static void AppendFiles(
        StringBuilder builder,
        string label,
        IReadOnlyList<AwkGoldenFile> files,
        Func<string, string> normalizeContent)
    {
        foreach (var file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            AppendValue(builder, label + ".path", file.Path);
            AppendValue(builder, label + ".permission", file.Permission.ToString());
            AppendValue(builder, label + ".content", normalizeContent(file.Content));
        }
    }

    private static void AppendReadOpenErrors(StringBuilder builder, IReadOnlyList<AwkGoldenReadOpenError> errors)
    {
        foreach (var error in errors.OrderBy(error => error.Path, StringComparer.Ordinal))
        {
            AppendValue(builder, "read-error.path", error.Path);
            AppendValue(builder, "read-error.message", error.Message);
        }
    }

    private static void AppendPaths(
        StringBuilder builder,
        string label,
        IReadOnlyList<AwkGoldenPath> paths)
    {
        for (var index = 0; index < paths.Count; index++)
        {
            AppendValue(builder, label + "." + index, paths[index].Path);
        }
    }

    private static void AppendEnvironment(
        StringBuilder builder,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        Func<string, string> normalizeValue)
    {
        foreach (var variable in environment.OrderBy(variable => variable.Name, StringComparer.Ordinal))
        {
            AppendValue(builder, "env.name", variable.Name);
            AppendValue(builder, "env.value", normalizeValue(variable.Value));
        }
    }

    private static void AppendSequence(
        StringBuilder builder,
        string label,
        IReadOnlyList<string> values,
        Func<string, string> normalizeValue)
    {
        for (var index = 0; index < values.Count; index++)
        {
            AppendValue(builder, label + "." + index, normalizeValue(values[index]));
        }
    }

    private static void AppendValue(StringBuilder builder, string label, string value)
    {
        builder.Append(label);
        builder.Append('=');
        builder.Append(value.Length);
        builder.Append(':');
        builder.Append(value);
        builder.Append('\n');
    }

    private static string PreserveFingerprintText(string text) => text;

    private static string NormalizeNearDuplicateText(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static bool HasObservableHostBehavior(AwkGoldenCase testCase) =>
        testCase.ExpectedFiles.Count > 0 ||
        testCase.InputDirectories.Count > 0 ||
        testCase.ReadOpenErrors.Count > 0 ||
        testCase.Tags.Contains(AwkGoldenTag.HostPolicy) ||
        testCase.Tags.Contains(AwkGoldenTag.HostPolicyBoundary);

    private static bool NeedsCompatibilityCaveat(AwkGoldenCase testCase) =>
        testCase.NondeterminismNote.Length > 0 ||
        testCase.SupportLevel == AwkGoldenSupportLevel.KnownUnsupported ||
        testCase.Tags.Contains(AwkGoldenTag.KnownUnsupported);

    private static readonly AwkGoldenTag[] FeatureCoverageTags =
    [
        AwkGoldenTag.AgentWorkflow,
        AwkGoldenTag.Array,
        AwkGoldenTag.CommandLine,
        AwkGoldenTag.CompatibilityMode,
        AwkGoldenTag.Diagnostic,
        AwkGoldenTag.Expression,
        AwkGoldenTag.Field,
        AwkGoldenTag.Function,
        AwkGoldenTag.HostPolicy,
        AwkGoldenTag.Io,
        AwkGoldenTag.Output,
        AwkGoldenTag.Pattern,
        AwkGoldenTag.Record,
        AwkGoldenTag.Regex,
        AwkGoldenTag.SourceLoading,
        AwkGoldenTag.Statement,
        AwkGoldenTag.UserFunction,
        AwkGoldenTag.Variable
    ];

    private static readonly AwkGoldenTag[] BehaviorTags =
    [
        AwkGoldenTag.RuntimeBehavior,
        AwkGoldenTag.DiagnosticBehavior,
        AwkGoldenTag.HostBehavior
    ];

}

