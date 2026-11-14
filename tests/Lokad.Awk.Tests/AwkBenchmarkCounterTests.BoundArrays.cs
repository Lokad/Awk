using System.Globalization;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task CompleteUpdatesAndReadsReuseValidatedCurrentLocations(int shape)
    {
        var (prefix, update, read) = shape switch
        {
            1 => ("BEGIN{k=\"k\"}", "a[k]++", "a[k]"),
            2 => ("", "a[$1]++", "a[$1]"),
            3 => ("BEGIN{SUBSEP=\":\"}", "a[$1,$2]++", "a[$1,$2]"),
            4 => ("", "a[$1][\"sum\"]++", "a[$1][\"sum\"]"),
            5 => ("BEGIN{a[\"x\"]=0;i=1}", "a[i]++", "a[i]"),
            _ => ("", "a[\"k\"]++", "a[\"k\"]")
        };
        const int count = 256;
        var result = await RunAsync([prefix + "{" + update + ";total+=" + read + "}END{print total}"],
            string.Concat(Enumerable.Repeat("k x 1\n", count)));
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("32896\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        // The update still locates its target. A following current-value read
        // removes a dictionary lookup; nested access retains its prefix lookup.
        Assert.InRange(result.Counters.ArrayOwnedHashCalls + result.Counters.ArrayBorrowedHashCalls,
            0, count * (shape == 4 ? 2 : 1) + 64);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NumericIndexReadsKeepStoredNumbersAndSpellingWithoutTextTransport(bool governed)
    {
        const int count = 4097;
        var source = "function sum(p,n, i,s){for(i=1;i<=n;i++)s+=p[i];return s}" +
            "BEGIN{for(i=1;i<=" + count + ";i++)a[i]=i;print sum(a," + count + ");" +
            "split(\"0007 8\",b);i=1;print b[i],typeof(b[i]);delete a[2048];print a[2048]+0,(2048 in a)}";
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("8394753\n0007 strnum\n0 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.NumericArraySlotReads >= count);
        Assert.True(counters.NumericLocalReads >= count);
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> BoundArrayWorkloads()
    {
        foreach (var keys in new[] { 16, 1024 })
        foreach (var composite in new[] { false, true })
        foreach (var fields in new[] { composite ? 3 : 2, 32 })
        foreach (var loop in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [keys, fields, loop, governed, composite];
    }

    [Theory]
    [MemberData(nameof(BoundArrayWorkloads))]
    public async Task BoundArrayProgramsReuseKeysAndNumericFields(int keys, int fields, bool loop, bool governed, bool composite)
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
            input.Append('k').Append(key);
            if (composite) input.Append(" part").Append(key % 4);
            input.Append(' ').Append(value);
            for (var field = composite ? 3 : 2; field < fields; field++) input.Append(" tail");
            input.Append('\n');
        }
        var keyExpression = composite ? "$1,$2" : "$1";
        var valueExpression = composite ? "$3" : "$2";
        var update = "sum[" + keyExpression + "]+=" + (loop ? "+" : "") + valueExpression + ";count[" + keyExpression + "]++";
        var source = (loop ? "{for(pass=0;pass<2;pass++){" + update + "}" : "{" + update + ";") +
            "total+=sum[" + keyExpression + "]}END{print total,length(sum),length(count)}";
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

        Assert.True(counters.ArrayTargetCommits >= records);
        Assert.True(counters.NumericArraySlotReads >= (loop ? 4L : 2L) * records - 2L * keys);
        Assert.Equal(records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 4 * keys + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
