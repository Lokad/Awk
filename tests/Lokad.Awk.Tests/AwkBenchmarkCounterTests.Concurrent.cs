namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedCountersKeepCompletedCommandTotalsUnderConcurrentExecution(bool enabled)
    {
        const int executions = 8;
        const int records = 256;
        var counters = enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        await Task.WhenAll(Enumerable.Range(0, executions).Select(_ => Task.Run(async () =>
        {
            var host = new MockFileSystem();
            host.SetStandardInput(string.Concat(Enumerable.Repeat("7\n", records)));
            var exit = await AwkExecutor.ExecuteAsync(host,
                AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk",
                    "{values[$1]+=$1} END{print values[\"7\"]}")),
                CancellationToken.None, CreateOptions(counters));
            Assert.Equal(0, exit.Value);
            Assert.Equal("1792\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        })));

        Assert.Equal(enabled ? executions * records : 0, counters.RecordsRead);
        Assert.Equal(enabled ? executions * "1792\n".Length : 0, counters.OutputBytes);
    }
}
