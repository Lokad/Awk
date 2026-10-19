namespace Lokad.Awk.Tests;

public sealed class AwkGetlinePostfixTests
{
    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var testCase in AwkGoldenCorpus.PipeGetlinePostfixCases())

            foreach (var utf8 in new[] { false, true })
                yield return [testCase, utf8];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeTargetsAssignOrRejectBeforeHostEffects(
        AwkGoldenCase testCase, bool utf8)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(),
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
regexEngine: AwkUtf8RegexEngine.Instance);

        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation),
            CancellationToken.None, options);
        Assert.Equal(testCase.ExpectedExitCode, exit.Value);
        Assert.Equal(testCase.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, host.OpenPipeCount);
        if (testCase.ExpectedExitCode != 0)
        {
            Assert.Empty(host.Commands);
            Assert.Empty(host.Writes);
            Assert.Equal(0, files.ReadOpenCount);
        }
        else
        {
            var command = Assert.Single(host.Commands);
            Assert.Equal("cat data", command.CommandName);
            Assert.Equal(invocation.CurrentDirectory, command.CurrentDirectory);
            Assert.Contains(command.Environment, entry => entry.Name == "LC_ALL" && entry.Value == "C.UTF-8");
        }
    }

    [Theory]
    [InlineData("BEGIN{print \"before\";if(0)getline $i++ < \"data\"}")]
    [InlineData("BEGIN{print \"before\";if(0)\"cat data\" | getline $i--}")]
    [InlineData("function unused(){getline $0++}BEGIN{print \"before\"}")]
    public void InvalidDestinationIsRejectedInUnreachableCode(string script)
    {
        var program = AwkBinder.Bind(AwkParser.Parse(script));

        Assert.Contains(program.Diagnostics, diagnostic => diagnostic.Message ==
            "Postfix field mutation is not a getline destination; put index mutations inside $(...).");
    }
}
