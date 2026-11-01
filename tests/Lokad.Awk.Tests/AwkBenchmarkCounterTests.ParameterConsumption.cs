namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> ParameterConsumptions()
    {
        foreach (var governed in new[] { false, true })
        foreach (var ordinary in new[] { false, true })
        foreach (var arity in new[] { 1, 9, 65 })
        foreach (var opaque in new[] { false, true })
            yield return [governed, ordinary, arity, opaque];
    }

    [Theory]
    [MemberData(nameof(ParameterConsumptions))]
    public async Task WholeCalleeConsumptionAvoidsOnlyUnobservableFieldText(bool governed, bool ordinary, int arity, bool opaque)
    {
        const int records = 128;
        var parameters = "x" + string.Concat(Enumerable.Range(1, arity - 1).Select(i => ",p" + i));
        var args = "$1" + string.Concat(Enumerable.Repeat(",0", arity - 1));
        var source = "function score(" + parameters + "){" + (ordinary ? "print \"called\";" : "") +
            (opaque ? "return x}" : "return x+1}") + "{total+=score(" + args + ")}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var result = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal((ordinary ? string.Concat(Enumerable.Repeat("called\n", records)) : "") +
            (records * (opaque ? 7 : 8)) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        if (opaque) Assert.InRange(counters.AwkTextAllocations, records, records + 32);
        else
        {
            Assert.InRange(counters.AwkTextAllocations, 0, 32);
            Assert.True(counters.NumericFieldByteParses >= records);
        }
        Assert.Equal(ordinary ? 0 : records, counters.SynchronousFunctionRuns);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
