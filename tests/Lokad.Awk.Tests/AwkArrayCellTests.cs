using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayCellTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void CapturedTargetRetiresItsPayloadBeforeArrayRefund(bool clear, bool indexed)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var array = variables.ResolveOrdinaryArray(AwkArrayName.FromText("a"));
        var key = indexed ? AwkArrayKey.FromOrdinal(1) : new AwkArrayKey("cell");
        variables.SetArrayValue(array, key, AwkValue.FromNumber(5));
        var target = variables.PrepareArrayUpdate(array, key);
        if (indexed) Assert.Null(target.Cell);
        else Assert.IsType<AwkArrayCell>(target.Cell);
        using var heldTarget = memory.Accounting.ReserveTransient(target.RetainedTargetBytes);
        var text = ReplaceAndRetire(variables, array, key, clear);
        Assert.Equal(5, target.Previous);
        if (target.Cell is { } cell)
        {
            Assert.False(cell.IsAttached);
            Assert.Equal(0, cell.Slot.RetainedTextMemoryBytes);
        }
        Assert.Equal(key, target.Key);
        Assert.False(array.ContainsKey(key));
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(text.TryGetTarget(out _));
        Assert.InRange(memory.Budget.Reserved, 1, 100_000);
        GC.KeepAlive(target);
    }

    [Fact]
    public void DuplicateInitialKeysKeepTheFinalValueAndSingleOwnedEntry()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var name = AwkArrayName.FromText("a");
        variables.InitializeArrayValues(name,
            [new(AwkArrayKey.FromOrdinal(7), AwkValue.FromNumber(1)),
             new(new AwkArrayKey("7"), AwkValue.FromNumber(2))]);
        var key = Assert.Single(variables.ResolveOrdinaryArray(name)).Key;
        Assert.Equal(new AwkArrayKey("7"), key);
        Assert.Equal(0, key.RetainedTextMemoryBytes);
        Assert.Equal(2, variables.GetArrayValue(name, AwkArrayKey.FromOrdinal(7)).AsNumber());
        variables.ClearArray(name);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DeletionRefundsStoredKeyOwnershipAcrossNumericAndTextLookups(bool storedOrdinal)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var array = variables.ResolveOrdinaryArray(AwkArrayName.FromText("a"));
        // Keep dictionary capacity stable while comparing the key's actual claim.
        variables.SetArrayValue(array, new("kept"), AwkValue.FromNumber(5));
        var before = memory.Budget.Reserved;
        var ordinal = AwkArrayKey.FromOrdinal(int.MaxValue);
        var spelling = new AwkArrayKey("2147483647");
        variables.SetArrayValue(array, storedOrdinal ? ordinal : spelling, AwkValue.FromNumber(7));
        Assert.Equal(2, array.Count);
        Assert.True(array.TryGetDictionaryCell(ordinal, out var cell));
        Assert.NotNull(cell);

        variables.DeleteArrayValue(array, storedOrdinal ? spelling : ordinal);

        Assert.Single(array);
        Assert.False(array.ContainsKey(ordinal));
        Assert.False(cell.IsAttached);
        Assert.Equal(5, variables.GetArrayNumber(array, new AwkArrayKey("kept")));
        Assert.Equal(before, memory.Budget.Reserved);
        variables.ClearArray(array);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReplacedOrDetachedOwnersDoNotRetainTheirPreviousTables(bool detach)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        var array = variables.ResolveOrdinaryArray(name);
        var table = RetireTable(variables, name, array, detach);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(table.TryGetTarget(out _));
        Assert.Equal(detach, array.IsDetached);
        Assert.Empty(array);
        Assert.False(array.ByteLookup.ContainsKey("1"u8));
        GC.KeepAlive(array);
    }

    [Fact]
    public void RefusedCellInsertionPreservesExistingArray()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var array = variables.ResolveOrdinaryArray(AwkArrayName.FromText("a"));
        variables.SetArrayValue(array, new("first"), AwkValue.FromNumber(5));
        var admitted = memory.Budget.Reserved;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() =>
            variables.SetArrayValue(array, new("second"), AwkValue.FromNumber(9)));
        Assert.Equal(admitted, memory.Budget.Reserved);
        Assert.Single(array);
        Assert.Equal(5, variables.GetArrayNumber(array, new AwkArrayKey("first")));
        Assert.True(array.ByteLookup.TryGetValue("first"u8, out var stored));
        Assert.Equal(5, variables.ReadArraySlotNumber(stored));
        Assert.False(array.ByteLookup.ContainsKey("second"u8));
    }

    [Fact]
    public void BorrowedLookupTracksGrowthAndReplacementWithoutInsertingMissingKeys()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var name = AwkArrayName.FromText("a");
        var array = variables.ResolveOrdinaryArray(name);
        variables.SetArrayValue(array, new AwkArrayKey("0007"), AwkValue.FromNumber(5));
        Assert.True(array.TryGetDictionaryCell(new("0007"), out var original));
        Assert.NotNull(original);
        for (var index = 0; index < 1024; index++)
            variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));

        Assert.True(array.ByteLookup.TryGetValue("0007"u8, out var retainedKey, out var grown));
        Assert.True(array.TryGetDictionaryCell(retainedKey, out var grownCell));
        Assert.Same(original, grownCell);
        Assert.Equal("0007", retainedKey.ToString());
        Assert.Equal(5, variables.ReadArraySlotNumber(grown));
        var count = array.Count;
        Assert.False(array.ByteLookup.ContainsKey("missing"u8));
        Assert.Equal(count, array.Count);

        variables.InitializeArrayValues(name, [new(new AwkArrayKey("replacement"), AwkValue.FromNumber(9))]);

        Assert.Same(array, variables.ResolveOrdinaryArray(name));
        Assert.False(original.IsAttached);
        Assert.False(array.ByteLookup.ContainsKey("0007"u8));
        Assert.True(array.ByteLookup.TryGetValue("replacement"u8, out var replaced));
        Assert.Equal(9, variables.ReadArraySlotNumber(replaced));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericUpdateKeepsItsCapturedValueAfterTheCellChanges(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: false);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InputNumericUpdateKeepsItsValueWhenConversionRefundReplacesTheCell(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: true);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericReadKeepsItsCapturedValueAfterTheCellChanges(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: false, update: false);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InputNumericReadKeepsItsValueWhenConversionRefundReplacesTheCell(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: true, update: false);

    private static void AssertConversionSnapshot(bool borrowedKey, bool input, bool update = true)
    {
        using var memory = new CooperativeTestMemory(8 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var variables = memory.State.Variables;
        var name = AwkArrayName.FromText("a");
        var array = variables.ResolveOrdinaryArray(name);
        var key = new AwkArrayKey("kept");
        var text = memory.State.ConcatText(
            [AwkText.FromString(new string('0', 2 * AwkRuntimeLimits.CancellationCheckBytes)), AwkText.FromString("7")]);
        variables.SetArrayValue(array, key, input ? AwkValue.ForStoredInputText(text) : AwkValue.FromText(text));
        Assert.True(array.ByteLookup.TryGetValue("kept"u8, out var stored));
        // The test retains its source while the callback replaces the array's claim.
        using var sourceMemory = memory.Accounting.ReserveTransient(stored.ByteText.EstimatedRetainedMemoryBytes);
        var replaced = false;
        if (input) memory.Budget.OnRefund = ReplaceCell;

        void ReplaceCell()
        {
            memory.Budget.OnRefund = null;
            replaced = true;
            variables.SetArrayValue(array, key, AwkValue.FromNumber(99));
        }

        var source = new AwkCopySource("kept"u8);
        if (update)
        {
            var target = borrowedKey
                ? variables.PrepareArrayUpdate(array, in source)
                : variables.PrepareArrayUpdate(array, key);
            if (!input) ReplaceCell();
            Assert.True(replaced);
            Assert.Equal(7, target.Previous);
            Assert.Equal(key, target.Key);
            Assert.Equal(99, variables.GetArrayNumber(array, key));
            variables.SetCapturedArrayNumber(array, in target, target.Previous + 3);
            Assert.Equal(10, variables.GetArrayNumber(array, key));
        }
        else
        {
            var number = borrowedKey ? variables.GetArrayNumber(array, in source) : variables.GetArrayNumber(array, key);
            if (!input) ReplaceCell();
            Assert.True(replaced);
            Assert.Equal(7, number);
            Assert.Equal(99, variables.GetArrayNumber(array, key));
        }
        variables.ClearArray(array);
        sourceMemory.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkArrayCell> RetireTable(AwkVariables variables,
        AwkArrayName name, AwkArrayIdentity array, bool detach)
    {
        variables.SetArrayValue(array, new("cell"), AwkValue.FromNumber(7));
        Assert.True(array.TryGetDictionaryCell(new("cell"), out var cell));
        var reference = new WeakReference<AwkArrayCell>(Assert.IsType<AwkArrayCell>(cell));
        if (detach) variables.RemoveFunctionLocalArray(name);
        else variables.ClearArray(name);
        return reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> ReplaceAndRetire(AwkVariables variables, AwkArrayIdentity array,
        AwkArrayKey key, bool clear)
    {
        var text = AwkText.FromString(new string('x', 1_000_000));
        variables.SetArrayValue(array, key, AwkValue.FromText(text));
        if (clear) variables.ClearArray(array);
        else variables.DeleteArrayValue(array, key);
        return new(text.Backing ?? throw new InvalidOperationException("Expected owned nonempty text."));
    }

    private static AwkVariables Variables(CooperativeTestMemory memory) =>
        memory.InitializeRuntime(AwkRuntimeCounters.Disabled).State.Variables;
}
