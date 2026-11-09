namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkBoundedSubstringTests
{
    [Theory]
    [InlineData(false, "a")] [InlineData(true, "a")]
    [InlineData(false, "é")] [InlineData(true, "é")]
    [InlineData(false, "😀")] [InlineData(true, "😀")]
    public void ShortResultsDoNotPrepareTheRemainingText(bool governed, string first)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        using (var state = State(memory.Owner, counters))
        {
            foreach (var length in new[] { 0, 1 })
            foreach (var numeric in new[] { false, true })
            {
                var lengthValue = numeric ? AwkValue.FromNumber(length) : AwkValue.FromText(length.ToString());
                _ = AwkBuiltins.EvaluateSubstrValues(state, AwkValue.FromText(first), AwkValue.FromNumber(1), lengthValue, 3);
                var source = AwkValue.FromText(first + new string('q', 1_000_000));
                var checks = counters.CancellationChecks;
                var indices = counters.RuneSliceIndexAllocations;
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                var result = AwkBuiltins.EvaluateSubstrValues(state, source, AwkValue.FromNumber(1), lengthValue, 3);
                Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - allocated, 0, 8192);
                Assert.InRange(counters.CancellationChecks - checks, 0, 32);
                Assert.Equal(System.Text.Encoding.UTF8.GetBytes(length == 0 ? "" : first), result.ByteText.AsSpan().ToArray());
                Assert.Equal(indices, counters.RuneSliceIndexAllocations);
                Assert.InRange(counters.RuneSliceBytesScanned, 0, 256);
            }
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OutOfRangeStartDoesNotDemandInvalidLengthConversion(bool bytes)
    {
        using var state = State(AwkExecutionMemoryOwner.Disabled, AwkRuntimeCounters.Disabled, bytes);
        var invalid = AwkValue.FromText("0x1.1", AwkNumberParsingMode.NonDecimalData);
        Assert.Throws<AwkRuntimeException>(() => invalid.AsNumber());
        var source = AwkValue.FromText("é");
        var outside = AwkValue.FromNumber(bytes ? 3 : 2);
        var result = AwkBuiltins.EvaluateSubstrValues(state, source, outside, invalid, 3);
        Assert.Empty(result.ByteText.AsSpan().ToArray());
    }

    [Theory]
    [InlineData(false, "é")] [InlineData(true, "é")]
    [InlineData(false, "a\0é😀e\u0301z")] [InlineData(true, "a\0é😀e\u0301z")]
    [InlineData(false, "界界界界界界界界界界界界界界界界界界界界界a")]
    [InlineData(true, "界界界界界界界界界界界界界界界界界界界界界a")]
    public void ShortScalarSlicesNeedNoWholeTextIndex(bool governed, string input)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        using (var state = State(memory.Owner, counters))
        {
            var source = AwkValue.FromText(input);
            var scalars = input.EnumerateRunes().ToArray();
            foreach (var start in new[] { -1, 0, 1, 2, scalars.Length, scalars.Length + 1, int.MaxValue })
            foreach (var length in new[] { -1, 0, 1, 2, int.MaxValue })
            {
                var expected = length <= 0 ? "" : string.Concat(scalars.Skip(Math.Max(1, start) - 1)
                    .Take(length).Select(rune => rune.ToString()));
                var result = AwkBuiltins.EvaluateSubstrValues(state, source,
                    AwkValue.FromNumber(start), AwkValue.FromNumber(length), 3);
                Assert.Equal(System.Text.Encoding.UTF8.GetBytes(expected), result.ByteText.AsSpan().ToArray());
            }
            var suffix = AwkBuiltins.EvaluateSubstrValues(state, source,
                AwkValue.FromNumber(2), AwkValue.Unassigned(), 2);
            Assert.Equal(string.Concat(scalars.Skip(1).Select(rune => rune.ToString())), suffix.ByteText.AsString());
            Assert.Equal(0, counters.RuneSliceIndexAllocations);
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters, bool bytes = false)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryOwner = owner };
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, counters, CancellationToken.None),
            bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default);
    }
}
