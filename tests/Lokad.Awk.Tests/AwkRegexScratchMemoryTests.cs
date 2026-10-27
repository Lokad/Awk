
namespace Lokad.Awk.Tests;

public sealed class AwkRegexScratchMemoryTests
{
    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("plain", 1, 0)]
    [InlineData("&", 1, 1)]
    [InlineData("\\1", 1, 2)]
    [InlineData("<\\1>", 3, 2)]
    [InlineData("a&b", 3, 1)]
    [InlineData("\\&", 1, 0)]
    [InlineData("\\\\", 1, 0)]
    [InlineData("\\", 1, 0)]
    [InlineData("\\x", 1, 0)]
    [InlineData("\\0\\9", 2, 10)]
    [InlineData("&\\1&", 3, 2)]
    public void GensubAdmitsExactlyTheParsedPartStorage(string replacement, int parts, int groups)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var bytes = AwkRuntimeMemory.EstimateGensubReplacementPartArrayBytes(parts);
        using var memoryForGovernor = Governor(Math.Max(1, bytes), counters);
        var governor = memoryForGovernor.Accounting;
        using (var plan = AwkGensubReplacementPlan.Rent(AwkText.FromString(replacement), governor: governor))
        {
            Assert.Equal(groups, plan.CapturePlan.SelectGroupCount(10));
            Assert.Equal(bytes, governor.CurrentCommittedBytes);
            Assert.Equal(0, governor.CurrentReservedBytes);
            Assert.Equal(0, counters.AwkTextAllocations);
            Assert.Equal(0, counters.Utf8Encodes);
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("literal")]
    [InlineData("ampersands")]
    [InlineData("pairs")]
    public void GensubRejectsInsufficientPartMemoryWithoutAReservation(string shape)
    {
        var (text, parts) = Replacement(shape);
        using var memoryForGovernor = Governor(AwkRuntimeMemory.EstimateGensubReplacementPartArrayBytes(parts) - 1);
        var governor = memoryForGovernor.Accounting;
        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            using var plan = AwkGensubReplacementPlan.Rent(text, governor: governor);
        });
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("literal", false)]
    [InlineData("literal", true)]
    [InlineData("pairs", false)]
    [InlineData("pairs", true)]
    public void GensubCancellationReleasesCountAndConstructionResources(string shape, bool constructing)
    {
        var (text, parts) = Replacement(shape);
        var expected = AwkRuntimeMemory.EstimateGensubReplacementPartArrayBytes(parts);
        using var memoryForGovernor = Governor(expected);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var plan = AwkGensubReplacementPlan.Rent(text, () =>
            {
                if ((governor.CurrentCommittedBytes > 0) == constructing &&
                    ++checks == (constructing ? 4 : 3)) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, governor);
        });
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        using var retry = new CancellationTokenSource();
        using (var plan = AwkGensubReplacementPlan.Rent(text, retry.Token.ThrowIfCancellationRequested, governor))
            Assert.Equal(expected, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedGensubAppendKeepsOnlyThePlanLeaseUntilItIsDisposed(bool canceled)
    {
        var text = AwkText.FromString(new string('a', 16_384));
        using var memoryForGovernor = Governor(1 << 20);
        var governor = memoryForGovernor.Accounting;
        var plan = AwkGensubReplacementPlan.Rent(text, governor: governor);
        using var builder = new AwkUtf8Builder(governor, 0);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        try
        {
            if (!canceled) memoryForGovernor.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            if (canceled)
                Assert.Throws<OperationCanceledException>(() => plan.Append(Match(), builder, () =>
                {
                    if (++checks == 5) cancellation.Cancel();
                    cancellation.Token.ThrowIfCancellationRequested();
                }));
            else
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(
                    Assert.Throws<AwkRuntimeException>(() => plan.Append(Match(), builder)).MessageBytes));
            Assert.Equal(0, builder.Length);
            builder.Dispose();
            Assert.Equal(AwkRuntimeMemory.EstimateGensubReplacementPartArrayBytes(1), governor.CurrentCommittedBytes);
        }
        finally
        {
            plan.Dispose();
        }
        plan.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoMatchOrMissingOccurrenceReleasesGensubScratchWithoutBuildingOutput(bool missingOccurrence)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance));
        var input = AwkText.FromString("x");
        var regex = state.CreateRegex(new AwkRegexPattern(missingOccurrence ? "(x)" : "z"));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var result = AwkRegexSubstitution.ReplaceOccurrence(state, regex, input, AwkText.FromString("<\\1>"),
            AwkReplacementMode.Gensub, missingOccurrence ? 2 : 1, out var count);
        Assert.Same(input, result);
        Assert.Equal(0, count);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(64, false)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    [InlineData(65, true)]
    [InlineData(1538, false)]
    [InlineData(1538, true)]
    public void CaptureMemoryAdmitsTheLiveCoordinateScratchPeak(int groups, bool reject)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var (retained, scratch) = CaptureBytes(groups);
        using var memoryForGovernor = Governor(Math.Max(retained, scratch) - (reject ? 1 : 0), counters);
        var governor = memoryForGovernor.Accounting;
        if (reject)
        {
            var error = Assert.Throws<AwkRuntimeException>(() => { using var result = Capture(groups, governor, counters); });
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(0, counters.RegexGroupsMaterialized);
        }
        else
        {
            using (var result = Capture(groups, governor, counters))
            {
                Assert.Equal(retained, governor.CurrentCommittedBytes);
                Assert.Equal(groups, result.Match.Groups.Count);
                Assert.All(result.Match.Groups, group =>
                {
                    Assert.Equal(0, group.Index);
                    Assert.Equal(4, group.Length);
                    Assert.Equal("\U0001f600", group.Value.AsString());
                });
            }
        }
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("allocation")]
    [InlineData("coordinates")]
    [InlineData("materialization")]
    public void CanceledCaptureReleasesBothResultAndScratchCharges(string phase)
    {
        const int groups = 65;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var (retained, scratch) = CaptureBytes(groups);
        using var memoryForGovernor = Governor(retained + scratch, counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var result = Capture(groups, governor, counters, () =>
            {
                var reached = phase == "materialization" ? counters.AwkTextAllocations > 0
                    : governor.CurrentCommittedBytes > 0 && counters.RegexGroupsMaterialized == 0 && ++checks == (phase == "allocation" ? 1 : 3);
                if (reached) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            });
        });
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        using var retry = new CancellationTokenSource();
        using (var result = Capture(groups, governor, counters, retry.Token.ThrowIfCancellationRequested))
            Assert.Equal(retained, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public void UnboundedCaptureCanRetryAfterCancellation()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var result = Capture(1538, AwkMemoryAccounting.Disabled, counters, () =>
            {
                if (++checks == 5) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            });
        });
        using var retry = new CancellationTokenSource();
        using var result = Capture(1538, AwkMemoryAccounting.Disabled, counters, retry.Token.ThrowIfCancellationRequested);
        Assert.Equal(1538, result.Match.Groups.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CountersDoNotChangeScratchMemoryAdmission(bool observe)
    {
        var (retained, scratch) = CaptureBytes(65);
        var counters = observe ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        using var memoryForGovernor = Governor(Math.Max(retained, scratch) - 1, counters);
        var governor = memoryForGovernor.Accounting;
        var error = Assert.Throws<AwkRuntimeException>(() => { using var result = Capture(65, governor, counters); });
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    private static (AwkText Text, int Parts) Replacement(string shape) => shape switch
    {
        "literal" => (AwkText.FromString(new string('a', 16_384)), 1),
        "ampersands" => (AwkText.FromString(new string('&', 1025)), 1025),
        "pairs" => (AwkText.FromString(string.Concat(Enumerable.Repeat("\\1", 8192))), 8192),
        _ => throw new ArgumentOutOfRangeException(nameof(shape))
    };

    private static (long Retained, long Scratch) CaptureBytes(int groups) =>
        (AwkRuntimeMemory.EstimateRegexMatchBytes(groups,
            AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.EstimateTextBytes(4), groups)),
         groups >= 64 ? AwkRuntimeMemory.EstimateRegexCoordinateArrayBytes((groups + 1) * 2) : 0);

    private static AwkLeasedRegexMatch Capture(int groups, AwkMemoryAccounting governor,
        AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        var whole = new AwkRegexCaptureRange(true, 0, 4, 0, 2, true);
        var ranges = Enumerable.Repeat(whole, groups).ToArray();
        return AwkRegexMatch.FromRanges(whole, "\U0001f600"u8, ranges, AwkRegexSearchOffset.Start,
            AwkRegexCoordinatePolicy.Byte, counters, governor, checkpoint ?? (() => { }));
    }

    private static AwkRegexMatch Match() => new(true, 0, 1, 0, 1, AwkText.FromString("H"),
        [new(true, 0, 1, AwkText.FromString("H")), new(true, 0, 1, AwkText.FromString("G"))]);

    private static CooperativeTestMemory Governor(long budget, AwkRuntimeCounters? counters = null) =>
        new CooperativeTestMemory(budget, counters ?? AwkRuntimeCounters.Disabled);
}
