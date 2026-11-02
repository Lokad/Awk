namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> FieldComparisonConsumptionCases()
    {
        foreach (var shape in new[] { "number", "reverse", "variable", "fields", "text", "warm", "mixed", "escape", "named-condition", "named-body", "do" })
        foreach (var governed in new[] { false, true }) yield return [shape, governed];
    }

    [Theory]
    [MemberData(nameof(FieldComparisonConsumptionCases))]
    public async Task ComparisonsConsumeScopedFieldsAndRetainOwnedEscapes(string shape, bool governed)
    {
        const int records = 128;
        var body = shape switch
        {
            "reverse" => "total+=(0<$1)",
            "variable" => "x=0;total+=($1>x)",
            "fields" => "total+=($1==$2)",
            "text" => "s=\"0007\";total+=($1==s)",
            "warm" => "sum+=$1;total+=($1>0)",
            "mixed" => "total+=($1>0);sum+=$1;total+=($1<10)",
            "escape" => "total+=($1>0);spelling=$1",
            "named-condition" => "if(positive($1)>0)total+=positive($1)",
            "named-body" => "if($1>0)total+=positive($1)",
            "do" => "i=0;do{total+=int($1)>0}while(++i<2)",
            _ => "total+=($1>0)"
        };
        var source = (shape.StartsWith("named-", StringComparison.Ordinal) ? "function positive(x){return x+0}" : "") +
            "{" + body + "}END{print total" +
            (shape == "escape" ? ",spelling" : "") + "}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 7\n", records)));
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        var total = records * (shape is "mixed" or "do" ? 2 : shape is "named-condition" or "named-body" ? 7 : 1);
        Assert.Equal(0, exit.Value);
        Assert.Equal(total.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            (shape == "escape" ? " 0007" : "") + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.SynchronousActionRuns + counters.BoundRegionRuns);
        Assert.Equal(shape == "do" ? records : 0, counters.SynchronousLoopRuns);
        Assert.Equal(shape == "named-condition" ? 2 * records : shape == "named-body" ? records : 0,
            counters.SynchronousFunctionRuns);
        Assert.Equal(records * (shape is "fields" or "warm" ? 2 : 1), counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, shape == "escape" ? records : 0,
            shape == "escape" ? records + 4 : 2);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
