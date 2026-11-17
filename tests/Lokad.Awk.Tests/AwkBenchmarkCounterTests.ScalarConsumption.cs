namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData("root", false)]
    [InlineData("root", true)]
    [InlineData("block", false)]
    [InlineData("block", true)]
    [InlineData("local", false)]
    [InlineData("local", true)]
    public async Task ArithmeticScalarInputAvoidsUnobservedFieldOwnership(string shape, bool governed)
    {
        var source = shape switch
        {
            "block" => "{{x=$1;" + string.Concat(Enumerable.Repeat("total+=x;", 65)) + "}}END{print total}",
            "local" => "function f(x){x=$1;return x+0}{total+=f(0)}END{print total}",
            _ => "{x=$1;total+=x}END{print total}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n5tail\nword\n", 32)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal(shape == "block" ? "24960\n" : "384\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.TextValueClassifications);
        Assert.Equal(0, counters.FieldValueCacheBufferAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
