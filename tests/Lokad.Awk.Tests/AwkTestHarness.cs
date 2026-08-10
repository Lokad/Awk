using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

internal static class AwkTestHarness
{
    public static AwkRuntimeState CreateRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken) =>
        CreateRuntimeState(fieldSeparator, cancellationToken, AwkRuntimeOptions.CreateSystemDefault());

    public static AwkRuntimeState CreateRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        new(fieldSeparator, cancellationToken, runtimeOptions, AwkCompatibilityProfile.Default);

    public static AwkOutputSink CreateOutputSink() =>
        new(
            AwkRuntimeLimits.CreateDefault(),
            AwkMemoryGovernor.Disabled,
            AwkRuntimeCounters.Disabled);

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
            fileSystem.AddReadOpenError(error.Path, error.Message);
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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", testCase.Arguments, testCase.Environment)));
        var exitCode = await tool.ExecuteAsync(fileSystem, cancellationToken).ConfigureAwait(false);
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

    public static async Task<AwkTestRunResult> RunGoldenCaseAsync(
        AwkGoldenCase testCase,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        var fileSystem = BuildFileSystem(testCase);
        var compiled = await AwkExecutor
            .CompileAsync(fileSystem, BuildInvocation("awk", testCase.Arguments, testCase.Environment), cancellationToken)
            .ConfigureAwait(false);
        foreach (var warning in compiled.Warnings)
        {
            await fileSystem
                .AppendAsync(AwkFileDescriptor.StdErr, AwkDiagnostics.Warning(warning), cancellationToken)
                .ConfigureAwait(false);
        }

        var exitCode = await compiled.ExecuteAsync(fileSystem, cancellationToken, runtimeOptions).ConfigureAwait(false);
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
        var tool = Assert.IsType<Awk>(Awk.TryParse(BuildInvocation("awk", arguments, [])));
        var exitCode = await tool.ExecuteAsync(fileSystem, cancellationToken).ConfigureAwait(false);

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
