namespace Lokad.Awk.Tests;

public sealed class AwkDirectStringBuiltinTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedAsciiCaseUsesOwnedChunkConversionsAtOriginalBoundaries(bool upper)
    {
        const int changedBytes = 2 * AwkRuntimeLimits.CancellationCheckBytes + 37;
        foreach (var prefixBytes in new[] { 0, 1, 4_095, 4_096, 4_097 })
        {
            var state = State(AwkExecutionMemoryOwner.Disabled);
            var prefix = new string('_', prefixBytes);
            var source = AwkValue.FromText(prefix + new string(upper ? 'a' : 'A', changedBytes));
            var result = AwkBuiltins.EvaluateCaseValue(state, source, upper);

            Assert.Equal(prefixBytes + changedBytes, result.ByteText.GetRuneLength(state.CancellationCheckpoint));

            Assert.True(result.ByteText.IsAscii(out var inspectedBytes));
            Assert.Equal(0, inspectedBytes);
            Assert.Equal(prefixBytes + changedBytes, state.CountStringUnits(result.ByteText));
            Assert.Equal(prefix + new string(upper ? 'A' : 'a', changedBytes), result.AsString());

            var offset = prefixBytes % AwkRuntimeLimits.CancellationCheckBytes;

            Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
            Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        }
        var alphabet = new string(Enumerable.Range(0, 128).Select(value => (char)value).ToArray());
        var alphabetState = State(AwkExecutionMemoryOwner.Disabled);
        var converted = AwkBuiltins.EvaluateCaseValue(alphabetState, AwkValue.FromText(alphabet), upper);
        Assert.Equal(upper ? alphabet.ToUpperInvariant() : alphabet.ToLowerInvariant(), converted.AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaseChunksKeepUnchangedSharingAndUnicodeFallback(bool upper)
    {
        var state = State(AwkExecutionMemoryOwner.Disabled);
        var stable = AwkText.FromString(upper ? "ALREADY-09" : "already-09");
        var result = AwkBuiltins.EvaluateCaseValue(state, AwkValue.FromText(stable), upper);
        Assert.True(stable.HasSameStorage(result.ByteText));

        var unicode = AwkValue.FromText(upper ? "café" : "CAFÉ");
        Assert.Equal(upper ? "CAFÉ" : "café", AwkBuiltins.EvaluateCaseValue(state, unicode, upper).AsString());

        Assert.Equal(1, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(1, state.Counters[AwkCounter.Utf8Encodes]);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, regexEngine: AwkUtf8RegexEngine.Instance)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }
}
