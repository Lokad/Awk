namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> PreparationEntryCases()
    {
        foreach (var count in new[] { 16, 17, 64 })
        foreach (var shape in new[] { "flat", "for", "while", "do" })
        foreach (var functions in new[] { "none", "unused", "used" })
        foreach (var governed in new[] { false, true })
            yield return [count, shape, functions, governed];
    }

    [Theory]
    [MemberData(nameof(PreparationEntryCases))]
    public async Task NumericComputationPreparationDoesNotDependOnUnusedFunctionsOrFlatBlockSize(
        int count, string shape, string functions, bool governed)
    {
        const int records = 4;
        var steps = string.Concat(Enumerable.Repeat(
            functions == "used" ? "sum+=f($1);" : "sum+=int($1);", count));
        var body = shape switch
        {
            "for" => "for(i=0;i<2;i++){" + steps + "}",
            "while" => "i=0;while(i++<2){" + steps + "}",
            "do" => "i=0;do{" + steps + "}while(++i<2)",
            _ => steps
        };
        var source = (functions == "none" ? "" : "function f(x){return int(x)}") +
            "{" + body + "}END{print sum}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        var iterations = shape == "flat" ? 1 : 2;
        Assert.Equal(0, exit.Value);
        Assert.Equal((7 * count * records * iterations).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        if (shape == "flat") Assert.Equal(records, counters.SynchronousActionRuns);
        else Assert.Equal(records, counters.SynchronousLoopRuns);
        Assert.Equal(functions == "used" ? records * count * iterations : 0, counters.SynchronousFunctionRuns);
        Assert.Equal(records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 2);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
