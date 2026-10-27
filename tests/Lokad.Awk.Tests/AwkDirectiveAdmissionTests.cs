namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkDirectiveAdmissionTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task LargeCandidateRefusalPrecedesTokenAllocation(bool file, bool include)
    {
        var body = string.Concat(Enumerable.Repeat("x=1;", 25_000));
        var source = include ? "@include \"/empty.awk\" #" + body : "BEGIN{" + body + "} #@";
        var host = new MockFileSystem();
        host.AddFile("/empty.awk", "");
        host.AddFile("/main.awk", source);
        string[] arguments = file ? ["-f", "/main.awk"] : [source];
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments));
        var budget = new CooperativeTestBudget(source.Length * 32L);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget);
        // The first refusal warms frontend/error paths; the next run measures the same contract.
        await awk.ExecuteAsync(host, policy, CancellationToken.None);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\nawk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(file ? 2 : 0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.InRange(allocated, 0, file ? 2 << 20 : 128 << 10);
    }
}
