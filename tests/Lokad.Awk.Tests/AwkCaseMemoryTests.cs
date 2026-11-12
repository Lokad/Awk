namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCaseMemoryTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task UnchangedBorrowedCaseResultStaysFundedUntilConsumption(bool upper, bool bytes)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(owner, bytes);
            var initialBytes = budget.Reserved;
            var source = AwkText.FromString(new string(upper ? 'A' : 'a', 8193));
            using (var destination = state.MemoryAccounting.CreateTransientLease())
            {
                var result = AwkBuiltins.EvaluateCaseText(state, new(source.AsSpan()), destination, upper);
                Assert.Equal(source.AsString(), result.AsString());
                Assert.True(destination.Bytes >= result.EstimatedRetainedMemoryBytes);
                Assert.True(budget.Reserved >= initialBytes + destination.Bytes);
                Assert.Equal(8193, state.CountStringUnits(result));
                Assert.Equal(0, state.Counters.StringInteropFallbacks);
            }
            Assert.Equal(initialBytes, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BorrowedUnicodeSourceRemainsFundedThroughCompatibilityConversion(bool upper)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(owner, false);
            var initialBytes = budget.Reserved;
            var source = AwkText.FromString("😀_" + new string(upper ? 'a' : 'A', 8193));
            using (var destination = state.MemoryAccounting.CreateTransientLease())
            {
                var sawFundedDecodedSource = false;
                budget.OnGrowth = _ =>
                {
                    if (state.Counters.StringDecodes == 0) return;
                    // A decoded source exists before conversion/output allocation. Its copied
                    // UTF-8 owner must still be funded, without relying on future result funding.
                    Assert.True(destination.Bytes >= source.EstimatedRetainedMemoryBytes);
                    sawFundedDecodedSource = true;
                };
                var result = AwkBuiltins.EvaluateCaseText(state, new(source.AsSpan()), destination, upper);
                budget.OnGrowth = null;
                Assert.True(sawFundedDecodedSource);
                Assert.Equal("😀_" + new string(upper ? 'A' : 'a', 8193), result.AsString());
                Assert.Equal(result.EstimatedRetainedMemoryBytes, destination.Bytes);
                Assert.Equal(8195, state.CountStringUnits(result));
            }
            Assert.Equal(initialBytes, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CaseDestinationFundsConstructionThroughItsActualUser(bool upper, bool bytes, bool unicode)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(owner, bytes);
            var initialBytes = budget.Reserved;
            var prefix = unicode ? "😀_" : "";
            var source = AwkText.FromString(prefix + new string(upper ? 'a' : 'A', 8193));
            using (var destination = state.MemoryAccounting.CreateTransientLease())
            {
                var result = AwkBuiltins.EvaluateCaseText(state, new(source), destination, upper);
                Assert.Equal(prefix + new string(upper ? 'A' : 'a', 8193), result.AsString());
                Assert.True(destination.Bytes >= result.EstimatedRetainedMemoryBytes);
                Assert.True(budget.Reserved >= destination.Bytes);
                Assert.Equal(AwkRuntimeValueKind.String, state.CreateStringValue(result).RuntimeKind);
                Assert.Equal(unicode && !bytes ? 1 : 0, state.Counters.StringInteropFallbacks);
                // The result remains usable while its actual destination owns admission.
                Assert.Equal(8193 + (unicode ? 2 : 0), result.GetRuneLength());
            }
            Assert.Equal(initialBytes, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CancellationDuringCaseAdmissionPrecedesAllocationAndRefundsUnusedStorage(bool upper, bool unicode)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(16 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, cancellation.Token))
        {
            var state = State(owner, false, cancellation.Token);
            var initialBytes = budget.Reserved;
            var source = AwkText.FromString((unicode ? "😀_" : "") + new string(upper ? 'a' : 'A', 1_000_000));
            _ = source.AsString();
            using var destination = state.MemoryAccounting.CreateTransientLease();
            var allocatedAtAdmission = 0L;
            budget.OnGrowth = _ =>
            {
                allocatedAtAdmission = GC.GetAllocatedBytesForCurrentThread();
                cancellation.Cancel();
            };
            Assert.Throws<OperationCanceledException>(() =>
                AwkBuiltins.EvaluateCaseText(state, new(source), destination, upper));
            Assert.Equal(0, state.Counters.AwkTextAllocations);
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - allocatedAtAdmission, 0, 64 * 1024);
            Assert.Equal(0, destination.Bytes);
            Assert.Equal(initialBytes, budget.Reserved);
        }
        Assert.Equal(0, budget.Reserved);
    }

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
            Assert.True(result.ByteText.HasSameStorage(AwkBuiltins.EvaluateCaseValue(state, result, upper).ByteText));
        }
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, bool bytes,
        CancellationToken cancellationToken = default)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, regexEngine: AwkUtf8RegexEngine.Instance) { MemoryOwner = owner };
        var profile = bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default;
        return new(AwkFieldSeparatorSpec.Default, cancellationToken, options,
            new AwkRuntimeResourceMonitor(limits, options.Counters), profile);
    }
}
