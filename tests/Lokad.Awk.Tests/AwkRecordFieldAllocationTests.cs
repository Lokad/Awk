namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordFieldAllocationTests
{
    [Fact]
    public void CachedFieldsAndFieldCountDoNotAllocateOnRepeatedReads()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, options);
        state.SetCurrentRecord(AwkText.FromString("alpha beta"));
        var fieldCount = AwkSpecialVariables.VariableName(AwkSpecialVariableId.FieldCount);
        for (var index = 0; index < 100; index++)
        {
            _ = state.GetFieldValue(1);
            _ = state.FieldCount;
            _ = state.ResolveVariableValue(fieldCount);
        }
        var textAllocations = counters.AwkTextAllocations;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;

        for (var index = 0; index < 10_000; index++)
        {
            total += state.GetFieldValue(1).ByteText.ByteLength;
            total += state.FieldCount;
            total += (int)state.ResolveVariableValue(fieldCount).AsNumber();
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(90_000, total);
        Assert.Equal(0, allocatedBytes);
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(2, counters.FieldsMaterialized);
        Assert.Equal(textAllocations, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }
}
