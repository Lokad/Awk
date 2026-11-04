namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkConcatAllocationTests
{
    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 5)]
    [InlineData(false, 8)]
    [InlineData(false, 9)]
    [InlineData(true, 2)]
    [InlineData(true, 5)]
    [InlineData(true, 8)]
    [InlineData(true, 9)]
    public void RepeatedConcatenationDoesNotAllocateArgumentStorage(bool governed, int arity)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        using var memory = new CooperativeTestMemory(64L << 20, enabled: governed);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options))).State;
        var source = "BEGIN{result=" + string.Join(' ', Enumerable.Range(0, arity)
            .Select(index => index == arity / 2 ? "\"café\"" : "\"\"")) + "}";
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(state);
        var result = AwkVariableName.FromText("result");
        for (var index = 0; index < 100; index++)
            if (region.Execute(state, storage) != AwkStatementFlow.Continue)
                throw new InvalidOperationException("Unexpected concat control flow.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var bytes = 0;
        for (var index = 0; index < 1_000; index++)
        {
            if (region.Execute(state, storage) != AwkStatementFlow.Continue)
                throw new InvalidOperationException("Unexpected concat control flow.");
            bytes += state.ResolveVariableValue(result).ByteText.ByteLength;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(5_000, bytes);
        Assert.InRange(allocated, 0, 4_096);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

}
