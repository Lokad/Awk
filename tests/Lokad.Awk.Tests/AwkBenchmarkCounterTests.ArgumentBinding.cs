namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> SharedArgumentBindings()
    {
        foreach (var shape in new[] { "global", "local", "array", "omitted-array" })
        foreach (var withSwitch in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [shape, withSwitch, governed];
    }

    [Theory]
    [MemberData(nameof(SharedArgumentBindings))]
    public async Task CallsAcrossSwitchBranchesShareGuardedReferenceBindings(string shape, bool withSwitch, bool governed)
    {
        const int records = 128;
        var boundary = withSwitch ? "switch(0){default:break}" : "";
        var source = shape switch
        {
            "global" => "function leaf(x,y){" + boundary + "return x+y}{x=$1+0;total+=leaf(x,1)}END{print total}",
            "local" => "function leaf(x,y){" + boundary + "return x+y}function outer(x){return leaf(x,1)}{total+=outer($1)}END{print total}",
            "array" => "function leaf(a,k,v){" + boundary + "a[k]+=v;return a[k]}function outer(a,k,v){return leaf(a,k,v)}{total+=outer(values,$1,$2)}END{print total,values[\"key\"]}",
            _ => "function leaf(a,k,v){" + boundary + "a[k]+=v;return a[k]}function outer(k,v,a){return leaf(a,k,v)}{total+=outer($1,$2)}END{print total,(\"a\" in SYMTAB)}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(shape is "global" or "local" ? "0007\n" : "key 2\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var result = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal(shape switch { "array" => "16512 256\n", "omitted-array" => "256 0\n", _ => "1024\n" },
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FunctionNameLookups);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
