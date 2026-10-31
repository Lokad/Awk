using System.Collections;
using System.Numerics;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayEstimateTests
{
    [Fact]
    public void RefusedIdentityAdmissionDoesNotPublishAnArray()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var name = AwkArrayName.FromText("values");
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        AssertMemoryLimit(() => variables.ClearArray(name));
        Assert.False(variables.TryGetArrayCount(name, out _));
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(1, memory.Budget.Refusals);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Fact]
    public void SplitPairIdentitiesStayFundedUntilTheirFunctionScopeRetires()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var first = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("fields"), 1);
        var second = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("separators"), 1);
        variables.SetSplitArrayValues(first, [AwkText.FromSourceLiteralString("x")], second, []);
        Assert.Equal("x", variables.GetArrayValue(first, AwkArrayKey.FromOrdinal(1)).AsString());
        var filled = memory.Budget.Reserved;
        variables.ClearArray(first);
        variables.ClearArray(second);
        Assert.InRange(memory.Budget.Reserved, 1, filled - 1);
        var empty = memory.Budget.Reserved;
        variables.RemoveFunctionLocalArray(first);
        Assert.InRange(memory.Budget.Reserved, 1, empty - 1);
        variables.RemoveFunctionLocalArray(second);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AggregateRejectsOverflowAndAcceptsTheLastRepresentableTotal(bool overflow)
    {
        // A valid shared rope gives large estimates without allocating its logical bytes.
        var text = AwkText.FromString("x");
        for (var index = 0; index < 30; index++)
            text = text.Concat(text, AwkRuntimeCounters.Disabled, AwkMemoryAccounting.Disabled);
        var slot = AwkVariableSlot.FromText(text);
        var elementBytes = AwkRuntimeMemory.EstimateArrayElementBytes(AwkArrayKey.FromOrdinal(0), slot);
        var count = checked((int)(long.MaxValue / elementBytes + (overflow ? 1 : 0)));
        var mathematicalTotal = (BigInteger)elementBytes * count;
        var slots = new RepeatedSlots(count, slot);

        if (overflow)
        {
            Assert.True(mathematicalTotal > long.MaxValue);
            AssertMemoryLimit(() => AwkVariables.EstimateArrayBytes(slots));
        }
        else
        {
            Assert.True(mathematicalTotal <= long.MaxValue);
            Assert.Equal((long)mathematicalTotal, AwkVariables.EstimateArrayBytes(slots));
        }
    }

    [Fact]
    public void EmptyAndMixedArrayTotalsIncludeRetainedKeysAndValues()
    {
        Assert.Equal(0, AwkVariables.EstimateArrayBytes(new Dictionary<AwkArrayKey, AwkArrayCell>()));
        var key = new AwkArrayKey("café");
        var text = AwkText.FromString("été");
        Dictionary<AwkArrayKey, AwkArrayCell> slots = new()
        {
            [AwkArrayKey.FromOrdinal(1)] = new(AwkVariableSlot.FromValue(AwkValue.FromNumber(42))),
            [key] = new(AwkVariableSlot.FromText(text))
        };
        var filled = AwkVariables.EstimateArrayBytes(slots);
        slots.Remove(key);
        Assert.Equal(AwkRuntimeMemory.EstimateArrayElementPayloadBytes(key, AwkVariableSlot.FromText(text)),
            filled - AwkVariables.EstimateArrayBytes(slots));
        slots.Clear();
        Assert.True(AwkVariables.EstimateArrayBytes(slots) > 0);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacementPreservesAdmissionAtomicityAndRefunds(bool refuse)
    {
        var budget = new CooperativeTestBudget(8192);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        var variables = new AwkVariables(accounting, counters, static text => AwkValue.FromText(text), static text => AwkValue.FromNumericString(text), static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), static (_, _) => { });
        var name = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(1);
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.CurrentCommittedBytes;
        variables.SetArrayValue(name, key, AwkValue.FromNumber(42));
        var committed = accounting.CurrentCommittedBytes;
        var reserved = budget.Reserved;
        Dictionary<AwkArrayKey, AwkArrayCell> replacement = new()
        {
            [key] = new(AwkVariableSlot.FromText(new string('x', refuse ? 4096 : 16)))
        };

        void Replace() => variables.ReplaceMutableArraySlots(name, replacement, AwkMemoryLease.Disabled, 0);
        if (refuse)
        {
            AssertMemoryLimit(Replace);
            Assert.Equal(42, variables.GetArrayValue(name, key).AsNumber());
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(reserved, budget.Reserved);
        }
        else
        {
            Replace();
            Assert.Equal(new string('x', 16), variables.GetArrayValue(name, key).AsString());
            Assert.Equal(emptyArrayBytes + AwkVariables.EstimateArrayBytes(replacement), accounting.CurrentCommittedBytes);
        }
        Assert.Equal(0, accounting.CurrentReservedBytes);
        variables.ClearArray(name);
        Assert.Equal(emptyArrayBytes, accounting.CurrentCommittedBytes);
        Assert.Equal(emptyArrayBytes, budget.Reserved);
    }

    [Fact]
    public void DeletingEntriesKeepsBackingCapacityFundedUntilTheArrayIsCleared()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var name = AwkArrayName.FromText("values");
        variables.ClearArray(name);
        var emptyArrayBytes = memory.Budget.Reserved;
        for (var index = 1; index <= 4096; index++)
            variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
        var filled = memory.Budget.Reserved;
        for (var index = 1; index <= 4096; index++) variables.DeleteArrayValue(name, AwkArrayKey.FromOrdinal(index));
        Assert.True(variables.TryGetArrayCount(name, out var count));
        Assert.Equal(0, count);
        Assert.True(memory.Budget.Reserved > 0);
        Assert.True(memory.Budget.Reserved < filled);
        variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        Assert.Equal(7, variables.GetArrayValue(name, AwkArrayKey.FromOrdinal(1)).AsNumber());
        variables.ClearArray(name);
        Assert.Equal(emptyArrayBytes, memory.Budget.Reserved);
    }

    [Fact]
    public void RefusedBackingGrowthPreservesAdmittedEntriesAndRefundsOnClear()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var name = AwkArrayName.FromText("values");
        variables.ClearArray(name);
        var ownerBytes = memory.Budget.Reserved;
        variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 192;
        var refused = false;
        for (var index = 2; index <= 16; index++)
        {
            var admitted = memory.Budget.Reserved;
            try { variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index)); }
            catch (AwkRuntimeException exception)
            {
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
                Assert.Equal(admitted, memory.Budget.Reserved);
                Assert.True(variables.TryGetArrayCount(name, out var count));
                Assert.Equal(index - 1, count);
                Assert.False(variables.ContainsArrayValue(name, AwkArrayKey.FromOrdinal(index)));
                refused = true;
                break;
            }
        }
        Assert.True(refused);
        Assert.Equal(7, variables.GetArrayValue(name, AwkArrayKey.FromOrdinal(1)).AsNumber());
        variables.ClearArray(name);
        Assert.Equal(ownerBytes, memory.Budget.Reserved);
    }

    private static void AssertMemoryLimit(Action action)
    {
        var error = Assert.Throws<AwkRuntimeException>(action);
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    // The estimator needs enumeration only. Unique ordinal keys avoid a multi-GB dictionary.
    private sealed class RepeatedSlots(int count, AwkVariableSlot slot)
        : IReadOnlyDictionary<AwkArrayKey, AwkArrayCell>
    {
        public int Count => count;
        public IEnumerable<AwkArrayKey> Keys => Enumerable.Range(0, count).Select(AwkArrayKey.FromOrdinal);
        private readonly AwkArrayCell _cell = new(slot);
        public IEnumerable<AwkArrayCell> Values => Enumerable.Repeat(_cell, count);
        public AwkArrayCell this[AwkArrayKey key] => throw new NotSupportedException();
        public bool ContainsKey(AwkArrayKey key) => throw new NotSupportedException();
        public bool TryGetValue(AwkArrayKey key, out AwkArrayCell value) => throw new NotSupportedException();
        public IEnumerator<KeyValuePair<AwkArrayKey, AwkArrayCell>> GetEnumerator()
        {
            for (var index = 0; index < count; index++)
                yield return new(AwkArrayKey.FromOrdinal(index), _cell);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
