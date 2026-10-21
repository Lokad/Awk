namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCaseMemoryTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task LargeCaseResultIsAdmittedBeforeAllocation(bool upper, bool bytes)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(owner, bytes);
            var source = AwkValue.FromText((upper ? "éa" : "ÉA") + new string('0', 1_000_000));
            _ = source.ByteText.AsString(); // The source's existing decode is not conversion scratch.
            var before = 0L;
            var allocatedBeforeRefusal = 0L;
            budget.OnRefusal = () => allocatedBeforeRefusal = GC.GetAllocatedBytesForCurrentThread() - before;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            before = GC.GetAllocatedBytesForCurrentThread();
            var failure = Assert.Throws<AwkRuntimeException>(() => AwkBuiltins.EvaluateCaseValue(state, source, upper));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.InRange(allocatedBeforeRefusal, 0, 64 * 1024);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MixedByteCaseStaysByteNativeAndReusesUnchangedText(bool upper)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(owner, true);
            var text = (upper ? "éaΩ" : "ÉAΩ") + new string(upper ? 'a' : 'A', 8192) + "😀";
            var source = AwkValue.FromText(text);
            var result = AwkBuiltins.EvaluateCaseValue(state, source, upper);
            Assert.Equal(0, state.Counters.StringDecodes);
            Assert.Equal(0, state.Counters.Utf8Encodes);
            Assert.Equal(source.ByteText.ByteLength, state.Counters.AsciiCasePreparationBytes);
            Assert.True(state.Counters.AsciiCaseConversionBytes >= 8192);
            Assert.Equal((upper ? "éAΩ" : "ÉaΩ") + new string(upper ? 'A' : 'a', 8192) + "😀", result.AsString());
            Assert.Same(result.ByteText, AwkBuiltins.EvaluateCaseValue(state, result, upper).ByteText);
        }
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, bool bytes)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits) { MemoryOwner = owner };
        var profile = bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default;
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, options.Counters), profile);
    }
}
