using Lokad.Awk;
using Xunit.Sdk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionLocalSlotTests
{
    [Fact]
    public void GlobalBindingScopeRetainsChargesPhysicalDepthAndNestedBindings()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForMemory = new CooperativeTestMemory(1 << 20, AwkRuntimeCounters.Disabled);
        var memory = memoryForMemory.Accounting;
        var stack = new AwkFunctionCallStack(2, memory, static slot => slot.ToValue(
            static text => AwkValue.FromText(text), static text => AwkValue.FromNumericString(text),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal)));
        var name = AwkVariableName.FromText("x");
        var slot = new AwkFunctionLocalSlotId(0);
        var layout = new AwkFunctionLocalLayout([name]);
        var initial = memory.CurrentCommittedBytes;
        stack.PushFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7))]);
        var retained = memory.CurrentCommittedBytes;
        Assert.True(retained > initial);
        using (stack.EnterGlobalBindings())
        {
            Assert.True(stack.IsInsideFunction);
            Assert.Equal(retained, memory.CurrentCommittedBytes);
            Assert.False(stack.HasScalarLocal(slot));
            Assert.False(stack.TryResolveLocalNumber(slot, out _));
            Assert.False(stack.TryResolveScalarLocalValue(slot, out _));
            Assert.False(stack.TryResolveLocal(name, out _));
            Assert.False(stack.TryResolveLocal(slot, out _));
            Assert.False(stack.TryResolveLocalValue(name, out _));
            Assert.False(stack.TryResolveLocalValue(slot, out _));
            Assert.False(stack.TryResolveArrayArgument(name, out _));
            Assert.False(stack.TryResolveOrPromoteArrayName(name, out _));
            Assert.False(stack.TryGetArrayReadName(name, out _));
            Assert.False(stack.TryAssignLocal(name, AwkValue.FromNumber(99)));
            Assert.False(stack.TryAssignLocal(slot, AwkValue.FromNumber(99)));
            Assert.Throws<AwkRuntimeException>(() => stack.SetReturnValue(AwkValue.FromNumber(99)));
            Assert.False(stack.TryConsumeReturnValue(out _));
            stack.PushFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(3))]);
            try
            {
                Assert.True(stack.TryResolveLocalValue(name, out var nested));
                Assert.Equal(3, nested.Value.AsNumber());
                using (stack.EnterGlobalBindings())
                {
                    Assert.False(stack.TryResolveLocal(name, out _));
                    Assert.Throws<AwkRuntimeException>(() => stack.PushFrame(layout, []));
                    Assert.Equal(0, memory.CurrentReservedBytes);
                }
                Assert.True(stack.TryResolveLocalValue(name, out nested));
                Assert.Equal(3, nested.Value.AsNumber());
            }
            finally { stack.PopFrame(static _ => { }); }
            Assert.False(stack.TryResolveLocal(name, out _));
            Assert.Equal(retained, memory.CurrentCommittedBytes);
        }
        Assert.True(stack.TryResolveLocalValue(name, out var restored));
        Assert.Equal(7, restored.Value.AsNumber());
        stack.PopFrame(static _ => { });
        Assert.Equal(initial, memory.CurrentCommittedBytes);
    }

    [Fact]
    public void GlobalBindingScopeKeepsHiddenAliasesInPromotion()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var outer = AwkVariableName.FromText("outer");
        var inner = AwkVariableName.FromText("inner");
        var array = AwkArrayName.FromText("shared");
        stack.PushFrame(new([outer]), [AwkFunctionArgument.PotentialArray(array)]);
        using (stack.EnterGlobalBindings())
        {
            stack.PushFrame(new([inner]), [AwkFunctionArgument.PotentialArray(array)]);
            try
            {
                Assert.True(stack.TryResolveOrPromoteArrayName(inner, out var promoted));
                Assert.Equal(array, promoted);
            }
            finally { stack.PopFrame(static _ => { }); }
            Assert.False(stack.TryResolveLocal(outer, out _));
        }
        Assert.True(stack.TryResolveLocal(outer, out var local));
        Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, local.Kind);
        Assert.Equal(array, local.ArrayName);
        stack.PopFrame(static _ => { });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GlobalBindingScopeRestoresCallerAfterPendingCompletion(bool cancel)
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var name = AwkVariableName.FromText("x");
        stack.PushFrame(new([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7))]);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task ExecuteGlobalAsync()
        {
            using var scope = stack.EnterGlobalBindings();
            await completion.Task.ConfigureAwait(false);
            Assert.False(stack.TryResolveLocal(name, out _));
        }
        var pending = ExecuteGlobalAsync();
        Assert.False(pending.IsCompleted);
        Assert.True(stack.IsInsideFunction);
        Assert.False(stack.TryResolveLocal(name, out _));
        if (cancel)
        {
            completion.SetCanceled();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        else
        {
            completion.SetResult();
            await pending;
        }
        Assert.True(stack.TryResolveLocalValue(name, out var restored));
        Assert.Equal(7, restored.Value.AsNumber());
        stack.PopFrame(static _ => { });
    }

    [Fact]
    public void ScalarArgumentOrdinalsRespectCurrentKindsAndActiveLayout()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var first = new AwkFunctionLocalSlotId(0);
        Assert.False(stack.HasScalarLocal(first));
        AwkVariableName[] names = [AwkVariableName.FromText("x"), AwkVariableName.FromText("y"),
            AwkVariableName.FromText("potential"), AwkVariableName.FromText("array"), AwkVariableName.FromText("missing")];
        var array = AwkArrayName.FromText("a");
        stack.PushFrame(new AwkFunctionLocalLayout(names), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(7)),
            AwkFunctionArgument.Scalar(AwkValue.FromNumericString("007")),
            AwkFunctionArgument.PotentialArray(array), AwkFunctionArgument.Array(array)]);
        try
        {
            Assert.True(stack.HasScalarLocal(first));
            Assert.True(stack.HasScalarLocal(new AwkFunctionLocalSlotId(1)));
            foreach (var index in new[] { -1, 2, 3, 4, 5, int.MaxValue })
                Assert.False(stack.HasScalarLocal(new AwkFunctionLocalSlotId(index)));
            Assert.True(stack.TryResolveLocal(names[4], out var missing));
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, missing.Kind);
            Assert.True(stack.TryAssignLocal(new AwkFunctionLocalSlotId(4), AwkValue.FromNumber(9)));
            Assert.True(stack.HasScalarLocal(new AwkFunctionLocalSlotId(4)));
            stack.PushFrame(new AwkFunctionLocalLayout([names[0]]), [AwkFunctionArgument.Array(array)]);
            try { Assert.False(stack.HasScalarLocal(first)); }
            finally { stack.PopFrame(static _ => { }); }
            Assert.True(stack.HasScalarLocal(first));
        }
        finally { stack.PopFrame(static _ => { }); }
        Assert.False(stack.HasScalarLocal(first));
    }

    [Fact]
    public void AwkFunctionCallStack_ReusesFrameAndSlotStorage()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        AwkFunctionArgument[] arguments =
            [AwkFunctionArgument.Scalar(AwkValue.FromText("value"))];
        stack.PushFrame(layout, arguments);
        stack.PopFrame(static _ => { });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            stack.PushFrame(layout, arguments);
            stack.PopFrame(static _ => { });
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.True(allocatedBytes < 1024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkFunctionCallStack_ResolvesNumericLocalSlotValueWithoutTextProjection()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var stack = CreateCallStack(counters);
        var localName = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([localName]);
        var slotId = new AwkFunctionLocalSlotId(0);
        stack.PushFrame(layout, [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);

        try
        {
            if (!stack.TryResolveLocalValue(slotId, out var warmup) ||
                warmup.Kind != AwkFunctionLocalSlotKind.Scalar ||
                warmup.Value.AsNumber() != 42)
            {
                throw new XunitException("Warmup local slot resolution failed.");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 10_000; index++)
            {
                if (!stack.TryResolveLocalValue(slotId, out var local) ||
                    local.Kind != AwkFunctionLocalSlotKind.Scalar ||
                    local.Value.AsNumber() != 42)
                {
                    throw new XunitException("Local slot value resolution failed.");
                }
            }

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.True(allocatedBytes < 1024, "Allocated bytes: " + allocatedBytes);
            Assert.Equal(0, counters.AwkTextAllocations);
            Assert.Equal(0, counters.Utf8Encodes);
        }
        finally
        {
            stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void AwkFunctionCallStack_PromotesDistinctPotentialArrayAliasesIndependently()
    {
        const int aliasCount = 512;
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var names = new AwkVariableName[aliasCount];
        var arguments = new AwkFunctionArgument[aliasCount];
        for (var index = 0; index < aliasCount; index++)
        {
            names[index] = AwkVariableName.FromText("parameter" + index);
            arguments[index] = AwkFunctionArgument.PotentialArray(
                AwkArrayName.FromText("argument" + index));
        }

        stack.PushFrame(new AwkFunctionLocalLayout(names), arguments);
        try
        {
            for (var index = 0; index < aliasCount; index++)
            {
                Assert.True(stack.TryResolveOrPromoteArrayName(names[index], out var arrayName));
                Assert.Equal(arguments[index].ArrayName, arrayName);
            }

            for (var index = 0; index < aliasCount; index++)
            {
                Assert.True(stack.TryResolveLocal(names[index], out var local));
                Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, local.Kind);
            }
        }
        finally
        {
            stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void AwkFunctionCallStack_PromotesEveryForwardedPotentialArrayAlias()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var outerName = AwkVariableName.FromText("outer");
        var innerName = AwkVariableName.FromText("inner");
        var sharedArrayName = AwkArrayName.FromText("shared");
        stack.PushFrame(
            new AwkFunctionLocalLayout([outerName]),
            [AwkFunctionArgument.PotentialArray(sharedArrayName)]);

        try
        {
            Assert.True(stack.TryResolveArrayArgument(outerName, out var forwardedArgument));
            stack.PushFrame(
                new AwkFunctionLocalLayout([innerName]),
                [forwardedArgument]);
            try
            {
                Assert.True(stack.TryResolveOrPromoteArrayName(innerName, out var promotedName));
                Assert.Equal(sharedArrayName, promotedName);
                Assert.False(stack.TryAssignLocal(innerName, AwkValue.FromNumber(1)));
            }
            finally
            {
                stack.PopFrame(static _ => { });
            }

            Assert.True(stack.TryResolveLocal(outerName, out var outerLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, outerLocal.Kind);
            Assert.False(stack.TryAssignLocal(outerName, AwkValue.FromNumber(1)));
        }
        finally
        {
            stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void AwkFunctionCallStack_ScalarProjectionPromotesEverySharedUntypedOrigin()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var firstName = AwkVariableName.FromText("first");
        var secondName = AwkVariableName.FromText("second");
        var sharedArrayName = AwkArrayName.FromText("shared");
        var value = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        stack.PushFrame(
            new AwkFunctionLocalLayout([firstName, secondName]),
            [
                AwkFunctionArgument.PotentialArray(sharedArrayName, value),
                AwkFunctionArgument.PotentialArray(sharedArrayName, value)
            ]);

        try
        {
            Assert.True(stack.TryResolveLocalValue(firstName, out var firstLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.Scalar, firstLocal.Kind);
            Assert.False(stack.TryResolveOrPromoteArrayName(secondName, out _));

            Assert.True(stack.TryResolveLocal(firstName, out firstLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.Scalar, firstLocal.Kind);
            Assert.True(stack.TryResolveLocal(secondName, out var secondLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.Scalar, secondLocal.Kind);
            Assert.True(secondLocal.Value.IsUnassignedValue);
        }
        finally
        {
            stack.PopFrame(static _ => { });
        }
    }

    private static AwkFunctionCallStack CreateCallStack(AwkRuntimeCounters counters) =>
        new(
            maxFunctionCallDepth: 8,
            AwkMemoryAccounting.Disabled,
            static slot => slot.ToValue(static text => AwkValue.FromText(text),
                static text => AwkValue.FromNumericString(text),
                static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal)));
}
