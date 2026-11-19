namespace Lokad.Awk.Tests;

public sealed class AwkParagraphPipeEofTests
{
    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var (testCase, commandCount) in AwkGoldenCorpus.PipeParagraphEofCases)
            yield return [testCase, commandCount];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeParagraphEofMatchesTheSelectedReferenceContract(
        AwkGoldenCase testCase, int commandCount)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var tool = AwkTestHarness.ParseAwk(invocation);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var exit = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), deadline.Token);

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(testCase.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        host.AssertInputCommands(invocation, commandCount);
    }
}
