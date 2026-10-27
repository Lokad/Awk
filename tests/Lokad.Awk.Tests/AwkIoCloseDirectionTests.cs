using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkIoCloseDirectionTests
{
    public static IEnumerable<object[]> CloseCases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test =>
            test.Name.StartsWith("close-mode-", StringComparison.Ordinal) ||
            test.Name.StartsWith("file-open-visible-", StringComparison.Ordinal))
            .Concat(AwkGoldenCorpus.CloseModePipeCases()))
        foreach (var governed in new[] { false, true })
            yield return [test, governed];
    }

    [Theory]
    [MemberData(nameof(CloseCases))]
    public async Task PublicClosePreservesStreamStatusAndLifetime(AwkGoldenCase test, bool governed)
    {
        var files = AwkTestHarness.BuildFileSystem(test);
        var input = test.Name.StartsWith("close-mode-pipe-True-", StringComparison.Ordinal);
        var host = new AwkEmptyPipeTestHost(files) { InputRecord = "row\n", CommandStatus = input ? 7 : 17 };
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", test.Arguments, test.Environment));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(test.ExpectedExitCode, (await awk.ExecuteAsync(host, policy, watchdog.Token)).Value);
        Assert.Equal(test.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, files.GetOutput(AwkFileDescriptor.StdErr));
        foreach (var file in test.ExpectedFiles) Assert.Equal(file.Content, files.ReadFile(file.Path));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void AwkRedirectedOutput_StoresTypedPath()
    {
        var path = new AwkRedirectionPath("/out.txt");
        var output = AwkTestHarness.CreateRedirectedOutput(
            path.Text,
            AwkOutputRedirectionKind.Write,
            "content");

        Assert.Equal(path, output.RedirectionPath);
        Assert.Equal("/out.txt", output.RedirectionPath.Text);
    }
}
