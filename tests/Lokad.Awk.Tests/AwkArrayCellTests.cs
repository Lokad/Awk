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
        Assert.True(Assert.Single(variables.ResolveOrdinaryArray(name).Slots).Key.IsOrdinal);
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
        Assert.Single(array.Slots);
        Assert.Equal(5, variables.GetArrayNumber(array, AwkArrayKey.FromOrdinal(1)));
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Dictionary<AwkArrayKey, AwkArrayCell>> RetireTable(AwkVariables variables,
        AwkArrayName name, AwkArrayIdentity array, bool detach)
    {
        variables.SetArrayValue(array, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var reference = new WeakReference<Dictionary<AwkArrayKey, AwkArrayCell>>(array.Slots);
        if (detach) variables.RemoveFunctionLocalArray(name);
        else variables.ClearArray(name);
        return reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> ReplaceAndRetire(AwkVariables variables, AwkArrayIdentity array,
        AwkArrayKey key, bool clear)
    {
        var text = AwkText.FromString(new string('x', 1_000_000));
        variables.SetArrayValue(array, key, AwkValue.FromText(text));
        if (clear) variables.ClearArray(array);
        else variables.DeleteArrayValue(array, key);
        return new(text);
    }

    private static AwkVariables Variables(CooperativeTestMemory memory) =>
        new(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), static (_, _) => { });
}
