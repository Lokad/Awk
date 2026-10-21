namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkDateFormattingMemoryTests
{
    [Theory]
    [InlineData("%1000000Y")]
    [InlineData("%_1000000Y")]
    [InlineData("%01000000Y")]
    [InlineData("%1000000Q")]
    [InlineData("%01000000Q")]
    [InlineData("%-1000000Q")]
    public void RefusedPaddingAdmissionPrecedesTheLargeManagedAllocation(string format)
    {
        _ = GnuDateFormatting.Format(DateTimeOffset.UnixEpoch, "%Y");
        using var memory = new CooperativeTestMemory(4096);
        using (var admission = memory.Accounting.ReserveAllocationPeak(128))
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var error = Assert.Throws<AwkRuntimeException>(() =>
                GnuDateFormatting.Format(DateTimeOffset.UnixEpoch, format, admission));
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.InRange(allocated, 0, 128 << 10);
        }
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void UnrepresentablePaddingRemainsAnAwkFailureWithoutAPrivateQuota()
    {
        var error = Assert.Throws<AwkRuntimeException>(() =>
            GnuDateFormatting.Format(DateTimeOffset.UnixEpoch, "%2147483647Y"));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    [Fact]
    public async Task RefusedFormattingFailsTheCommandAndPreservesEarlierOutput()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{print \"before\";v=strftime(\"%2000000Y\",0,1);print \"after\"} END{print \"end\"}"));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData("%１１Y", "%１１Y")]
    [InlineData("%-1000000Y", "1970")]
    public async Task LiteralOrUnpaddedFormatsDoNotRequestLargePadding(string format, string expected)
    {
        var budget = new CooperativeTestBudget(64 << 10);
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{print \"before\";print strftime(\"" + format + "\",0,1);print \"after\"}"));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(0, exit.Value);
        Assert.Equal("before\n" + expected + "\nafter\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }
}
