using System;
using System.Collections.Generic;

namespace Lokad.Awk.Tests;

public sealed class AwkGoldenCase
{
    public AwkGoldenCase(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles,
        IReadOnlyList<AwkGoldenPath> inputDirectories,
        IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors,
        string standardInput,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkExpectedExitCode expectedExitCode,
        string expectedStdOut,
        string expectedStdErr,
        IReadOnlyList<AwkGoldenFile> expectedFiles,
        AwkGoldenTextComparison stdOutComparison,
        AwkGoldenSupportLevel supportLevel,
        IReadOnlyList<AwkGoldenTag> tags,
        string nondeterminismNote)
    {
        Name = name;
        Arguments = arguments;
        InputFiles = inputFiles;
        InputDirectories = inputDirectories;
        ReadOpenErrors = readOpenErrors;
        StandardInput = standardInput;
        Environment = environment;
        ExpectedStatus = expectedExitCode;
        ExpectedStdOut = expectedStdOut;
        ExpectedStdErr = expectedStdErr;
        ExpectedFiles = expectedFiles;
        StdOutComparison = stdOutComparison;
        SupportLevel = supportLevel;
        Tags = tags;
        NondeterminismNote = nondeterminismNote;
    }

    public string Name { get; }

    public IReadOnlyList<string> Arguments { get; }

    public IReadOnlyList<AwkGoldenFile> InputFiles { get; }

    public IReadOnlyList<AwkGoldenPath> InputDirectories { get; }

    public IReadOnlyList<AwkGoldenReadOpenError> ReadOpenErrors { get; }

    public string StandardInput { get; }

    public IReadOnlyList<AwkEnvironmentVariable> Environment { get; }

    public AwkExpectedExitCode ExpectedStatus { get; }

    public int ExpectedExitCode => ExpectedStatus.Value;

    public string ExpectedStdOut { get; }

    public string ExpectedStdErr { get; }

    public IReadOnlyList<AwkGoldenFile> ExpectedFiles { get; }

    public AwkGoldenTextComparison StdOutComparison { get; }

    public AwkGoldenSupportLevel SupportLevel { get; }

    public IReadOnlyList<AwkGoldenTag> Tags { get; }

    public string NondeterminismNote { get; }

    public override string ToString() => Name;
}

internal sealed class AwkGoldenCaseBuilder
{
    private readonly string _name;
    private readonly IReadOnlyList<string> _arguments;
    private readonly IReadOnlyList<AwkGoldenFile> _inputFiles;
    private IReadOnlyList<AwkGoldenPath> _inputDirectories = [];
    private IReadOnlyList<AwkGoldenReadOpenError> _readOpenErrors = [];
    private string _standardInput = string.Empty;
    private IReadOnlyList<AwkEnvironmentVariable> _environment = [];
    private AwkExpectedExitCode _expectedExitCode = new(0);
    private string _expectedStdOut = string.Empty;
    private string _expectedStdErr = string.Empty;
    private IReadOnlyList<AwkGoldenFile> _expectedFiles = [];
    private AwkGoldenTextComparison _stdOutComparison = AwkGoldenTextComparison.Exact;
    private AwkGoldenSupportLevel _supportLevel = AwkGoldenSupportLevel.CurrentPass;
    private IReadOnlyList<AwkGoldenTag> _tags = [AwkGoldenTag.CurrentPass];
    private string _nondeterminismNote = string.Empty;

    private AwkGoldenCaseBuilder(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles)
    {
        _name = name;
        _arguments = arguments;
        _inputFiles = inputFiles;
    }

    public static AwkGoldenCaseBuilder Create(
        string name,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkGoldenFile> inputFiles) =>
        new(name, arguments, inputFiles);

    public AwkGoldenCaseBuilder WithInputDirectories(IReadOnlyList<string> inputDirectories)
    {
        _inputDirectories = ToGoldenPaths(inputDirectories);
        return this;

        static AwkGoldenPath[] ToGoldenPaths(IReadOnlyList<string> paths)
        {
            if (paths.Count == 0)
                return [];

            var converted = new AwkGoldenPath[paths.Count];
            for (var index = 0; index < paths.Count; index++)
            {
                converted[index] = new AwkGoldenPath(paths[index]);
            }

            return converted;
        }
    }

    public AwkGoldenCaseBuilder WithInputDirectories(IReadOnlyList<AwkGoldenPath> inputDirectories)
    {
        _inputDirectories = inputDirectories;
        return this;
    }

    public AwkGoldenCaseBuilder WithReadOpenErrors(IReadOnlyList<AwkGoldenReadOpenError> readOpenErrors)
    {
        _readOpenErrors = readOpenErrors;
        return this;
    }

    public AwkGoldenCaseBuilder WithStandardInput(string standardInput)
    {
        _standardInput = standardInput;
        return this;
    }

    public AwkGoldenCaseBuilder WithEnvironment(IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        _environment = environment;
        return this;
    }

    public AwkGoldenCaseBuilder ExitsWith(int exitCode)
    {
        _expectedExitCode = new AwkExpectedExitCode(exitCode);
        return this;
    }

    public AwkGoldenCaseBuilder WithStdOut(string expectedStdOut)
    {
        _expectedStdOut = expectedStdOut;
        return this;
    }

    public AwkGoldenCaseBuilder WithStdErr(string expectedStdErr)
    {
        _expectedStdErr = expectedStdErr;
        return this;
    }

    public AwkGoldenCaseBuilder WithExpectedFiles(IReadOnlyList<AwkGoldenFile> expectedFiles)
    {
        _expectedFiles = expectedFiles;
        return this;
    }

    public AwkGoldenCaseBuilder WithStdOutComparison(AwkGoldenTextComparison comparison)
    {
        _stdOutComparison = comparison;
        return this;
    }

    public AwkGoldenCaseBuilder WithSupportLevel(AwkGoldenSupportLevel supportLevel)
    {
        _supportLevel = supportLevel;
        return this;
    }

    public AwkGoldenCaseBuilder WithTags(IReadOnlyList<AwkGoldenTag> tags)
    {
        _tags = tags;
        return this;
    }

    public AwkGoldenCaseBuilder WithNondeterminismNote(string nondeterminismNote)
    {
        _nondeterminismNote = nondeterminismNote;
        return this;
    }

    public AwkGoldenCase Build() =>
        new(
            _name,
            _arguments,
            _inputFiles,
            _inputDirectories,
            _readOpenErrors,
            _standardInput,
            _environment,
            _expectedExitCode,
            _expectedStdOut,
            _expectedStdErr,
            _expectedFiles,
            _stdOutComparison,
            _supportLevel,
            _tags,
            _nondeterminismNote);
}

public readonly record struct AwkGoldenPath
{
    private readonly string _path;

    public AwkGoldenPath(string path)
    {
        if (!AwkPath.IsValid(path))
            throw new ArgumentException("Golden paths must be absolute awk paths.", nameof(path));

        _path = path;
    }

    public string Path => _path ?? string.Empty;

    public override string ToString() => Path;
}

public readonly record struct AwkExpectedExitCode
{
    public AwkExpectedExitCode(int value)
    {
        ExitCode = new AwkExitCode(value);
    }

    public AwkExitCode ExitCode { get; }

    public int Value => ExitCode.Value;

    public override string ToString() => Value.ToString();
}

public readonly struct AwkGoldenFile
{
    public AwkGoldenFile(string path, string content)
        : this(new AwkGoldenPath(path), content)
    {
    }

    public AwkGoldenFile(string path, string content, AwkFileTreePermission permission)
        : this(new AwkGoldenPath(path), content, permission)
    {
    }

    public AwkGoldenFile(AwkGoldenPath path, string content)
        : this(path, content, AwkFileTreePermission.ReadWrite)
    {
    }

    public AwkGoldenFile(AwkGoldenPath path, string content, AwkFileTreePermission permission)
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

public readonly struct AwkGoldenReadOpenError
{
    public AwkGoldenReadOpenError(string path, string message)
        : this(new AwkGoldenPath(path), message)
    {
    }

    public AwkGoldenReadOpenError(AwkGoldenPath path, string message)
    {
        GoldenPath = path;
        Message = message;
    }

    public AwkGoldenPath GoldenPath { get; }

    public string Path => GoldenPath.Path;

    public string Message { get; }
}

public enum AwkGoldenTextComparison
{
    Exact,
    UnorderedNonEmptyLines
}

public enum AwkGoldenSupportLevel
{
    CurrentPass,
    KnownUnsupported
}

public enum AwkGoldenTag
{
    CurrentPass,
    DirectLayer,
    RuntimeBehavior,
    DiagnosticBehavior,
    HostBehavior,
    CompatibilityCaveat,
    Posix,
    GnuCommon,
    GnuAdvanced,
    HostPolicyBoundary,
    KnownUnsupported,
    AgentWorkflow,
    Array,
    CommandLine,
    CompilerDiagnostic,
    CompatibilityMode,
    Diagnostic,
    Expression,
    Field,
    Function,
    HostPolicy,
    Io,
    Output,
    Pattern,
    Record,
    Regex,
    SourceLoading,
    Statement,
    UserFunction,
    Variable
}

internal static class AwkMessages
{
    public const string HelpMessageText =
        "Usage: awk [OPTION]... 'program' [file...]\n" +
        "Process text files using awk patterns and actions.\n" +
        "\n" +
        "  -F fs, --field-separator=fs   use fs for the input field separator\n" +
        "  -f file, --file=file          read program source from file\n" +
        "  -e program, --source=program  use program as awk source text\n" +
        "  -v var=value, --assign=var=value\n" +
        "                                assign a variable before program execution\n" +
        "  -h, --help                    display this help and exit\n" +
        "  -V, --version                 display Lokad awk version and exit\n";

    public static string VersionMessageText => "Lokad awk " + Lokad.Awk.AwkVersion.Text + "\n";
}
