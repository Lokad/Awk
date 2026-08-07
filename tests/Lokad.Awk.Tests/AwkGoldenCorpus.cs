using System;
using System.Collections.Generic;

namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    public static IReadOnlyList<AwkGoldenCase> DirectCases { get; } =
    [
        .. Tagged(VariableCases(), AwkGoldenTag.Variable),

        .. Tagged(FieldCases(), AwkGoldenTag.Field),

        .. Tagged(RecordSchedulingCases(), AwkGoldenTag.Record),

        .. Tagged(CommandLineRuntimeCases(), AwkGoldenTag.CommandLine),

        .. Tagged(PatternCases(), AwkGoldenTag.Pattern),

        .. Tagged(RegexSubstitutionCases(), AwkGoldenTag.Regex),

        .. Tagged(ExpressionCases(), AwkGoldenTag.Expression),

        .. Tagged(ExpressionRuntimeCases(), AwkGoldenTag.Expression),

        .. Tagged(
            CompilerDiagnostics(
                DiagnosticCases(),
                "invalid-delete-scalar-fails",
                "invalid-delete-scalar-element-fails",
                "builtin-scalar-as-array-fails",
                "bindtextdomain-function-unsupported",
                "dcgettext-function-unsupported",
                "dcngettext-function-unsupported",
                "internationalized-string-marker-in-string",
                "ambiguous-string-escape-warnings"),
            AwkGoldenTag.Diagnostic),

        .. Tagged(BuiltinFunctionCases(), AwkGoldenTag.Function),

        .. Tagged(IoCases(), AwkGoldenTag.Io),

        .. Tagged(HostPolicyCases(), AwkGoldenTag.HostPolicy),

        .. Tagged(RegexCases(), AwkGoldenTag.Regex),

        .. Tagged(OutputCases(), AwkGoldenTag.Output),

        .. Tagged(ArrayRuntimeCases(), AwkGoldenTag.Array),

        .. Tagged(StatementControlCases(), AwkGoldenTag.Statement),

        .. Tagged(StatementSeparatorCases(), AwkGoldenTag.Statement),

        .. Tagged(StatementRuntimeCases(), AwkGoldenTag.Statement),

        .. Tagged(AgentWorkflowCases(), AwkGoldenTag.AgentWorkflow),

        .. Tagged(ArrayCases(), AwkGoldenTag.Array),

        .. Tagged(CommandLineSourceLoadingCases(), AwkGoldenTag.CommandLine, AwkGoldenTag.SourceLoading),

        .. Tagged(CompatibilityModeCases(), AwkGoldenTag.CompatibilityMode),

        .. Tagged(UserFunctionCases(), AwkGoldenTag.UserFunction, AwkGoldenTag.Function),

        .. Tagged(FunctionSyntaxCases(), AwkGoldenTag.UserFunction, AwkGoldenTag.Function),

        .. Tagged(MainInputIoCases(), AwkGoldenTag.Io, AwkGoldenTag.Record),

        .. Tagged(
            CompilerDiagnostics(
                SyntaxDiagnosticCases(),
                "trailing-print-redirection-token-fails"),
            AwkGoldenTag.Diagnostic)
    ];

    private static IEnumerable<AwkGoldenCase> Tagged(
        IEnumerable<AwkGoldenCase> testCases,
        params AwkGoldenTag[] tags)
    {
        foreach (var testCase in testCases)
            yield return WithTags(testCase, tags);
    }

    private static IEnumerable<AwkGoldenCase> CompilerDiagnostics(
        IEnumerable<AwkGoldenCase> testCases,
        params string[] excludedNames)
    {
        var excluded = new HashSet<string>(excludedNames, StringComparer.Ordinal);
        foreach (var testCase in testCases)
        {
            yield return excluded.Contains(testCase.Name)
                ? testCase
                : CompilerDiagnostic(testCase);
        }
    }

    private static AwkGoldenCase CompilerDiagnostic(AwkGoldenCase testCase) =>
        WithTags(testCase, [AwkGoldenTag.CompilerDiagnostic]);

    private static AwkGoldenCase WithTags(AwkGoldenCase testCase, AwkGoldenTag[] tags)
    {
        var mergedTags = new List<AwkGoldenTag>(testCase.Tags);
        for (var index = 0; index < tags.Length; index++)
        {
            if (!mergedTags.Contains(tags[index]))
                mergedTags.Add(tags[index]);
        }

        AddDerivedTags(testCase, mergedTags);

        return AwkGoldenCaseBuilder
            .Create(testCase.Name, testCase.Arguments, testCase.InputFiles)
            .WithInputDirectories(testCase.InputDirectories)
            .WithReadOpenErrors(testCase.ReadOpenErrors)
            .WithStandardInput(testCase.StandardInput)
            .WithEnvironment(testCase.Environment)
            .ExitsWith(testCase.ExpectedExitCode)
            .WithStdOut(testCase.ExpectedStdOut)
            .WithStdErr(testCase.ExpectedStdErr)
            .WithExpectedFiles(testCase.ExpectedFiles)
            .WithStdOutComparison(testCase.StdOutComparison)
            .WithSupportLevel(testCase.SupportLevel)
            .WithTags(mergedTags)
            .WithNondeterminismNote(testCase.NondeterminismNote)
            .Build();
    }

    private static void AddDerivedTags(AwkGoldenCase testCase, List<AwkGoldenTag> tags)
    {
        AddTag(tags, AwkGoldenTag.DirectLayer);
        AddTag(tags, AwkGoldenTag.OracleReviewed);

        if (testCase.ExpectedStdErr.Length > 0 ||
            testCase.ExpectedExitCode != 0 ||
            testCase.SupportLevel != AwkGoldenSupportLevel.CurrentPass)
        {
            AddTag(tags, AwkGoldenTag.DiagnosticBehavior);
        }

        if (HasHostBehavior(testCase, tags))
            AddTag(tags, AwkGoldenTag.HostBehavior);

        if (!HasBehaviorTag(tags))
            AddTag(tags, AwkGoldenTag.RuntimeBehavior);

        if (testCase.NondeterminismNote.Length > 0 ||
            testCase.SupportLevel == AwkGoldenSupportLevel.KnownUnsupported ||
            tags.Contains(AwkGoldenTag.KnownUnsupported))
        {
            AddTag(tags, AwkGoldenTag.CompatibilityCaveat);
        }
    }

    private static bool HasHostBehavior(AwkGoldenCase testCase, IReadOnlyList<AwkGoldenTag> tags) =>
        testCase.ExpectedFiles.Count > 0 ||
        testCase.InputDirectories.Count > 0 ||
        testCase.ReadOpenErrors.Count > 0 ||
        tags.Contains(AwkGoldenTag.HostPolicy) ||
        tags.Contains(AwkGoldenTag.HostPolicyBoundary);

    private static bool HasBehaviorTag(IReadOnlyList<AwkGoldenTag> tags) =>
        tags.Contains(AwkGoldenTag.RuntimeBehavior) ||
        tags.Contains(AwkGoldenTag.DiagnosticBehavior) ||
        tags.Contains(AwkGoldenTag.HostBehavior);

    private static void AddTag(List<AwkGoldenTag> tags, AwkGoldenTag tag)
    {
        if (!tags.Contains(tag))
            tags.Add(tag);
    }

    private static AwkGoldenCase Passing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut)
    {
        return Passing(name, arguments, inputFiles, expectedStdOut, string.Empty);
    }

    private static AwkGoldenCase Passing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        string standardInput)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStandardInput(standardInput)
            .Build();
    }

    private static AwkGoldenCase PassingUnorderedStdOut(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStdOutComparison(AwkGoldenTextComparison.UnorderedNonEmptyLines)
            .WithNondeterminismNote("GNU awk leaves this array traversal order unspecified.")
            .Build();
    }

    private static AwkGoldenCase PassingWithEnvironment(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithEnvironment(environment)
            .WithStdOut(expectedStdOut)
            .Build();
    }

    private static AwkGoldenCase PassingWithExpectedFiles(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        IReadOnlyList<AwkGoldenFile> expectedFiles)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithExpectedFiles(expectedFiles)
            .Build();
    }

    private static AwkGoldenCase PassingWithStdErr(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase PassingWithReadOpenErrorsAndStdErr(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithReadOpenErrors(readOpenErrors)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase Exiting(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .Build();
    }

    private static AwkGoldenCase Failing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase FailingWithExpectedFiles(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr,
        IReadOnlyList<AwkGoldenFile> expectedFiles)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .WithExpectedFiles(expectedFiles)
            .Build();
    }

    private static AwkGoldenCase ExpectedFailing(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr,
        IReadOnlyList<AwkGoldenTag> tags,
        string nondeterminismNote)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .WithSupportLevel(AwkGoldenSupportLevel.ExpectedFailing)
            .WithTags([.. tags, AwkGoldenTag.ExpectedFailing])
            .WithNondeterminismNote(nondeterminismNote)
            .Build();
    }

    private static AwkGoldenCase FailingWithReadOpenErrors(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithReadOpenErrors(readOpenErrors)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase FailingWithDirectories(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<string> inputDirectories,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        int expectedExitCode,
        string expectedStdOut,
        string expectedStdErr)
    {
        return AwkGoldenCaseBuilder
            .Create(name, arguments, inputFiles)
            .WithInputDirectories(inputDirectories)
            .ExitsWith(expectedExitCode)
            .WithStdOut(expectedStdOut)
            .WithStdErr(expectedStdErr)
            .Build();
    }

    private static AwkGoldenCase Posix(AwkGoldenCase testCase) =>
        AwkGoldenCaseBuilder
            .Create(testCase.Name, testCase.Arguments, testCase.InputFiles)
            .WithInputDirectories(testCase.InputDirectories)
            .WithReadOpenErrors(testCase.ReadOpenErrors)
            .WithStandardInput(testCase.StandardInput)
            .WithEnvironment(testCase.Environment)
            .ExitsWith(testCase.ExpectedExitCode)
            .WithStdOut(testCase.ExpectedStdOut)
            .WithStdErr(testCase.ExpectedStdErr)
            .WithExpectedFiles(testCase.ExpectedFiles)
            .WithStdOutComparison(testCase.StdOutComparison)
            .WithSupportLevel(testCase.SupportLevel)
            .WithTags([.. testCase.Tags, AwkGoldenTag.Posix])
            .WithNondeterminismNote(testCase.NondeterminismNote)
            .Build();

    private static AwkGoldenFile File(string path, string content) => new(path, content);

    private static AwkGoldenFile ReadOnlyFile(string path, string content) =>
        new(path, content, AwkFileTreePermission.ReadOnly);

    private static AwkGoldenReadOpenError ReadOpenError(string path, string message) => new(path, message);
}

