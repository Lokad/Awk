namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(2, false)] [InlineData(2, true)]
    [InlineData(5, false)] [InlineData(5, true)]
    [InlineData(16, false)] [InlineData(16, true)]
    [InlineData(65, false)] [InlineData(65, true)]
    public async Task ContiguousCallArgumentsShareStagingAcrossArities(int arity, bool governed)
    {
        var names = Enumerable.Range(0, arity).Select(x => "p" + x).ToArray();
        var arguments = string.Join(',', Enumerable.Range(0, arity));
        var source = "function sum(" + string.Join(',', names) + "){return " + string.Join('+', names) +
            "}BEGIN{for(i=0;i<32;i++)total=sum(" + arguments + ");print total}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, result.Value);
        Assert.Equal((arity * (arity - 1) / 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(32, counters.BoundFunctionRuns);
        Assert.Equal(counters.BoundFunctionRuns, counters.FunctionArgumentSegments);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData("single", false)] [InlineData("single", true)]
    [InlineData("rules", false)] [InlineData("rules", true)]
    [InlineData("loop", false)] [InlineData("loop", true)]
    public async Task RepeatedPreparationKeepsConstantsAcrossRulesAndRecycledFunctionFrames(string shape, bool governed)
    {
        const int records = 128;
        var body = shape switch
        {
            "rules" => "{total+=one($1)}NR%2==0{total+=two($1)}{total+=3}",
            "loop" => "{for(i=0;i<3;i++)total+=one(two($1))}",
            _ => "{total+=one($1)+3}"
        };
        var source = "function one(x){return x+1}function two(x){return x+2}" + body + "END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(shape switch { "rules" => "1984\n", "loop" => "3840\n", _ => "1408\n" },
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.RecordsRead);
        Assert.Equal(0, counters.FunctionNameLookups);
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }

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
        Assert.Equal(functions == "used" ? records * count * iterations : 0, counters.BoundFunctionRuns);
        Assert.Equal(records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 2);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
