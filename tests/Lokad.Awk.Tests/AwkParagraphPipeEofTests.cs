namespace Lokad.Awk.Tests;

public sealed class AwkParagraphPipeEofTests
{
    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var testCase in AwkGoldenCorpus.PipeParagraphEofCases())
        foreach (var executable in new[] { false, true })
        foreach (var utf8 in new[] { false, true })
            yield return [testCase, executable, utf8];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeParagraphEofMatchesTheSelectedReferenceContract(
        AwkGoldenCase testCase, bool executable, bool utf8)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            executable ? AwkExecutionEngineKind.Executable : AwkExecutionEngineKind.Interpreter, regexEngine: AwkUtf8RegexEngine.Instance);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        AwkExitCode exit;
        if (executable)
        {
            var compiled = await AwkExecutor.CompileAsync(host, invocation, deadline.Token);
            exit = await compiled.ExecuteAsync(host, deadline.Token, options);
        }
        else
        {
            exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation), deadline.Token, options);
        }

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(testCase.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        var reopened = testCase.Arguments.Any(arg => arg.Contains("close(\"cat data\")", StringComparison.Ordinal));
        Assert.Equal(reopened ? 2 : 1, host.Commands.Count);
        foreach (var command in host.Commands)
        {
            Assert.Equal("cat data", command.CommandName);
            Assert.Equal(invocation.CurrentDirectory, command.CurrentDirectory);
            Assert.Contains(command.Environment, v => v.Name == "LC_ALL" && v.Value == "C.UTF-8");
        }
        if (executable)
            Assert.True(counters.VmBasicBlockEntries > 0 || counters.VmPatternFallbacks > 0);
    }
}
