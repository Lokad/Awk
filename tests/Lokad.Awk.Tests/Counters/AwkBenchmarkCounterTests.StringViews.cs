namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, "substr($2,1)", "é😀z", 3)]
    [InlineData(true, "substr($2,1)", "é😀z", 3)]
    [InlineData(false, "substr($2,1,3)", "é😀z", 3)]
    [InlineData(true, "substr($2,1,3)", "é😀z", 3)]
    [InlineData(false, "tolower($2)", "abcdef", 6)]
    [InlineData(true, "tolower($2)", "abcdef", 6)]
    public async Task WholeStringResultsReuseTheExistingFieldOwner(bool governed, string expression, string input, int length)
    {
        const int records = 32, repetitions = 8;
        var source = "{for(i=0;i<8;i++){kept=" + expression + ";total+=length(kept)}}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("key " + input + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * repetitions * length) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters[AwkCounter.TextAllocations], records, records + 32);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedNonlocalSlicesReuseTheOwnersExistingIndex(bool governed)
    {
        const int records = 8, repetitions = 16, characters = 4096;
        var source = "{for(i=0;i<16;i++)total+=length(substr($2,2049,2))}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("key " + new string('é', characters) + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal((records * repetitions * 2) + "\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 2048)]
    [InlineData(true, 2048)]
    [InlineData(false, 16384)]
    [InlineData(true, 16384)]
    public async Task ShiftedAsciiSliceDoesNotOwnItsSourceField(bool governed, int fieldLength)
    {
        var half = fieldLength / 2;
        var host = new MockFileSystem();
        host.SetStandardInput("key " + new string('a', half) + "ID" + new string('z', half - 2) + "\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", $"{{ kept = substr($2, {half + 1}, 2) }} END {{ print kept }}"),
            CancellationToken.None, CreateOptions(counters) with { MemoryBudget = governed ? budget : null });

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("ID\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters[AwkCounter.TextBytes], 2, fieldLength - 1);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ImmediateAsciiCasingOwnsOnlyTheEscapingResult(bool governed, bool upper)
    {
        const int records = 128;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("key AbCdEfghij\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = "{kept=" + (upper ? "toupper" : "tolower") + "($2);total+=length(kept)}END{print total}";
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("1280\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters[AwkCounter.TextAllocations], records, records + 32);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ImmediateSubstringOwnsOnlyTheEscapingSlice(bool governed, bool dynamic)
    {
        const int records = 128;
        var source = "BEGIN{column=2}{kept=substr(" + (dynamic ? "$(column)" : "$2") +
            ",1,2);total+=length(kept)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("key résumé\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("256\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters[AwkCounter.TextAllocations], records, records + 32);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ImmediateIndexDoesNotOwnEitherField(bool governed, bool dynamic)
    {
        const int records = 128;
        var source = "BEGIN{column=1}{total+=index(" + (dynamic ? "$(column)" : "$1") +
            ",$2)}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("résumé sum\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var exit = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("384\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters[AwkCounter.TextAllocations], 0, 32);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
        Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
