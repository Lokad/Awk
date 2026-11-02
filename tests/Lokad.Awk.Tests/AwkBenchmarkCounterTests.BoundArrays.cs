using System.Globalization;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> BoundArrayWorkloads()
    {
        foreach (var keys in new[] { 16, 1024 })
        foreach (var fields in new[] { 2, 32 })
        foreach (var loop in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [keys, fields, loop, governed];
    }

    [Theory]
    [MemberData(nameof(BoundArrayWorkloads))]
    public async Task BoundArrayProgramsReuseKeysAndNumericFields(int keys, int fields, bool loop, bool governed)
    {
        const int records = 1024;
        var input = new StringBuilder();
        var sums = new long[keys];
        long total = 0;
        for (var row = 0; row < records; row++)
        {
            var key = row % keys;
            var value = row % 97;
            sums[key] += (loop ? 2 : 1) * value;
            total += sums[key];
            input.Append('k').Append(key).Append(' ').Append(value);
            for (var field = 2; field < fields; field++) input.Append(" tail");
            input.Append('\n');
        }
        var source = (loop
            ? "{for(pass=0;pass<2;pass++){sum[$1]+=+$2;count[$1]++}total+=sum[$1]}"
            : "{sum[$1]+=$2;count[$1]++;total+=sum[$1]}") + "END{print total,length(sum),length(count)}";
        var host = new MockFileSystem();
        host.SetStandardInput(input.ToString());
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(total.ToString(CultureInfo.InvariantCulture) + " " + keys + " " + keys + "\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.BoundRegionRuns);
        Assert.True(counters.ArrayCellCommits >= records);
        Assert.Equal(records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 4 * keys + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
