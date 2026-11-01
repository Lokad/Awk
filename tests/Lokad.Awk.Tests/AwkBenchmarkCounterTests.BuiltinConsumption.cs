namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, 1)] [InlineData(true, false, 1)]
    [InlineData(false, true, 1)] [InlineData(true, true, 1)]
    [InlineData(false, false, 2)] [InlineData(true, false, 2)]
    [InlineData(false, true, 2)] [InlineData(true, true, 2)]
    [InlineData(false, false, 65)] [InlineData(true, false, 65)]
    [InlineData(false, true, 65)] [InlineData(true, true, 65)]
    public async Task NumericBuiltinsKeepFieldInputsByteNative(bool governed, bool forwarding, int arity)
    {
        const int records = 128;
        var binary = arity > 1;
        var arguments = "$1" + string.Concat(Enumerable.Repeat(",$2", arity - 1));
        var expression = (binary ? "and(" : "int(") + arguments + ")";
        var definitions = "";
        if (forwarding)
        {
            var parameters = "x" + string.Concat(Enumerable.Range(1, arity - 1).Select(i => ",y" + i));
            definitions = "function use(" + parameters + "){return " + (binary ? "and(" : "int(") + parameters + ")}";
            expression = "use(" + arguments + ")";
        }
        var source = definitions + "{sum+=" + expression + "}END{print sum}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 0003\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * (binary ? 3 : 7)) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.True(counters.NumericFieldByteParses >= records * (binary ? 2 : 1));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
