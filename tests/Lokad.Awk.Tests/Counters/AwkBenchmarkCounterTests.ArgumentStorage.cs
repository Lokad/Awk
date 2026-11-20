namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, 5)]
    [InlineData(true, false, 5)]
    [InlineData(false, true, 5)]
    [InlineData(true, true, 5)]
    [InlineData(false, false, 65)]
    [InlineData(true, false, 65)]
    [InlineData(false, true, 65)]
    [InlineData(true, true, 65)]
    public async Task RepeatedOutputArgumentsReuseClearedCapacity(bool governed, bool printf, int arity)
    {
        const int records = 32;
        var operands = string.Join(",", Enumerable.Repeat("$1+0", arity));
        var format = string.Join(":", Enumerable.Repeat("%d", arity));
        var statement = printf ? "printf \"" + format + "\\n\"," + operands : "print " + operands;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var invocation = AwkTestHarness.BuildInvocation("awk", "{" + statement + "}");
        var result = await AwkExecutor.ExecuteAsync(host, invocation, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        var expected = string.Join(printf ? ":" : " ", Enumerable.Repeat("7", arity)) + "\n";
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal(string.Concat(Enumerable.Repeat(expected, records)), host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ScalarBuiltinArgumentsUseExecutionOwnedStorage(bool governed, bool prepared)
    {
        const int records = 128;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = prepared
            ? "function use(x){return int(x)}{sum+=use($1)}END{print sum}"
            : "{sum+=sqrt($1)}END{print int(sum)}";
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(prepared ? "896\n" : "338\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
