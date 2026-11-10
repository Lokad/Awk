namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public async Task MixedBuiltinParametersOnlyOwnObservableFieldSpellings(bool governed, bool forward, bool observable)
    {
        const int records = 128;
        var source = "function part(s,p,n){" +
            (observable ? "if(p!=\"0002\"||n!=\"0003\")return \"MISMATCH\";" : "") +
            "return substr(s,p,n)}" +
            (forward ? "function relay(s,p,n){return part(s,p,n)}" : "") +
            "{sum+=length(" + (forward ? "relay" : "part") + "($1,$2,$3))}END{print sum}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("abcdef 0002 0003\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("384\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        var expected = records * (observable ? 4 : 2);
        Assert.InRange(counters.AwkTextAllocations, expected, expected + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> KeyParameterConsumptions()
    {
        foreach (var governed in new[] { false, true })
        foreach (var forward in new[] { false, true })
        foreach (var arity in new[] { 2, 9 })
        foreach (var key in new[] { "0007", "é😀", "a\0b", "abcdefgh" })
        foreach (var observable in new[] { false, true })
            yield return [governed, forward, arity, key, observable];
    }

    [Theory]
    [MemberData(nameof(KeyParameterConsumptions))]
    public async Task KeyOnlyArgumentsOwnBytesWithoutFullFieldScalars(bool governed, bool forward, int arity, string key, bool observable)
    {
        const int records = 128;
        var extras = string.Concat(Enumerable.Range(2, arity - 2).Select(i => ",p" + i));
        var args = string.Concat(Enumerable.Repeat(",0", arity - 2));
        var source = "function update(a,k" + extras + "){" + (observable ? "observed=length(k);" : "") +
            "a[k]++;return a[k]}" + (forward ? "function relay(a,k" + extras + "){return update(a,(k)" + args + ")}" : "") +
            "{total+=" + (forward ? "relay" : "update") + "(a,$1" + args + ")}END{print total,length(a)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(key + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var result = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal("8256 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.FieldsMaterialized);
        if (observable) Assert.True(counters.FieldValueCacheBufferAllocations > 0);
        else Assert.Equal(0, counters.FieldValueCacheBufferAllocations);
        if (observable || AwkUtf8Codec.Encode(key).Length > 7)
            Assert.InRange(counters.AwkTextAllocations, records, records + 32);
        else Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(records * (forward ? 2 : 1), counters.BoundFunctionRuns);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

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
        Assert.Equal(records, counters.BoundFunctionRuns);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
