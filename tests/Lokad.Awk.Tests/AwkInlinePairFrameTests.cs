using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkInlinePairFrameTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OneTwoAndThreeLocalLayoutsRecycleAndKeepMissingValues(int mode)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using var owner = mode == 2
            ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None) : AwkExecutionMemoryOwner.Disabled;
        var accounting = Accounting(mode, owner);
        var stack = Stack(accounting);
        AwkVariableName[] names = [AwkVariableName.FromText("a"), AwkVariableName.FromText("b"), AwkVariableName.FromText("c")];
        foreach (var count in new[] { 2, 1, 3, 0, 2 })
        {
            var layout = new AwkFunctionLocalLayout(names[..count]);
            var args = Enumerable.Range(0, count == 2 ? 1 : count)
                .Select(index => AwkFunctionArgument.Scalar(AwkValue.FromNumber(20 + index))).ToArray();
            Assert.Equal(count is 1 or 2, stack.PushFrame(layout, args));
            try
            {
                for (var index = 0; index < count; index++)
                {
                    Assert.True(stack.TryResolveLocal(new AwkFunctionLocalSlotId(index), out var local));
                    if (count == 2 && index == 1)
                    {
                        Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, local.Kind);
                        Assert.True(stack.TryAssignLocal(new AwkFunctionLocalSlotId(1), AwkValue.FromNumericString("007")));
                        Assert.True(stack.TryResolveLocalValue(new AwkFunctionLocalSlotId(1), out local));
                        Assert.Equal("007", local.Value.ByteText.AsString());
                        Assert.Equal(7, local.Value.AsNumber());
                    }
                    else
                    {
                        Assert.True(stack.TryResolveLocalValue(new AwkFunctionLocalSlotId(index), out local));
                        Assert.Equal(20 + index, local.Value.AsNumber());
                    }
                }
            }
            finally { stack.PopFrame(static _ => { }); }
            Assert.False(stack.IsInsideFunction);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(0, accounting.CurrentCommittedBytes);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RecycledPairReleasesTheSecondPayloadAfterActualUse(int mode)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using var owner = mode == 2
            ? AwkExecutionMemoryOwner.Create(budget, CancellationToken.None) : AwkExecutionMemoryOwner.Disabled;
        var stack = Stack(Accounting(mode, owner));
        var weak = RunPairWithOwnedPayload(stack);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.TryGetTarget(out _));
        GC.KeepAlive(stack);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task RefusedPairEntryPreservesCallerAndRefundsAtUnwind()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = Accounting(2, owner);
        var stack = Stack(accounting);
        var name = AwkVariableName.FromText("parent");
        stack.PushFrame(new([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(19))]);
        var before = budget.Reserved;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var shared = AwkArrayName.FromText("shared");
        var origin = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        Assert.Throws<AwkRuntimeException>(() => stack.PushFrame(
            new([AwkVariableName.FromText("a"), AwkVariableName.FromText("b")]),
            [AwkFunctionArgument.PotentialArray(shared, origin), AwkFunctionArgument.PotentialArray(shared, origin)]));
        Assert.Equal(before, budget.Reserved);
        Assert.Equal(before, accounting.CurrentCommittedBytes);
        Assert.Equal(0, accounting.CurrentReservedBytes);
        Assert.True(stack.TryResolveLocalValue(name, out var local));
        Assert.Equal(19, local.Value.AsNumber());
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        stack.PopFrame(static _ => { });
        Assert.False(stack.IsInsideFunction);
        Assert.Equal(0, budget.Reserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> RunPairWithOwnedPayload(AwkFunctionCallStack stack)
    {
        var text = AwkText.FromString(new string('x', 17_000));
        var weak = new WeakReference<AwkText>(text);
        stack.PushFrame(new([AwkVariableName.FromText("a"), AwkVariableName.FromText("b")]),
            [AwkFunctionArgument.Scalar(AwkValue.FromNumber(1)), AwkFunctionArgument.Scalar(AwkValue.FromText(text))]);
        Assert.True(stack.TryResolveLocalValue(new AwkFunctionLocalSlotId(1), out var local));
        Assert.Equal(17_000, local.Value.ByteText.ByteLength);
        stack.PopFrame(static _ => { });
        return weak;
    }

    private static AwkMemoryAccounting Accounting(int mode,AwkExecutionMemoryOwner owner)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        return AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
    }

    private static AwkFunctionCallStack Stack(AwkMemoryAccounting accounting) =>
        new(8, accounting, static slot => slot.ToValue(
            static text => AwkValue.FromText(text),
            static text => AwkValue.FromNumericString(text),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal)));
}

