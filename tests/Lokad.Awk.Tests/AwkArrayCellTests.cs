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
        Assert.True(Assert.Single(variables.ResolveOrdinaryArray(name)).Key.IsOrdinal);
        Assert.Equal(2, variables.GetArrayValue(name, AwkArrayKey.FromOrdinal(7)).AsNumber());
        variables.ClearArray(name);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
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
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() =>
            variables.SetArrayValue(array, new("second"), AwkValue.FromNumber(9)));
        Assert.Single(array);
        Assert.Equal(5, variables.GetArrayNumber(array, new AwkArrayKey("first")));
        Assert.True(array.ByteLookup.TryGetValue("first"u8, out var stored));
        Assert.Equal(5, variables.ReadArraySlotNumber(stored));
        Assert.False(array.ByteLookup.ContainsKey("second"u8));
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Fact]
    public void BorrowedLookupTracksGrowthAndReplacementWithoutInsertingMissingKeys()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var name = AwkArrayName.FromText("a");
        var array = variables.ResolveOrdinaryArray(name);
        variables.SetArrayValue(array, new AwkArrayKey("0007"), AwkValue.FromNumber(5));
        Assert.True(array.TryGetEntry(new("0007"), out _, out _, out var original));
        Assert.NotNull(original);
        for (var index = 0; index < 1024; index++)
            variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));

        Assert.True(array.ByteLookup.TryGetValue("0007"u8, out var retainedKey, out var grown));
        Assert.True(array.TryGetEntry(retainedKey, out _, out _, out var grownCell));
        Assert.Same(original, grownCell);
        Assert.Equal("0007", retainedKey.AsString(AwkRuntimeCounters.Disabled));
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
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericUpdateKeepsItsValueWhenConversionPollReplacesTheCell(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: false);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InputNumericUpdateKeepsItsValueWhenConversionPollReplacesTheCell(bool borrowedKey) =>
        AssertConversionSnapshot(borrowedKey, input: true);

    private static void AssertConversionSnapshot(bool borrowedKey, bool input)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        Action? onPoll = null;
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, checkpoint: () => onPoll?.Invoke());
        var name = AwkArrayName.FromText("a");
        var array = variables.ResolveOrdinaryArray(name);
        var key = new AwkArrayKey("kept");
        var text = AwkText.FromString(new string('0', 2 * AwkRuntimeLimits.CancellationCheckBytes) + "7");
        if (input)
            variables.InitializeArrayExternalStringValues(name,
                new[] { new KeyValuePair<string, string>("kept", text.AsString()) },
                static pair => pair.Key, static pair => pair.Value);
        else variables.SetArrayValue(array, key, AwkValue.FromText(text));
        Assert.True(array.ByteLookup.TryGetValue("kept"u8, out var stored));
        // The test retains its source while the callback replaces the array's claim.
        using var sourceMemory = memory.Accounting.ReserveTransient(stored.ByteText.EstimatedRetainedMemoryBytes);
        var replaced = false;
        onPoll = () =>
        {
            onPoll = null;
            replaced = true;
            variables.SetArrayValue(array, key, AwkValue.FromNumber(99));
        };

        var target = borrowedKey
            ? variables.PrepareArrayUpdate(array, "kept"u8)
            : variables.PrepareArrayUpdate(array, key);

        Assert.True(replaced);
        Assert.Equal(7, target.Previous);
        Assert.Equal(key, target.Key);
        Assert.Equal(99, variables.GetArrayNumber(array, key));
        Assert.True(variables.TrySetCapturedArrayNumber(array, in target, target.Previous + 3));
        Assert.Equal(10, variables.GetArrayNumber(array, key));
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
        Assert.True(array.TryGetEntry(new("cell"), out _, out _, out var cell));
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
        new(memory.Accounting, AwkRuntimeCounters.Disabled, static (_, _) => { });
}
