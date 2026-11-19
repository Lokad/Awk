namespace Lokad.Awk.Tests;

public sealed class AwkGetlinePostfixTests
{
    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var testCase in AwkGoldenCorpus.PipeGetlinePostfixCases())

            yield return [testCase];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeTargetsAssignOrRejectBeforeHostEffects(
        AwkGoldenCase testCase)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var tool = AwkTestHarness.ParseAwk(invocation);

        var exit = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);
        Assert.Equal(testCase.ExpectedExitCode, exit.Value);
        Assert.Equal(testCase.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        if (testCase.ExpectedExitCode != 0)
        {
            Assert.Empty(host.Commands);
            Assert.Empty(host.Writes);
            Assert.Equal(0, files.ReadOpenCount);
        }
        else
        {
            host.AssertInputCommands(invocation, 1);
        }
    }

    [Theory]
    [InlineData("BEGIN{print \"before\";if(0)getline $i++ < \"data\"}")]
    [InlineData("BEGIN{print \"before\";if(0)\"cat data\" | getline $i--}")]
    [InlineData("function unused(){getline $0++}BEGIN{print \"before\"}")]
    public void InvalidDestinationIsRejectedInUnreachableCode(string script)
    {
        var program = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(script, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.Contains(program.Diagnostics, diagnostic => diagnostic.Message ==
            "Postfix field mutation is not a getline destination; put index mutations inside $(...).");
    }
}
