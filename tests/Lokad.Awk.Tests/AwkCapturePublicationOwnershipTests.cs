using Xunit.Abstractions;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCapturePublicationOwnershipTests(ITestOutputHelper output)
{
    private static readonly AwkArrayName Target = AwkArrayName.FromText("captures");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatorChangesAndOptionalGroupsKeepCurrentKeys(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        foreach (var separator in new[] { "\u001c", "", "é😀", "different" })
        {
            state.AssignVariableValue(AwkVariableName.FromText("SUBSEP"), AwkValue.FromText(separator));
            using var match = Match(state);
            state.ReplaceMatchCaptureArray(Target, match);
            Assert.True(state.TryGetExistingArrayCount(Target, out var count));
            Assert.Equal(6, count);
            Assert.Equal("s", state.GetArrayByteText(Target, AwkArrayKey.FromOrdinal(1)).AsString());
            Assert.Equal(1, state.GetArrayAwkValue(Target, new AwkArrayKey("1" + separator + "start")).AsNumber());
            Assert.Equal(1, state.GetArrayAwkValue(Target, new AwkArrayKey("1" + separator + "length")).AsNumber());
            Assert.False(state.TryPeekArrayAwkValue(Target, AwkArrayKey.FromOrdinal(2), out _));
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
    }
    [Fact]
    public void RefusedPublicationKeepsPreviousArrayAndReleasesScratch()
    {
        using var memoryForState = State(true);
        var state = memoryForState.State;
        state.SetArrayString(Target, new AwkArrayKey("kept"), "old");
        using var match = Match(state);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.ReplaceMatchCaptureArray(Target, match));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("old", state.GetArrayByteText(Target, new AwkArrayKey("kept")).AsString());
        Assert.True(state.TryGetExistingArrayCount(Target, out var count));
        Assert.Equal(1, count);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void EmptyCaptureSetNeedsNoKeyBuilderAdmission()
    {
        const int limit = 1 << 20;
        using var memoryForState = State(true);
        var state = memoryForState.State;
        state.SetArrayString(Target, new AwkArrayKey("kept"), "old");
        using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.CurrentCommittedBytes);
        state.ReplaceMatchCaptureArray(Target, AwkLeasedRegexMatch.NoMatch);
        Assert.True(state.TryGetExistingArrayCount(Target, out var count));
        Assert.Equal(0, count);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void CanceledLongKeyConstructionKeepsPreviousArray()
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(true, token: cancellation.Token);
        var state = memoryForState.State;
        state.AssignVariableValue(AwkVariableName.FromText("SUBSEP"), AwkValue.FromText(new string('s', 8192)));
        state.SetArrayString(Target, new AwkArrayKey("kept"), "old");
        using var match = Match(state);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.ReplaceMatchCaptureArray(Target, match));
        Assert.True(state.TryGetExistingArrayCount(Target, out var count));
        Assert.Equal(1, count);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedFailedMatchesKeepAllocationBounded(bool finite)
    {
        using var memoryForState = State(finite);
        var state = memoryForState.State;
        for (var index = 0; index < 100; index++)
            state.ReplaceMatchCaptureArray(Target, AwkLeasedRegexMatch.NoMatch);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            state.ReplaceMatchCaptureArray(Target, AwkLeasedRegexMatch.NoMatch);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine("10,000 failed-match capture replacements: {0} bytes; finite: {1}.", allocated, finite);
        Assert.InRange(allocated, 0, 1_000_000);
        Assert.True(state.TryGetExistingArrayCount(Target, out var count));
        Assert.Equal(0, count);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkLeasedRegexMatch Match(AwkRuntimeState state)
    {
        var text = AwkText.FromString("s");
        AwkRegexGroup[] groups = [new(true, 0, 1, text), new(true, 0, 1, text), new(false, 0, 0, AwkText.Empty)];
        var match = new AwkRegexMatch(true, 0, 1, 0, 1, text, groups);
        var retained = groups.Sum(group => group.Value.EstimatedRetainedMemoryBytes);
        return new(match, state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateRegexMatchBytes(groups.Length, retained)), retained);
    }

    private static CooperativeTestMemory State(bool finite, CancellationToken token = default)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: finite && (long)(1 << 20) != long.MaxValue);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, limits,
            AwkRegexEngineKind.DotNetCompatibility);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options),
            new AwkRuntimeResourceMonitor(limits, AwkRuntimeCounters.Disabled), AwkCompatibilityProfile.Default));
    }
}
