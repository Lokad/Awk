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
        Assert.False(cell.TrySetNumber(8));
        Assert.Equal(0, cell.RetainedTextMemoryBytes);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(text.TryGetTarget(out _));
        Assert.InRange(memory.Budget.Reserved, 1, 100_000);
        GC.KeepAlive(target);
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
        new(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromText, AwkValue.FromNumericString,
            text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), static (_, _) => { });
}
