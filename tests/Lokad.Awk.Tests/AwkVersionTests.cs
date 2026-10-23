using System.Reflection;

namespace Lokad.Awk.Tests;

public sealed class AwkVersionTests
{
    public static IEnumerable<object[]> Queries()
    {
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var option in new[] { new[] { "--version" }, ["-V"], ["--ver"], ["-Wversion"], ["-W", "version"] })
        foreach (var governed in new[] { false, true })
            yield return [profile, option, governed];
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task VersionQueriesReportOnlyTheBriefArtifactVersion(string profile, string[] option, bool governed)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        string[] arguments = profile.Length == 0 ? option : [profile, .. option];
        Assert.True(Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []), out var awk));

        Assert.Equal(AwkExitCode.Success, await awk.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("Lokad awk " + BriefArtifactVersion() + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("", false)] [InlineData("", true)]
    [InlineData("--posix", false)] [InlineData("--posix", true)]
    [InlineData("--traditional", false)] [InlineData("--traditional", true)]
    public async Task ProcInfoVersionRespectsTheProfileWithoutBuildDetails(string profile, bool governed)
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var source = "BEGIN{print PROCINFO[\"version\"];exit}";
        string[] arguments = profile.Length == 0 ? [source] : [profile, source];
        Assert.True(Awk.TryParse(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []), out var awk));

        Assert.Equal(AwkExitCode.Success, await awk.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal((profile.Length == 0 ? BriefArtifactVersion() : "") + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    private static string BriefArtifactVersion()
    {
        var metadata = typeof(Awk).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Assert.NotNull(metadata);
        return metadata.InformationalVersion.Split('+')[0];
    }
}
