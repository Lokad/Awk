namespace Lokad.Awk.Tests;

public sealed class AwkNestedArrayStorageTests
{
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

        var received = AwkFunctionArgument.FromValue(argument);
        Assert.Equal(AwkFunctionArgumentKind.Array, received.Kind);
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
        variables.ClearArray(name);
        Assert.True(variables.TryGetOrdinaryArray(name, out var parent));
        var empty = memory.Budget.Reserved;

        var child = variables.GetSubarray(parent, AwkArrayKey.FromOrdinal(1));
        var grandchild = variables.GetSubarray(child, AwkArrayKey.FromOrdinal(2));
        variables.SetArrayValue(grandchild, AwkArrayKey.FromOrdinal(3), AwkValue.FromText(new string('x', 4096)));
        Assert.True(grandchild.IsWithin(parent));
        Assert.InRange(memory.Budget.Reserved, empty + 4096, long.MaxValue);
        variables.RemoveFunctionLocalArray(name);
        Assert.Equal(0, memory.Budget.Reserved);
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
        var parent = new AwkArrayIdentity(new());
        var child = new AwkArrayIdentity(new(), parent);
        parent.Slots.Add(AwkArrayKey.FromOrdinal(1), new(AwkVariableSlot.FromArrayBinding(child)));
        var parameter = AwkVariableName.FromText("p");
        var calls = new AwkFunctionCallStack(4, AwkMemoryAccounting.Disabled, _ => AwkValue.Unassigned(),
            checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        calls.PushFrame(new([parameter]), [AwkFunctionArgument.Array(child)]);
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

    private static AwkVariables Variables(CooperativeTestMemory memory, AwkRuntimeCounters? counters = null) =>
        new(memory.Accounting, counters ?? AwkRuntimeCounters.Disabled, AwkValue.FromText,
            AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal), (_, _) => { });

    private static AwkArrayIdentity Parent(AwkVariables variables)
    {
        var name = AwkArrayName.FromText("a");
        variables.ClearArray(name);
        Assert.True(variables.TryGetOrdinaryArray(name, out var parent));
        return parent;
    }
}
