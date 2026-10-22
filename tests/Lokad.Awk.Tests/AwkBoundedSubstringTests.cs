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

    [Fact]
    public async Task RefusedResultPreservesAssignmentAndRecord()
    {
        const int limit = 8 << 20;
        var budget = new CooperativeTestBudget(limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            using var state = State(owner, AwkRuntimeCounters.CreateEnabled());
            var record = AwkText.FromSourceLiteralString("é" + new string('q', 100_000));
            state.SetCurrentRecord(record);
            var target = AwkVariableName.FromText("saved");
            state.AssignVariableValue(target, AwkValue.FromText("keep"));
            Assert.True(AwkBuiltins.TryGetCallable("substr", out var builtin));
            var call = AwkExpression.BuiltinCall(builtin,
                [AwkExpression.FieldIndex(AwkExpression.Number(0)), AwkExpression.Number(1), AwkExpression.Number(1)]);
            var assignment = AwkExpression.Assignment(AwkReference.Scalar(target), call);
            using (var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.CurrentCommittedBytes))
            {
                var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await assignment.EvaluateAsync(new(state)));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal("keep", state.ResolveVariableValue(target).AsString());
                Assert.Same(record, state.CurrentRecordByteText);
            }
        }
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters, bool bytes = false)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits)
            { MemoryOwner = owner };
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, counters, CancellationToken.None),
            bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default);
    }
}
