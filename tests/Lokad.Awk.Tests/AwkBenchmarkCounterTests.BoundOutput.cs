namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> FixedOutputWidths()
    {
        foreach (var width in new[] { 2, 4, 5, 16, 65 })
        foreach (var grouped in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [width, grouped, governed];
    }

    [Theory]
    [MemberData(nameof(FixedOutputWidths))]
    public async Task FixedFieldOutputKeepsByteViewsAcrossWidthsAndGrouping(int width, bool grouped, bool governed)
    {
        const int records = 128;
        var fields = Enumerable.Range(1, width).Select(index => index == 1 ? "0007" : "café" + index).ToArray();
        var ordinals = Enumerable.Range(1, width).Reverse();
        var operands = string.Join(",", ordinals.Select(index =>
        {
            var ordinal = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return grouped ? "$( (" + ordinal + ") )" : "$" + ordinal;
        }));
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(string.Join(" ", fields) + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{print " + operands + "}"), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(string.Concat(Enumerable.Repeat(string.Join(" ", fields.Reverse()) + "\n", records)),
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.FieldListPrintRuns);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.GovernedArgumentBufferAllocations);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
