using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed partial class AwkFunctionStorageTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(5)] [InlineData(8)]
    public void GovernedWarmFramesReuseTheirFundedBackingWithoutPerCallArrays(int count)
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layout = Layout(count);
        AwkFunctionLocalSlot[] arguments = [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))];
        for (var i = 0; i < 100; i++) Cycle();
        stack.PushFrame(layout, arguments);
        Assert.True(stack.TryResolveLocalValue(new AwkFunctionLocalSlotId(0), out var value));
        Assert.Equal(42, value.Value.AsNumber());
        if (count > 1)
        {
            Assert.True(stack.TryResolveLocal(new AwkFunctionLocalSlotId(count - 1), out var missing));
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, missing.Kind);
        }
        stack.PopFrame(static _ => { });
        var retained = memory.Budget.Reserved;
        Assert.True(retained >= count * Unsafe.SizeOf<AwkFunctionLocalSlot>());
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++) Cycle();
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
        Assert.Equal(retained, memory.Budget.Reserved);
        Assert.False(stack.IsInsideFunction);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Cycle()
        {
            stack.PushFrame(layout, arguments);
            stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void LargerInactiveCapacityStaysFundedAcrossSmallAndDeeperCalls()
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        var stack = Stack(memory);
        stack.PushFrame(Layout(10_000), []);
        stack.PopFrame(static _ => { });
        var backingBytes = 10_000L * Unsafe.SizeOf<AwkFunctionLocalSlot>();
        Assert.InRange(memory.Budget.Reserved, backingBytes, 2 * backingBytes + 4096);
        var retained = memory.Budget.Reserved;
        stack.PushFrame(Layout(3), []);
        stack.PopFrame(static _ => { });
        Assert.Equal(retained, memory.Budget.Reserved);
        var small = Layout(1);
        for (var i = 0; i < 32; i++) stack.PushFrame(small, []);
        Assert.InRange(memory.Budget.Reserved, retained, retained + 32 * 1024);
        for (var i = 0; i < 32; i++) stack.PopFrame(static _ => { });
        Assert.False(stack.IsInsideFunction);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void AlternatingArityDoesNotAllocateOrRefundReusableBacking()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        var layouts = new[] { Layout(8), Layout(1), Layout(3) };
        AwkFunctionLocalSlot[] arguments = [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))];
        for (var index = 0; index < 100; index++) Cycle();
        var retained = memory.Budget.Reserved;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++) Cycle();
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
        Assert.Equal(retained, memory.Budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Cycle()
        {
            foreach (var layout in layouts)
            {
                stack.PushFrame(layout, arguments);
                stack.PopFrame(static _ => { });
            }
        }
    }

    [Fact]
    public void RefusedReplacementKeepsCallerAndDoesNotRetryAfterNormalRetirement()
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        var stack = Stack(memory);
        var callerName = AwkVariableName.FromText("caller");
        stack.PushFrame(new([callerName]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);
        stack.PushFrame(Layout(4096), []);
        stack.PopFrame(static _ => { });
        var funded = memory.Budget.Reserved;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => stack.PushFrame(Layout(8192), []));
        Assert.True(stack.TryResolveLocalValue(callerName, out var caller));
        Assert.Equal(42, caller.Value.AsNumber());
        Assert.InRange(memory.Budget.Reserved, 1, funded - 4096L * Unsafe.SizeOf<AwkFunctionLocalSlot>());
        Assert.Equal(1, memory.Budget.Refusals);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        stack.PopFrame(static _ => { });
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CancellationDuringBackingAdmissionPrecedesTheLargeArrayAllocation()
    {
        using var memory = new CooperativeTestMemory(16 << 20);
        using var cancellation = new CancellationTokenSource();
        var stack = Stack(memory, cancellation.Token.ThrowIfCancellationRequested);
        stack.PushFrame(Layout(1), []);
        stack.PopFrame(static _ => { });
        var layout = Layout(100_000);
        var before = GC.GetAllocatedBytesForCurrentThread();
        memory.Budget.OnGrowth = _ => cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => stack.PushFrame(layout, []));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 64 * 1024);
        Assert.False(stack.IsInsideFunction);
        Assert.InRange(memory.Budget.Reserved, 1, 4096);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CachedBackingReleasesScalarReferences()
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        var stack = Stack(memory);
        var weak = FillAndPop(stack);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.TryGetTarget(out _));
        Assert.InRange(memory.Budget.Reserved, 1, 4096);
        GC.KeepAlive(stack);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void FailedSetupClearsPayloadButKeepsItsAdmittedBackingFunded()
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var stack = Stack(memory, cancellation.Token.ThrowIfCancellationRequested);
        // Cancel the captured text's admission after the smaller backing is funded.
        memory.Budget.OnGrowth = bytes => { if (bytes >= 32_768) cancellation.Cancel(); };
        AwkFunctionLocalSlot[] arguments = [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(new string('x', 32_768)))];
        Assert.ThrowsAny<OperationCanceledException>(() => stack.PushFrame(Layout(128), arguments));
        Assert.False(stack.IsInsideFunction);
        var backingBytes = 128L * Unsafe.SizeOf<AwkFunctionLocalSlot>();
        Assert.InRange(memory.Budget.Reserved, backingBytes, 2 * backingBytes + 4096);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void FramesReleasePayloadsAndReuseOtherAritiesAsUnassigned(int count)
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        var stack = Stack(memory);
        var weak = FillAndPop(stack, count);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.TryGetTarget(out _));
        foreach (var arity in new[] { 3, 2, 1, 0, 2 })
        {
            stack.PushFrame(Layout(arity), []);
            for (var index = 0; index < arity; index++)
            {
                Assert.True(stack.TryResolveLocal(new AwkFunctionLocalSlotId(index), out var local));
                Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, local.Kind);
            }
            stack.PopFrame(static _ => { });
        }
        Assert.False(stack.IsInsideFunction);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> FillAndPop(AwkFunctionCallStack stack, int count = 8)
    {
        var text = AwkText.FromString(new string('x', 32_768));
        var value = AwkValue.FromText(text);
        stack.PushFrame(Layout(count), Enumerable.Repeat(AwkFunctionLocalSlot.Scalar(value), count).ToArray());
        stack.PopFrame(static _ => { });
        return new(text.Backing ?? throw new InvalidOperationException("Expected owned nonempty text."));
    }

    private static AwkFunctionLocalLayout Layout(int count) =>
        new(Enumerable.Range(0, count).Select(i => AwkVariableName.FromText("p" + i)).ToArray());
}
