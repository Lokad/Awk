using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

internal static class AwkTestHarness
{
    public static AwkParseResult ParseSource(string source, int maxSyntaxNestingDepth = int.MaxValue) =>
        AwkParser.Parse(source, new AwkFrontendBudget(maxSyntaxNestingDepth), CancellationToken.None);

    public static AwkPhaseResult<AwkProgramResult> BindDefaultProfile(AwkParseResult parsed) =>
        AwkBinder.Bind(parsed, AwkCompatibilityProfile.Default);

    public static List<AwkToken> TokenizeSource(string source) => AwkLexer.Tokenize(source, CancellationToken.None);

    internal static bool TryPrepareRule(AwkRuleSyntax rule, AwkLoweringScope scope, int rangeIndex,
        [NotNullWhen(true)] out AwkRegion? region) => TryPrepareRule(rule, scope, rangeIndex, out region, out _);

    internal static bool TryPrepareRule(AwkRuleSyntax rule, AwkLoweringScope scope, int rangeIndex,
        [NotNullWhen(true)] out AwkRegion? region, out AwkDiagnostic diagnostic)
    {
        if (rule.Kind != AwkRuleKind.Main)
            return AwkRegionCompiler.TryCompilePhase(rule, scope, out region, out diagnostic);
        var compiler = AwkRegionCompiler.CreateMain(scope);
        var success = compiler.AppendMainRule(rule, rangeIndex, out diagnostic);
        region = success ? compiler.BuildMain() : null;
        return success;
    }

    // Semantic cursor tests consume the same one-shot production handoff.
    // Completion/lifetime tests exercise MoveNextAsync and TakeRecord directly.
    public static async ValueTask<AwkInputRecordResult> ReadRecordAsync(this IAwkInputRecordReader reader)
    {
        await reader.MoveNextAsync().ConfigureAwait(false);
        reader.TakeRecord(out var record);
        return record;
    }

    public static AwkExecutionPolicy CreatePolicy() =>
        AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance)
            .WithInternalFailureObserver(AwkInternalFailuresAttribute.CreateObserver());

    public static AwkRuntimeOptions CreateRuntimeOptions(AwkRuntimeCounters? counters = null) =>
        AwkRuntimeOptions.CreateSystemDefault() with
        {
            Counters = counters ?? AwkRuntimeCounters.Disabled,
            RegexEngine = AwkUtf8RegexEngine.Instance,
            InternalFailureObserver = AwkInternalFailuresAttribute.CreateObserver()
        };

    // Use the production record owner; fixtures can retain this view to verify
    // actual backing reuse before checking that captured values remain owned.
    public static ReadOnlyMemory<byte> PublishBufferedRecord(AwkRuntimeState state, ReadOnlySpan<byte> validUtf8)
    {
        var storage = state.InputRecordStorage;
        storage.StageValidated(validUtf8);
        state.PublishInputRecord(AwkInputRecordResult.Buffered(storage, AwkRecordTerminator.Empty), readsMainInput: false);
        return storage.Content;
    }

    public static AwkRuntimeState CreateRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken) =>
        CreateRuntimeState(fieldSeparator, cancellationToken, AwkTestHarness.CreateRuntimeOptions());

    public static AwkRuntimeState CreateRuntimeStateWithCounters(AwkRuntimeCounters counters) =>
        CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, null,
                counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            {
                InternalFailureObserver = AwkInternalFailuresAttribute.CreateObserver()
            });

    public static AwkRuntimeState CreateRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkCompatibilityProfile? profile = null) =>
        new(
            fieldSeparator,
            cancellationToken,
            runtimeOptions,
            profile ?? AwkCompatibilityProfile.Default);

    public static AwkOutputSink CreateOutputSink() =>
        new(
            AwkMemoryAccounting.Disabled);

    public static AwkIo CreateRegionIo(AwkRuntimeState state, IAwkHost? host = null,
        CancellationToken cancellationToken = default) =>
        new(host ?? new MockFileSystem(), AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            cancellationToken, state.Limits, state, static _ => []);

    public static AwkInputScheduler BindRegionInput(string source, AwkRuntimeActionExecutor executor,
        AwkRuntimeState state, AwkIo io, IAwkHost host, CancellationToken cancellationToken = default)
    {
        var invocation = AwkCommandParser.Parse(BuildInvocation("awk", source));
        var input = new AwkInputScheduler(host, invocation, executor, state, io, cancellationToken);
        io.BindMainInputReader(input, input.PreserveStandardInput);
        return input;
    }

    public static AwkCompatibilityProfile ProfileForNumberParsing(AwkNumberParsingMode mode) => mode switch
    {
        AwkNumberParsingMode.Decimal => AwkCompatibilityProfile.Default,
        AwkNumberParsingMode.HexadecimalInput => AwkCompatibilityProfile.Default.WithMode(AwkCompatibilityMode.Posix),
        AwkNumberParsingMode.NonDecimalData => AwkCompatibilityProfile.Default.WithNonDecimalData(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static AwkRedirectedOutput CreateRedirectedOutput(
        string path,
        AwkOutputRedirectionKind kind,
        string content) =>
        new(new AwkRedirectionPath(path), kind, AwkUtf8Codec.Encode(content));

    public static AwkCommandInvocation BuildInvocation(string command, params string[] arguments)
    {
        return BuildInvocation(command, arguments, []);
    }

    public static AwkCommandInvocation BuildInvocation(
        string command,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        var argumentBytes = new List<string>(arguments.Count);
        for (var index = 0; index < arguments.Count; index++)
        {
            argumentBytes.Add(arguments[index]);
        }

        return AwkCommandInvocation.CreateWithStandardDescriptors(command, argumentBytes, environment);
    }

    public static Awk ParseAwk(AwkCommandInvocation invocation)
    {
        Assert.True(Awk.TryParse(invocation, out var awk));
        return awk;
    }

    public static MockFileSystem BuildFileSystem(AwkGoldenCase testCase)
    {
        var fileSystem = new MockFileSystem();
        foreach (var directory in testCase.InputDirectories)
        {
            fileSystem.AddDirectory(directory.Path);
        }

        foreach (var file in testCase.InputFiles)
        {
            fileSystem.AddFile(file.Path, file.Content, permission: file.Permission);
        }

        foreach (var error in testCase.ReadOpenErrors)
        {
            fileSystem.AddReadOpenError(error.Path, new(AwkOperationErrorKind.IoFailure, error.Message));
        }

        if (testCase.StandardInput.Length > 0)
        {
            fileSystem.SetStandardInput(testCase.StandardInput);
        }

        return fileSystem;
    }

    public static async Task<AwkTestRunResult> RunGoldenCaseAsync(
        AwkGoldenCase testCase,
        CancellationToken cancellationToken)
    {
        var fileSystem = BuildFileSystem(testCase);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", testCase.Arguments, testCase.Environment));
        var exitCode = await tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellationToken).ConfigureAwait(false);
        var expectedFiles = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var expectedFile in testCase.ExpectedFiles)
        {
            expectedFiles[expectedFile.Path] = fileSystem.FileExists(expectedFile.Path)
                ? fileSystem.ReadFile(expectedFile.Path)
                : null;
        }

        return new AwkTestRunResult(
            exitCode.Value,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr),
            expectedFiles);
    }

    public static async Task<AwkTestRunResult> RunProgramAsync(
        string program,
        IReadOnlyList<AwkTestInputFile> files,
        IReadOnlyList<string> inputArguments,
        CancellationToken cancellationToken)
    {
        var fileSystem = new MockFileSystem();
        for (var index = 0; index < files.Count; index++)
        {
            fileSystem.AddFile(files[index].Path, files[index].Content, permission: files[index].Permission);
        }

        var arguments = new List<string>(1 + inputArguments.Count) { program };
        arguments.AddRange(inputArguments);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", arguments, []));
        var exitCode = await tool.ExecuteAsync(fileSystem, AwkTestHarness.CreatePolicy(), cancellationToken).ConfigureAwait(false);

        return new AwkTestRunResult(
            exitCode.Value,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr),
            new Dictionary<string, string?>(StringComparer.Ordinal));
    }

    public static void AssertFriendlyAwkDiagnostic(AwkTestRunResult result)
    {
        Assert.NotEmpty(result.StdErr);
        Assert.StartsWith("awk:", result.StdErr);
        AssertNoClrExceptionLeak(result.StdErr);
    }

    public static void AssertFriendlyAwkFailure(
        AwkTestRunResult result,
        int expectedExitCode,
        bool expectNoStdOut)
    {
        Assert.Equal(expectedExitCode, result.ExitCode);
        if (expectNoStdOut)
            Assert.Equal(string.Empty, result.StdOut);

        AssertFriendlyAwkDiagnostic(result);
    }

    public static void AssertNoClrExceptionLeak(string text)
    {
        Assert.DoesNotContain("System.", text);
        Assert.DoesNotContain("Exception", text);
        Assert.DoesNotContain("NullReferenceException", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("AggregateException", text);
        Assert.DoesNotContain("\n   at ", text);
        Assert.DoesNotContain("\r\n   at ", text);
    }
}

internal sealed class AwkTestRunResult(
    int exitCode,
    string stdOut,
    string stdErr,
    IReadOnlyDictionary<string, string?> expectedFiles)
{
    public int ExitCode { get; } = exitCode;

    public string StdOut { get; } = stdOut;

    public string StdErr { get; } = stdErr;

    public IReadOnlyDictionary<string, string?> ExpectedFiles { get; } = expectedFiles;
}

internal readonly struct AwkTestInputFile
{
    public AwkTestInputFile(string path, string content)
        : this(new AwkGoldenPath(path), content)
    {
    }

    public AwkTestInputFile(string path, string content, AwkFileTreePermission permission)
        : this(new AwkGoldenPath(path), content, permission)
    {
    }

    public AwkTestInputFile(AwkGoldenPath path, string content)
        : this(path, content, AwkFileTreePermission.ReadWrite)
    {
    }

    public AwkTestInputFile(AwkGoldenPath path, string content, AwkFileTreePermission permission)
    {
        GoldenPath = path;
        Content = content;
        Permission = permission;
    }

    public AwkGoldenPath GoldenPath { get; }

    public string Path => GoldenPath.Path;

    public string Content { get; }

    public AwkFileTreePermission Permission { get; }
}
