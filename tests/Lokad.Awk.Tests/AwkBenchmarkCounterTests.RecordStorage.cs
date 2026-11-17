using System.Text;

namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    public static IEnumerable<object[]> OwnedRecordWidths()
    {
        foreach (var governed in new[] { false, true })
            foreach (var width in new[] { 4095, 4096, 4097, 8193, 32768, 65536, 65537 })
                foreach (var chunk in new[] { 1023, 131072 })
                    yield return [governed, width, chunk];
    }

    [Theory]
    [MemberData(nameof(OwnedRecordWidths))]
    public async Task OwnedRecordStagingReusesFundedStorageWithoutLargerHostBorrows(
        bool governed, int width, int chunk)
    {
        const int records = 32;
        var record = "0007 " + new string('x', width - 7) + "é";
        var input = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(record + "\n", records)));
        var offset = 0;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, maximum, _) =>
            {
                var count = Math.Min(Math.Min(chunk, maximum), input.Length - offset);
                var bytes = input.AsMemory(offset, count);
                offset += count;
                return ValueTask.FromResult(AwkReadBuffer.FromContent(bytes,
                    offset == input.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData,
                    static _ => { }));
            }
        };
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        var command = AwkTestHarness.BuildInvocation("awk", "{sum+=$1}END{print sum,NR}");
        var exit = await AwkExecutor.ExecuteAsync(host, command, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("224 32\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        if (width <= 65536)
        {
            Assert.Equal(records, counters.BufferedInputRecords);
            Assert.InRange(counters.AwkTextAllocations, 0, 8);
            Assert.True(counters.InputRecordBufferReuses >= records - 2);
        }
        else Assert.Equal(0, counters.BufferedInputRecords);
        if (width > 4096) Assert.Equal(0, counters.BorrowedInputRecords);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, budget.Reserved);
    }
}
