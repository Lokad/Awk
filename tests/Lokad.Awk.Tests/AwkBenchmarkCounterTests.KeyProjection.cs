namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PreparedScalarArgumentsAvoidValueFactoryRoundTrips(bool inputOrigin, bool governed)
    {
        var factories = new List<long>();
        foreach (var iterations in new[] { 1, 1024 })
        {
            // The numeric return isolates transport from unrelated text-consumer factories.
            var source = "function leaf(x){return 3}function forward(x){return leaf(x)}" +
                (inputOrigin ? "{g=$1;" : "BEGIN{g=\"007\";") + "for(i=0;i<" + iterations +
                ";i++)total+=forward(g);print total}";
            var host = new MockFileSystem();
            host.SetStandardInput(inputOrigin ? "007\n" : "");
            var counters = AwkRuntimeCounters.CreateEnabled();
            var budget = new CooperativeTestBudget(8 << 20);
            await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
                AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
            var result = await compiled.ExecuteAsync(host, CancellationToken.None,
                CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
            Assert.Equal(0, result.Value);
            Assert.Equal(iterations * 3 + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, counters.StringDecodes);
            Assert.Equal(0, counters.Utf8Encodes);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(0, host.OpenFileCount);
            factories.Add(counters.ValuesCreated);
        }
        Assert.Equal(factories[0], factories[1]);
    }

    [Fact]
    public async Task CapturedShortKeysAvoidRepeatedPayloadFunding()
    {
        var repeatedFunding = new List<long>();
        foreach (var key in new[] { "key", "long-key-name" })
        {
            var calls = new List<long>();
            foreach (var iterations in new[] { 1, 128 })
            {
                var source = "function add(a,k){a[k][\"sum\"]++}BEGIN{for(i=0;i<" + iterations +
                    ";i++)add(a,\"" + key + "\");print a[\"" + key + "\"][\"sum\"]}";
                var host = new MockFileSystem();
                var counters = AwkRuntimeCounters.CreateEnabled();
                var budget = new CooperativeTestBudget(8 << 20);
                await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
                    AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
                var result = await compiled.ExecuteAsync(host, CancellationToken.None,
                    CreateOptions(counters) with { MemoryBudget = budget });
                Assert.Equal(0, result.Value);
                Assert.Equal(iterations + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(0, counters.StringDecodes);
                Assert.Equal(0, counters.Utf8Encodes);
                Assert.Equal(0, budget.Reserved);
                Assert.Equal(0, host.OpenFileCount);
                calls.Add(budget.ResizeCalls);
            }
            repeatedFunding.Add(calls[1] - calls[0]);
        }
        Assert.True(repeatedFunding[0] < repeatedFunding[1]);
    }

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
