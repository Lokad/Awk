namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData("literal", false)] [InlineData("literal", true)]
    [InlineData("global", false)] [InlineData("global", true)]
    [InlineData("local", false)] [InlineData("local", true)]
    public async Task PreparedScalarKeysDoNotRecreateValuesOnEveryAccess(string location, bool governed)
    {
        var key = location switch { "literal" => "\"007\"", "global" => "key", _ => "k" };
        var factories = new List<long>();
        foreach (var iterations in new[] { 1, 1024 })
        {
            var source = "function run(k,i){for(i=0;i<" + iterations + ";i++){a[" + key +
                "][\"sum\"]++;if(" + key + " in seen)hits++;seen[" + key + "]=1}return a[" + key +
                "][\"sum\"]}BEGIN{key=\"007\";hits=0;print run(key),hits,length(a),length(seen)}";
            var host = new MockFileSystem();
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
                regexEngine: AwkUtf8RegexEngine.Instance);
            var result = await compiled.ExecuteAsync(host, CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(0, result.Value);
            Assert.Equal($"{iterations} {iterations - 1} 1 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
            factories.Add(counters.ValuesCreated);
        }
        Assert.Equal(factories[0], factories[1]);
    }
}
