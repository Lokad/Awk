namespace Lokad.Awk.Tests;

public sealed class AwkOperandPathBoundaryTests
{
    public static IEnumerable<object[]> RestrictedOriginalCases()
    {
        string[] names = ["lazy-exit", "lazy-argv-data", "lazy-delete", "lazy-argc", "lazy-reclassify",
            "lazy-later-exit", "path-main-colon", "path-main-star", "path-main-backslash",
            "path-source-colon", "path-source-star", "path-source-backslash", "cwd-relative-control", "cwd-absolute"];
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test =>
                     names.Any(name => test.Name == "operand-path-" + name)))
        foreach (var governed in new[] { false, true })
            yield return [test, governed];
    }

    [Theory]
    [MemberData(nameof(RestrictedOriginalCases))]
    public async Task OriginalOperandsKeepTheirAuthorityWithoutEagerPathChecks(AwkGoldenCase test, bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput(test.StandardInput);
        foreach (var file in test.InputFiles) host.AddFile(file.Path, file.Content);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", test.Arguments, test.Environment);
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithAccessiblePathRoot(command.CurrentDirectory);
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.True(Awk.TryParse(command, out var awk));
        Assert.Equal(test.ExpectedExitCode, (await awk.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SelectedPathsStillRequireRootAndOriginalOperandAuthority(bool changed, bool governed)
    {
        var host = new MockFileSystem();
        host.AddFile("/jail/safe", "must not open\n");
        var source = changed ? "BEGIN{print \"before\";ARGV[1]=\"/jail/safe\"}{print}"
            : "BEGIN{print \"before\"}{print}";
        var command = new AwkCommandInvocation("awk", [source, changed ? "../../ignored" : "../outside"], [],
            new AwkPath("/jail"), AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr);
        var policy = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithAccessiblePathRoot(new AwkPath("/jail"));
        var budget = new CooperativeTestBudget(1 << 20);
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.True(Awk.TryParse(command, out var awk));
        Assert.Equal(AwkExitCode.FatalError, await awk.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(changed ? "awk: dynamic main-input paths are disabled by execution policy\n"
            : "awk: path is outside the execution policy root\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task ResolutionRefusalPreservesEarlierOutputAndNeverOpensTheFile()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var armed = false;
        var host = new MockFileSystem
        {
            AfterAppend = () =>
            {
                if (armed) return;
                armed = true;
                budget.RefuseNextAdmissionMinimumGrowth = 32 << 10;
            }
        };
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{print \"before\"}{print}END{print \"end\"}", "/" + new string('a', 8192)], []);
        Assert.True(Awk.TryParse(command, out var awk));
        Assert.Equal(AwkExitCode.FatalError, await awk.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None));
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationAfterClosePrecedesTheNextOperandResolution(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        var host = new MockFileSystem { AfterCloseDescriptor = cancellation.Cancel };
        host.AddFile("/safe", "SAFE\n");
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["{print}", "safe", "../../ignored"], []);
        Assert.True(Awk.TryParse(command, out var awk));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => awk.ExecuteAsync(host, policy, cancellation.Token));
        Assert.Equal("SAFE\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("/a:b")]
    [InlineData("/a*b")]
    [InlineData("/a\\b")]
    public void LiteralRawCharactersAgreeWithCanonicalPaths(string path)
    {
        Assert.True(AwkPath.IsValid(path));
        Assert.True(AwkRawPath.TryParse(path, out var raw));
        Assert.Equal(path, raw.ToAbsolute(AwkPath.Root).Path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/a\0b")]
    [InlineData("/a\tb")]
    [InlineData("/a\nb")]
    [InlineData("/a\u007fb")]
    public void RawPathsKeepTheirEmptyAndControlCharacterRestrictions(string path)
    {
        Assert.False(AwkRawPath.IsValid(path));
        Assert.False(AwkRawPath.TryParse(path, out _));
    }
}
