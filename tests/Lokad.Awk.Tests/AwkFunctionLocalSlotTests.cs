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

    private static AwkFunctionCallStack CreateCallStack(AwkRuntimeCounters counters) =>
        new(
            maxFunctionCallDepth: 8,
            AwkMemoryGovernor.Create(AwkRuntimeLimits.CreateDefault(), counters),
            static text => AwkValue.FromText(text),
            static text => AwkValue.FromNumericString(text));
}
