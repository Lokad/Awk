namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkBoundedSubstringTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void PartialResultsStayFundedByTheirActualDestination(bool bytes, bool unicode, bool borrowed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using (var state = State(memory.Owner, counters, bytes))
        {
            var expected = (unicode ? "😀" : "") + new string('a', 8193);
            var source = AwkText.FromString("x" + expected + "z");
            // This fixture owns its already-prepared source and its scalar index.
            _ = source.CopyRuneSlice(65, 1, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled);
            var initialBytes = memory.Budget.Reserved;
            using (var destination = state.MemoryAccounting.CreateTransientLease())
            {
                var operand = borrowed ? new AwkScalarOperand(source.AsSpan(), -1) :
                    new AwkScalarOperand(AwkValue.FromText(source));
                var length = bytes ? System.Text.Encoding.UTF8.GetByteCount(expected) : 8193 + (unicode ? 1 : 0);
                var result = AwkBuiltins.EvaluateSubstrText(state, operand,
                    AwkValue.FromNumber(2), AwkValue.FromNumber(length), 3, destination);
                Assert.Equal(expected, result.AsString());
                Assert.Equal(AwkRuntimeValueKind.String, state.CreateStringValue(result).RuntimeKind);
                Assert.True(destination.Bytes >= result.EstimatedRetainedMemoryBytes);
                Assert.True(memory.Budget.Reserved >= destination.Bytes);
                Assert.Equal(0, counters.StringDecodes);
                Assert.Equal(0, counters.Utf8Encodes);
            }
            Assert.Equal(initialBytes, memory.Budget.Reserved);
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void AdmissionFailurePrecedesSliceAllocationAndTargetMutation(bool borrowed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using (var state = State(memory.Owner, counters, token: cancellation.Token))
        {
            var target = AwkVariableName.FromText("saved");
            state.AssignVariableValue(target, AwkValue.FromText("keep"));
            var source = AwkText.FromString(new string('a', 1_000_000));
            _ = source.GetRuneLength();
            var initialBytes = memory.Budget.Reserved;
            using var destination = state.MemoryAccounting.CreateTransientLease();
            var allocatedAtAdmission = 0L;
            memory.Budget.OnGrowth = _ =>
            {
                allocatedAtAdmission = GC.GetAllocatedBytesForCurrentThread();
                if (cancel) cancellation.Cancel();
            };
            if (!cancel) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            void Evaluate()
            {
                var operand = borrowed ? new AwkScalarOperand(source.AsSpan(), -1) :
                    new AwkScalarOperand(AwkValue.FromText(source));
                var result = AwkBuiltins.EvaluateSubstrText(state, operand,
                    AwkValue.FromNumber(2), AwkValue.FromNumber(500_000), 3, destination);
                state.AssignVariableValue(target, state.CreateStringValue(result));
            }
            if (cancel) Assert.Throws<OperationCanceledException>(Evaluate);
            else
            {
                var failure = Assert.Throws<AwkRuntimeException>(Evaluate);
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.Equal(1, memory.Budget.Refusals);
                Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
            }
            Assert.Equal("keep", state.ResolveVariableValue(target).AsString());
            Assert.Equal(0, counters.AwkTextAllocations);
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - allocatedAtAdmission, 0, 64 * 1024);
            Assert.Equal(0, destination.Bytes);
            Assert.Equal(initialBytes, memory.Budget.Reserved);
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void WholeAndEmptyResultsKeepTheirExistingStorage(bool bytes, bool empty)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        using var state = State(memory.Owner, counters, bytes);
        var source = AwkText.FromString("008");
        using var destination = state.MemoryAccounting.CreateTransientLease();
        var result = AwkBuiltins.EvaluateSubstrText(state, new(AwkValue.FromText(source)),
            AwkValue.FromNumber(1), AwkValue.FromNumber(empty ? 0 : 3), 3, destination);
        Assert.Equal(empty ? "" : "008", result.AsString());
        Assert.Equal(AwkRuntimeValueKind.String, state.CreateStringValue(result).RuntimeKind);
        if (!empty) Assert.True(source.HasSameStorage(result));
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, destination.Bytes);
    }

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

    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner, AwkRuntimeCounters counters, bool bytes = false,
        CancellationToken token = default)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance)
            { MemoryOwner = owner };
        return new(AwkFieldSeparatorSpec.Default, token, options,
            new AwkRuntimeResourceMonitor(limits, counters, token),
            bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default);
    }
}
