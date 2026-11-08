using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkNestedArrayStorageTests
{
    [Fact]
    public void RetiredPathKeepsFundedCapacityWithoutRetainingKeyPayload()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = State(memory);
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Paths: 1));
        var name = AwkArrayName.FromText("a");
        storage.BeginPath(new(name, default, 2));
        var path = storage.Path(0);
        var capacity = memory.Budget.Reserved;
        var text = CaptureLargeKey(path, state);
        Assert.True(memory.Budget.Reserved >= capacity + 4096);

        storage.ReleasePath(0);
        Assert.False(path.IsActive);
        Assert.Equal(capacity, memory.Budget.Reserved);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(text.TryGetTarget(out _));

        storage.BeginPath(new(AwkArrayName.FromText("other"), default, 1));
        Assert.Same(path, storage.Path(0));
        Assert.Equal(capacity, memory.Budget.Reserved);
        var key = new AwkArrayKey("last");
        path.CaptureKey(state, 0, key);
        Assert.Equal(key, path.Key); // Shorter use must not read the retained array's last slot.
        storage.ReleasePath(0);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = Assert.Throws<AwkRuntimeException>(() => storage.BeginPath(new(name, default, 64)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(capacity, memory.Budget.Reserved);
        Assert.False(path.IsActive);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void TakenPathKeepsItsOwnFundingAfterStorageDisposal()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = State(memory);
        var initial = memory.Budget.Reserved;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Paths: 1));
        storage.BeginPath(new(AwkArrayName.FromText("a"), default, 2));
        var path = storage.Path(0);
        path.CaptureKey(state, 0, new AwkArrayKey("row"));
        var key = new AwkArrayKey(AwkText.FromString(new string('x', 4096)));
        path.CaptureKey(state, 1, key);
        using var taken = storage.TakePath(0);
        storage.Dispose();
        Assert.Equal(key, taken.Key);
        Assert.True(memory.Budget.Reserved >= initial + 4096);
        taken.Dispose();
        Assert.Equal(initial, memory.Budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CancellationAfterPathAdmissionRefundsUnpublishedStorage(bool growth)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = State(memory, cancellation.Token);
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(0, Paths: 1));
        var descriptor = new AwkRegionPath(AwkArrayName.FromText("a"), default, 2);
        storage.BeginPath(descriptor);
        var path = storage.Path(0);
        var capacity = memory.Budget.Reserved;
        if (!growth) path.CaptureKey(state, 0, new AwkArrayKey("first"));
        var next = new AwkArrayKey(AwkText.FromString(new string('x', 4096)));
        memory.Budget.OnGrowth = _ => cancellation.Cancel();
        try
        {
            if (growth)
                Assert.ThrowsAny<OperationCanceledException>(() => storage.BeginPath(descriptor with { Count = 64 }));
            else
                Assert.ThrowsAny<OperationCanceledException>(() => path.CaptureKey(state, 1, next));
        }
        finally { memory.Budget.OnGrowth = null; }
        storage.ReleasePath(0);
        Assert.False(path.IsActive);
        Assert.Equal(capacity, memory.Budget.Reserved);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> CaptureLargeKey(AwkCapturedArrayPath path, AwkRuntimeState state)
    {
        var text = AwkText.FromString(new string('x', 4096));
        path.CaptureKey(state, 0, new AwkArrayKey(text));
        return new(text.Backing ?? throw new InvalidOperationException("Expected owned nonempty text."));
    }

    [Fact]
    public void CapturedPathRebindsDetachedOwnersAfterLaterKeyEffects()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(AwkTestHarness.CreateRuntimeOptions()))).State;
        var name = AwkArrayName.FromText("a");
        var row = new AwkArrayKey("row");
        var leaf = new AwkArrayKey("leaf");
        var original = state.GetSubarray(name, row, default);
        state.SetArrayAwkValue(original, leaf, AwkValue.FromNumber(7));
        using (var captured = AwkCapturedArrayPath.Begin(state, name, default, 2))
        {
            captured.Append(state, 0, row);
            state.ClearArray(name);
            var replacement = state.GetSubarray(name, row, default);
            state.SetArrayAwkValue(replacement, leaf, AwkValue.FromNumber(9));
            captured.Append(state, 1, leaf);
            Assert.True(original.IsDetached);
            Assert.NotSame(original, replacement);
            Assert.Same(replacement, captured.ResolveOwner(state));
            Assert.Equal(9, state.GetArrayAwkValue(captured.ResolveOwner(state), captured.Key).AsNumber());
        }
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void UntouchedElementArgumentsShareArrayPromotionButEarlierScalarCopiesDoNot()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var parent = Parent(variables);
        var key = AwkArrayKey.FromOrdinal(1);
        var argument = variables.GetArrayValue(parent, key);
        var earlier = argument.AsScalarCopy();
        Assert.Equal(AwkRuntimeValueKind.Untyped, argument.RuntimeKind);

        var child = variables.GetSubarray(parent, key);
        variables.SetArrayValue(child, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(7));
        Assert.True(argument.TryGetArrayBinding(out var promoted));
        Assert.Same(child, promoted);
        Assert.Equal(AwkRuntimeValueKind.Array, argument.RuntimeKind);
        Assert.True(earlier.IsUnassignedValue);
        Assert.Equal("", earlier.AsString());
        Assert.Throws<AwkRuntimeException>(() => argument.AsNumber());
        Assert.Throws<AwkRuntimeException>(() => argument.ForceScalar());

        var received = AwkFunctionLocalSlot.FromArgumentValue(argument);
        Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, received.Kind);
        Assert.True(received.Value.TryGetArrayBinding(out var transported));
        Assert.Same(child, transported);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefusedChildAdmissionLeavesTheParentAndOriginUntouched(bool missingRead)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var parent = Parent(variables);
        var key = AwkArrayKey.FromOrdinal(1);
        var argument = missingRead ? variables.GetArrayValue(parent, key) : default;
        var reserved = memory.Budget.Reserved;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;

        var failure = Assert.Throws<AwkRuntimeException>(() => variables.GetSubarray(parent, key));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(missingRead ? 1 : 0, parent.Slots.Count);
        Assert.False(argument.IsArrayValue);
        if (missingRead) Assert.Equal(AwkRuntimeValueKind.Untyped, argument.RuntimeKind);
        Assert.Equal(reserved, memory.Budget.Reserved);
    }

    [Fact]
    public void ScalarTypingRejectsPromotionWithoutChangingTheElement()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var parent = Parent(variables);
        var key = AwkArrayKey.FromOrdinal(1);
        variables.GetArrayValue(parent, key).ForceScalar();
        var reserved = memory.Budget.Reserved;

        Assert.Throws<AwkRuntimeException>(() => variables.GetSubarray(parent, key));
        Assert.Equal(AwkRuntimeValueKind.Unassigned, variables.GetArrayValue(parent, key).RuntimeKind);
        Assert.Equal(reserved, memory.Budget.Reserved);
    }

    [Fact]
    public void ChildOwnersAndGrowthAreFundedAndTheirFunctionTreeRefunds()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = Variables(memory);
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("a"), 1);
        var retained = memory.Budget.Reserved;
        variables.ClearArray(name);
        Assert.True(variables.TryGetOrdinaryArray(name, out var parent));
        var empty = memory.Budget.Reserved;

        var child = variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(1));
        var grandchild = variables.GetSubarray(child, AwkArrayKey.FromOrdinal(2));
        variables.SetArrayValue(grandchild, AwkArrayKey.FromOrdinal(3), AwkValue.FromText(new string('x', 4096)));
        Assert.True(grandchild.IsWithin(parent));
        Assert.InRange(memory.Budget.Reserved, empty + 4096, long.MaxValue);
        variables.RemoveFunctionLocalArray(name);
        Assert.Equal(retained, memory.Budget.Reserved);
    }

    [Fact]
    public void PromotionCountersDistinguishNewOwnersFromReuseAndRefusal()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var variables = Variables(memory, counters);
        var parent = Parent(variables);
        var first = variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(1));
        Assert.Same(first, variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(1)));
        var pending = variables.GetArrayValue(parent, AwkArrayKey.FromOrdinal(2));
        Assert.True(pending.TryGetUntypedScalar(out var origin));
        var second = variables.PromoteArrayOrigin(origin);
        Assert.Same(second, variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(2)));
        Assert.Equal(2, counters.SubarrayPromotions);
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(() => variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(3)));
        Assert.Equal(2, counters.SubarrayPromotions);
    }

    [Fact]
    public void AliasRepairPollsCancellationAndStillCompletesDuringUnwind()
    {
        using var cancellation = new CancellationTokenSource();
        var parent = new AwkArrayIdentity(new(AwkArrayKeyComparer.Instance));
        var child = new AwkArrayIdentity(new(AwkArrayKeyComparer.Instance), parent);
        parent.Slots.Add(AwkArrayKey.FromOrdinal(1), new(AwkVariableSlot.FromArrayBinding(child)));
        var parameter = AwkVariableName.FromText("p");
        var calls = new AwkFunctionCallStack(4, AwkMemoryAccounting.Disabled, _ => AwkValue.Unassigned(),
            checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        calls.PushFrame(new([parameter]), [AwkFunctionLocalSlot.ArrayAlias(child)]);
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => calls.RepairSubarrayAliases(parent, false, true));
        Assert.True(calls.TryGetBoundArray(parameter, out var original));
        Assert.Same(child, original);
        calls.RepairSubarrayAliases(parent, false, false);
        Assert.True(calls.TryResolveLocal(parameter, out var repaired));
        Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, repaired.Kind);
        Assert.True(repaired.ArrayName.IsFunctionLocal);
        Assert.False(calls.TryGetBoundArray(parameter, out _));
        calls.PopFrame(_ => { });
    }

    private static AwkRuntimeState State(CooperativeTestMemory memory, CancellationToken token = default) =>
        memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            token, memory.Configure(AwkTestHarness.CreateRuntimeOptions()))).State;

    private static AwkVariables Variables(CooperativeTestMemory memory, AwkRuntimeCounters? counters = null) =>
        new(memory.Accounting, counters ?? AwkRuntimeCounters.Disabled, AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });

    private static AwkArrayIdentity Parent(AwkVariables variables)
    {
        var name = AwkArrayName.FromText("a");
        variables.ClearArray(name);
        Assert.True(variables.TryGetOrdinaryArray(name, out var parent));
        return parent;
    }
}
