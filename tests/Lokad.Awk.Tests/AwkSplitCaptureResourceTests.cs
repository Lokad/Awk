using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkSplitCaptureResourceTests
{
    private static readonly AwkArrayName Fields = AwkArrayName.FromText("fields");
    private static readonly AwkArrayName Separators = AwkArrayName.FromText("separators");
    private static readonly AwkArrayKey Old = new("old");

    [Theory]
    [InlineData("abc", ",", 1, 0, 1)]
    [InlineData(",a,", ",", 3, 2, 3)]
    [InlineData("a b", " ", 2, 1, 3)]
    [InlineData(" a b ", " ", 2, 3, 5)]
    [InlineData(" \t", " ", 0, 1, 1)]
    [InlineData("abc", "", 3, 2, 3)]
    [InlineData("é😀", "", 2, 1, 2)]
    public void SparseCapturesRemainByteNative(string input, string separator, int fields, int captures, int textAllocations)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(AwkRuntimeLimits.CreateDefault(), counters);

        var count = Split(state, input, separator);

        Assert.Equal(fields, count);
        Assert.True(state.TryGetExistingArrayCount(Separators, out var captureCount));
        Assert.Equal(captures, captureCount);
        Assert.Equal(textAllocations, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(1, counters.SeparatorCaptureFallbacks);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("a", ",", 1, 1, 0)]
    [InlineData("a", "", 1, 1, 0)]
    [InlineData("a ", " ", 1, 1, 1)]
    [InlineData(" a", " ", 1, 1, 1)]
    [InlineData(" ", " ", 1, 0, 1)]
    [InlineData("ab", "", 2, 2, 1)]
    public void ElementLimitCountsOnlyPublishedEntries(string input, string separator, int limit, int fields, int captures)
    {
        var state = Seed(State(AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(limit)));

        Assert.Equal(fields, Split(state, input, separator));

        Assert.True(state.TryGetExistingArrayCount(Fields, out var fieldCount));
        Assert.True(state.TryGetExistingArrayCount(Separators, out var captureCount));
        Assert.Equal(fields, fieldCount);
        Assert.Equal(captures, captureCount);
        Assert.False(state.ContainsArrayValue(Fields, Old));
        Assert.False(state.ContainsArrayValue(Separators, Old));
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherReplacementElementLimitFailurePreservesBothArrays(bool patsplit)
    {
        var state = Seed(State(AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1)));
        var before = state.MemoryGovernor.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (patsplit)
                state.PatsplitIntoArray(AwkText.FromSourceLiteralString("a"), Fields,
                    new AwkRegexPattern("a"), AwkOptionalIdentifier.FromText(Separators.Text));
            else
                Split(state, "a,b", ",");
        });

        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        AssertPreserved(state, before);
    }

    [Fact]
    public void CancellationBeforeSplittingReleasesTheCaptureBufferReservation()
    {
        using var cancellation = new CancellationTokenSource();
        var state = Seed(State(AwkRuntimeLimits.CreateDefault(), cancellationToken: cancellation.Token));
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Split(state, "a,b", ","));

        AssertPreserved(state, before);
    }

    [Fact]
    public void ByteWorkFailureLeavesBothCaptureDestinationsUntouched()
    {
        var state = Seed(State(AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(1)));
        var before = state.MemoryGovernor.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() => Split(state, new string('a', 5000), ","));

        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        AssertPreserved(state, before);
    }

    [Fact]
    public void CaptureBufferEstimateIncludesIndexedValueEntriesAndHeaders()
    {
        Assert.Equal(16, Unsafe.SizeOf<AwkCapturedSeparator>());
        Assert.Equal(32 + 24 + 4 * Unsafe.SizeOf<AwkCapturedSeparator>(), AwkRuntimeMemory.EstimateSeparatorCaptureListBytes(4));
        Assert.Equal(0, AwkRuntimeMemory.EstimateSeparatorCaptureListBytes(0));
        Assert.Same(AwkText.Empty, default(AwkCapturedSeparator).Text);
    }

    private static int Split(AwkRuntimeState state, string input, string separator) =>
        state.SplitIntoArray(AwkText.FromSourceLiteralString(input), Fields,
            AwkFieldSeparatorSpec.FromText(separator), AwkOptionalIdentifier.FromText(Separators.Text));

    private static AwkRuntimeState Seed(AwkRuntimeState state)
    {
        state.SetArrayString(Fields, Old, "field");
        state.SetArrayString(Separators, Old, "separator");
        return state;
    }

    private static void AssertPreserved(AwkRuntimeState state, long committed)
    {
        Assert.True(state.TryGetExistingArrayCount(Fields, out var fields));
        Assert.True(state.TryGetExistingArrayCount(Separators, out var separators));
        Assert.Equal(1, fields);
        Assert.Equal(1, separators);
        Assert.Equal("field", state.GetArrayByteText(Fields, Old).AsString());
        Assert.Equal("separator", state.GetArrayByteText(Separators, Old).AsString());
        Assert.Equal(committed, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkRuntimeLimits limits, AwkRuntimeCounters? counters = null,
        CancellationToken cancellationToken = default) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken,
            new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters ?? AwkRuntimeCounters.Disabled, limits,
                AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter));
}
