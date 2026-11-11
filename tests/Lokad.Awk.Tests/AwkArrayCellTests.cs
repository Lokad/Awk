using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkArrayCellTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CapturedCellRetiresItsPayloadBeforeArrayRefund(bool clear)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var array = variables.ResolveOrdinaryArray(AwkArrayName.FromText("a"));
        var key = AwkArrayKey.FromOrdinal(1);
        variables.SetArrayValue(array, key, AwkValue.FromNumber(5));
        var target = variables.PrepareArrayUpdate(array, key);
        var cell = Assert.IsType<AwkArrayCell>(target.Cell);
        using var heldTarget = memory.Accounting.ReserveTransient(target.RetainedTargetBytes);
        var text = ReplaceAndRetire(variables, array, key, clear);
        Assert.Equal(5, target.Previous);
        Assert.False(cell.IsAttached);
        Assert.Equal(key, target.Key);
        Assert.Equal(0, cell.RetainedTextMemoryBytes);
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
        variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(5));
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() =>
            variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(9)));
        Assert.Single(array);
        Assert.Equal(5, variables.GetArrayNumber(array, AwkArrayKey.FromOrdinal(1)));
        Assert.True(array.ByteLookup.TryGetValue("1"u8, out var stored));
        Assert.Equal(5, variables.ReadArraySlotNumber(stored));
        Assert.False(array.ByteLookup.ContainsKey("2"u8));
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
        Assert.True(array.ByteLookup.TryGetValue("0007"u8, out var original));
        for (var index = 0; index < 1024; index++)
            variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));

        Assert.True(array.ByteLookup.TryGetValue("0007"u8, out var retainedKey, out var grown));
        Assert.Same(original, grown);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkArrayCell> RetireTable(AwkVariables variables,
        AwkArrayName name, AwkArrayIdentity array, bool detach)
    {
        variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var reference = new WeakReference<AwkArrayCell>(array[AwkArrayKey.FromOrdinal(1)]);
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
