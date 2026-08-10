using Lokad.Awk;
using Xunit.Sdk;

namespace Lokad.Awk.Tests;

public sealed class AwkFunctionLocalSlotTests
{
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
    public void AwkFunctionCallStack_ScalarProjectionLeavesSiblingPotentialAliasPromotable()
    {
        var stack = CreateCallStack(AwkRuntimeCounters.Disabled);
        var firstName = AwkVariableName.FromText("first");
        var secondName = AwkVariableName.FromText("second");
        var sharedArrayName = AwkArrayName.FromText("shared");
        stack.PushFrame(
            new AwkFunctionLocalLayout([firstName, secondName]),
            [
                AwkFunctionArgument.PotentialArray(sharedArrayName),
                AwkFunctionArgument.PotentialArray(sharedArrayName)
            ]);

        try
        {
            Assert.True(stack.TryResolveLocalValue(firstName, out var firstLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.Scalar, firstLocal.Kind);
            Assert.True(stack.TryResolveOrPromoteArrayName(secondName, out var promotedName));
            Assert.Equal(sharedArrayName, promotedName);

            Assert.True(stack.TryResolveLocal(firstName, out firstLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.Scalar, firstLocal.Kind);
            Assert.True(stack.TryResolveLocal(secondName, out var secondLocal));
            Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, secondLocal.Kind);
        }
        finally
        {
            stack.PopFrame(static _ => { });
        }
    }

    private static AwkFunctionCallStack CreateCallStack(AwkRuntimeCounters counters) =>
        new(
            maxFunctionCallDepth: 8,
            AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters),
            static text => AwkValue.FromText(text),
            static text => AwkValue.FromNumericString(text),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal));
}
