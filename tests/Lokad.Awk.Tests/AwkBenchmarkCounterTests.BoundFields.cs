namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, " ")] [InlineData(true, " ")]
    [InlineData(false, "|")] [InlineData(true, "|")]
    public async Task SequentialFieldReadsReuseAdmittedDescriptors(bool governed, string separator)
    {
        const int records = 32;
        const int fields = 128;
        var host = new MockFileSystem();
        var row = string.Join(separator, Enumerable.Repeat("000007", fields)) + "\n";
        host.SetStandardInput(string.Concat(Enumerable.Repeat(row, records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = "BEGIN{FS=\"" + separator + "\"}{for(i=1;i<=128;i++)total+=$i}END{print total}";
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("28672\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records * fields, counters.NumericFieldByteParses);
        Assert.Equal(records * fields, counters.FieldsMaterialized);
        // Capacity grows only for the first record. Do not pin a particular growth factor.
        Assert.InRange(counters.FieldPrefixStages, 1, 16);
        Assert.InRange(counters.FieldDescriptorCopies, 0, fields * 2);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("$column==\"0007\"", false)] [InlineData("$column==\"0007\"", true)]
    [InlineData("accept(column)", false)] [InlineData("accept(column)", true)]
    public async Task RuleConditionsAndActionsShareBoundExecutionAndBorrowedFields(string condition, bool governed)
    {
        const int records = 1024;
        var source = "function accept(n){return $n==\"0007\"}BEGIN{column=1}" + condition +
            "{total+=$2}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 3\nmiss 99\n", records / 2)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1536\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("column", false)] [InlineData("column", true)]
    [InlineData("column+0", false)] [InlineData("column+0", true)]
    [InlineData("select(column)", false)] [InlineData("select(column)", true)]
    public async Task ComputedFieldsReuseNumericCachesAndBorrowedArrayKeys(string index, bool governed)
    {
        const int records = 1024;
        var source = "function select(n){return n}{column=1;total+=$((" + index + "))+$column;sum[$column]+=$2}END{print total,sum[\"0007\"],length(sum)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007 3 trailing fields\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("14336 3072 1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(2 * records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
