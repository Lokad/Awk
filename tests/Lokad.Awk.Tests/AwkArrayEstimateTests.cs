using System.Collections;
using System.Numerics;

namespace Lokad.Awk.Tests;

public sealed class AwkArrayEstimateTests
{
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
        Assert.Equal(0, AwkVariables.EstimateArrayBytes(new Dictionary<AwkArrayKey, AwkVariableSlot>()));
        var key = new AwkArrayKey("café");
        var text = AwkText.FromString("été");
        Dictionary<AwkArrayKey, AwkVariableSlot> slots = new()
        {
            [AwkArrayKey.FromOrdinal(1)] = AwkVariableSlot.FromValue(AwkValue.FromNumber(42)),
            [key] = AwkVariableSlot.FromText(text)
        };
        var filled = AwkVariables.EstimateArrayBytes(slots);
        slots.Remove(key);
        Assert.Equal(key.RetainedTextMemoryBytes + AwkRuntimeMemory.EstimateVariableSlotBytes(AwkVariableSlot.FromText(text)),
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
        variables.SetArrayValue(name, key, AwkValue.FromNumber(42));
        var committed = accounting.CurrentCommittedBytes;
        var reserved = budget.Reserved;
        Dictionary<AwkArrayKey, AwkVariableSlot> replacement = new()
        {
            [key] = AwkVariableSlot.FromText(new string('x', refuse ? 4096 : 16))
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
            Assert.Equal(AwkVariables.EstimateArrayBytes(replacement), accounting.CurrentCommittedBytes);
        }
        Assert.Equal(0, accounting.CurrentReservedBytes);
        variables.ClearArray(name);
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void DeletingEntriesKeepsBackingCapacityFundedUntilTheArrayIsCleared()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var name = AwkArrayName.FromText("values");
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
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void RefusedBackingGrowthPreservesAdmittedEntriesAndRefundsOnClear()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });
        var name = AwkArrayName.FromText("values");
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
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static void AssertMemoryLimit(Action action)
    {
        var error = Assert.Throws<AwkRuntimeException>(action);
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }

    // The estimator needs enumeration only. Unique ordinal keys avoid a multi-GB dictionary.
    private sealed class RepeatedSlots(int count, AwkVariableSlot slot)
        : IReadOnlyDictionary<AwkArrayKey, AwkVariableSlot>
    {
        public int Count => count;
        public IEnumerable<AwkArrayKey> Keys => Enumerable.Range(0, count).Select(AwkArrayKey.FromOrdinal);
        public IEnumerable<AwkVariableSlot> Values => Enumerable.Repeat(slot, count);
        public AwkVariableSlot this[AwkArrayKey key] => throw new NotSupportedException();
        public bool ContainsKey(AwkArrayKey key) => throw new NotSupportedException();
        public bool TryGetValue(AwkArrayKey key, out AwkVariableSlot value) => throw new NotSupportedException();
        public IEnumerator<KeyValuePair<AwkArrayKey, AwkVariableSlot>> GetEnumerator()
        {
            for (var index = 0; index < count; index++)
                yield return new(AwkArrayKey.FromOrdinal(index), slot);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
