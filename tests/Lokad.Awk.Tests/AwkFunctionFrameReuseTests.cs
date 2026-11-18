using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionFrameReuseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneTwoAndThreeLocalLayoutsRecycleAndKeepMissingValues(bool governed)
    {
        using var memory = CreateMemory(governed);
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        Assert.Equal(governed, accounting.IsEnabled);
        var stack = memory.State.FunctionCalls;
        var bootstrap = budget.Reserved;
        AwkVariableName[] names = [AwkVariableName.FromText("a"), AwkVariableName.FromText("b"), AwkVariableName.FromText("c")];
        foreach (var count in new[] { 2, 1, 3, 0, 2 })
        {
            var layout = new AwkFunctionLocalLayout(names[..count]);
            var args = Enumerable.Range(0, count == 2 ? 1 : count)
                .Select(index => AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(20 + index))).ToArray();
            memory.State.PushFunctionFrame(layout, args);
            try
            {
                for (var index = 0; index < count; index++)
                {
                    Assert.True(stack.TryResolveLocal(new AwkFunctionLocalSlotId(index), out var local));
                    if (count == 2 && index == 1)
                    {
                        Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, local.Kind);
                        Assert.True(stack.TryAssignLocal(new AwkFunctionLocalSlotId(1), AwkTestHarness.NumericInput("007")));
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
            if (governed) Assert.True(budget.Reserved > bootstrap); // Cached frame, not its released payload.
        }
        memory.Dispose();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecycledPairReleasesTheSecondPayloadAfterActualUse(bool governed)
    {
        using var memory = CreateMemory(governed);
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        Assert.Equal(governed, accounting.IsEnabled);
        var stack = memory.State.FunctionCalls;
        var weak = RunPairWithOwnedPayload(memory.State);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.TryGetTarget(out _));
        GC.KeepAlive(stack);
        memory.Dispose();
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void RefusedPairEntryPreservesCallerAndRefundsAtUnwind()
    {
        using var memory = CreateMemory(true);
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        var stack = memory.State.FunctionCalls;
        var name = AwkVariableName.FromText("parent");
        memory.State.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(19))]);
        var before = budget.Reserved;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var shared = AwkArrayName.FromText("shared");
        var origin = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        Assert.Throws<AwkRuntimeException>(() => memory.State.PushFunctionFrame(
            new([AwkVariableName.FromText("a"), AwkVariableName.FromText("b")]),
            [AwkFunctionLocalSlot.PotentialArrayAlias(shared, origin), AwkFunctionLocalSlot.PotentialArrayAlias(shared, origin)]));
        Assert.Equal(before, budget.Reserved);
        Assert.Equal(before, accounting.LiveBytes);
        Assert.True(stack.TryResolveLocalValue(name, out var local));
        Assert.Equal(19, local.Value.AsNumber());
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        stack.PopFrame(static _ => { });
        Assert.False(stack.IsInsideFunction);
        Assert.True(budget.Reserved > 0);
        memory.Dispose();
        Assert.Equal(0, budget.Reserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> RunPairWithOwnedPayload(AwkRuntimeState state)
    {
        var stack = state.FunctionCalls;
        var text = AwkText.FromString(new string('x', 17_000));
        var weak = new WeakReference<object>(text.Backing ?? throw new InvalidOperationException("Expected owned nonempty text."));
        state.PushFunctionFrame(new([AwkVariableName.FromText("a"), AwkVariableName.FromText("b")]),
            [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(1)), AwkFunctionLocalSlot.Scalar(AwkValue.FromText(text))]);
        Assert.True(stack.TryResolveLocalValue(new AwkFunctionLocalSlotId(1), out var local));
        Assert.Equal(17_000, local.Value.ByteText.ByteLength);
        stack.PopFrame(static _ => { });
        return weak;
    }

    private static CooperativeTestMemory CreateMemory(bool governed)
    {
        var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        var options = memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
            null, AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(8)));
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None, options));
    }
}

