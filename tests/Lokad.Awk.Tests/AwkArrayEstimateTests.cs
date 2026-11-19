namespace Lokad.Awk.Tests;

public sealed class AwkArrayEstimateTests
{
    [Fact]
    public void RefusedIdentityAdmissionDoesNotPublishAnArray()
    {
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("values");
        var retained = memory.Budget.Reserved;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        AssertMemoryLimit(() => variables.ClearArray(name));
        Assert.False(variables.TryGetArrayCount(name, out _));
        Assert.Equal(retained, memory.Budget.Reserved);
        Assert.Equal(1, memory.Budget.Refusals);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData("x", false)]
    [InlineData(" x ", true)]
    public void SplitPairIdentitiesStayFundedUntilTheirFunctionScopeRetires(string input, bool capturesSeparators)
    {
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var state = memory.State;
        var variables = state.Variables;
        var first = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("fields"), 1);
        var retained = memory.Budget.Reserved;
        var second = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("separators"), 1);
        var count = state.Arrays.SplitIntoArray(AwkText.FromSourceLiteralString(input), new(first),
            AwkFieldSeparatorSpec.Default, new AwkArrayTarget(second));
        Assert.Equal(1, count);
        Assert.Equal("x", variables.GetArrayValue(first, AwkArrayKey.FromOrdinal(1)).AsString());
        if (capturesSeparators)
            Assert.Equal(" ", variables.GetArrayValue(second, AwkArrayKey.FromOrdinal(0)).AsString());
        var filled = memory.Budget.Reserved;
        variables.ClearArray(first);
        variables.ClearArray(second);
        Assert.InRange(memory.Budget.Reserved, 1, filled - 1);
        var empty = memory.Budget.Reserved;
        variables.RemoveFunctionLocalArray(first);
        Assert.InRange(memory.Budget.Reserved, 1, empty - 1);
        variables.RemoveFunctionLocalArray(second);
        Assert.Equal(retained, memory.Budget.Reserved);
    }

    [Fact]
    public void AggregateEstimatesRealArrayPayloadBeyondInt32()
    {
        // A valid shared rope gives large estimates without allocating its logical bytes.
        using var state = AwkTestHarness.CreateRuntimeStateWithCounters(AwkRuntimeCounters.Disabled);
        var text = AwkText.FromString("x");
        for (var index = 0; index < 30; index++)
            text = state.ConcatText([text, text]);
        var slot = AwkValue.ForStoredText(text);
        var slots = new AwkArrayIdentity(3, AwkArrayKeyComparer.Instance);
        var expected = slots.BackingBytes;
        for (var index = 0; index < 3; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index);
            slots.Add(key, slot);
            expected = checked(expected + AwkRuntimeMemory.EstimateArrayElementPayloadBytes(key, slot));
        }
        Assert.True(expected > int.MaxValue);
        Assert.Equal(expected, AwkVariables.EstimateArrayBytes(slots));
    }

    [Fact]
    public void EmptyAndMixedArrayTotalsIncludeRetainedKeysAndValues()
    {
        var slots = new AwkArrayIdentity(0, AwkArrayKeyComparer.Instance);
        Assert.Equal(0, AwkVariables.EstimateArrayBytes(slots));
        var ordinal = AwkArrayKey.FromOrdinal(1);
        var key = new AwkArrayKey("café-profond");
        var text = AwkText.FromString("été");
        slots.Add(ordinal, AwkValue.FromNumber(42));
        slots.Add(key, AwkValue.ForStoredText(text));
        var filled = AwkVariables.EstimateArrayBytes(slots);
        Assert.True(slots.Remove(key));
        Assert.Equal(AwkRuntimeMemory.EstimateArrayElementPayloadBytes(key, AwkValue.ForStoredText(text)),
            filled - AwkVariables.EstimateArrayBytes(slots));
        Assert.True(slots.Remove(ordinal));
        Assert.True(AwkVariables.EstimateArrayBytes(slots) > 0);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementPreservesAdmissionAtomicityAndRefunds(bool refuse)
    {
        using var memory = new CooperativeTestMemory(8192)
            .InitializeRuntime(AwkRuntimeCounters.CreateEnabled());
        memory.Budget.SetAvailableBytes(8192);
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(1);
        variables.ClearArray(name);
        var emptyArrayBytes = accounting.LiveBytes;
        variables.SetArrayValue(name, key, AwkValue.FromNumber(42));
        var committed = accounting.LiveBytes;
        var reserved = budget.Reserved;
        var replacement = variables.CreateMutableArraySlots(0);
        replacement[key] = AwkValue.ForStoredText(new string('x', refuse ? 4096 : 16));

        void Replace() => variables.ReplaceMutableArraySlots(new(name), replacement, AwkMemoryLease.Disabled, 0);
        if (refuse)
        {
            AssertMemoryLimit(Replace);
            Assert.Equal(42, variables.GetArrayValue(name, key).AsNumber());
            Assert.Equal(committed, accounting.LiveBytes);
            Assert.Equal(reserved, budget.Reserved);
        }
        else
        {
            Replace();
            Assert.Equal(new string('x', 16), variables.GetArrayValue(name, key).AsString());
            Assert.Equal(emptyArrayBytes + AwkVariables.EstimateArrayBytes(variables.ResolveOrdinaryArray(name)), accounting.LiveBytes);
        }
        variables.ClearArray(name);
        Assert.Equal(emptyArrayBytes, accounting.LiveBytes);
        Assert.Equal(emptyArrayBytes, budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void DeletingEntriesKeepsBackingCapacityFundedUntilTheArrayIsCleared()
    {
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("values");
        variables.ClearArray(name);
        var emptyArrayBytes = memory.Budget.Reserved;
        for (var index = 1; index <= 4096; index++)
            variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromText(AwkText.FromString("retained payload")));
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
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var variables = memory.State.Variables;
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

}
