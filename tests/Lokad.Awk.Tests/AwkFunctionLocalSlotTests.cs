using Lokad.Awk;
using Xunit.Sdk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionLocalSlotTests
{
    [Fact]
    public void NumericOwnerWritesPreserveRawBitsAndEarlierSnapshots()
    {
        var cell = AwkFunctionLocalSlot.MissingScalar();
        foreach (var bits in new long[] { 0, long.MinValue, 0x3ff4000000000000,
            0x7ff0000000000000, unchecked((long)0xfff0000000000000UL),
            0x7ff8000000000123, 0x7ff0000000000001 })
        {
            var snapshot = cell;
            var prior = cell.Value;
            var priorBits = BitConverter.DoubleToInt64Bits(prior.StoredNumber);
            Assert.True(cell.TryWriteNumber(BitConverter.Int64BitsToDouble(bits)));
            Assert.True(cell.TryReadNumber(out var number));
            Assert.Equal(bits, BitConverter.DoubleToInt64Bits(number));
            Assert.Equal(AwkValueKind.Number, cell.Value.StoredKind);
            Assert.Null(cell.Value.StoredReference);
            Assert.Equal(default, cell.ArrayName);
            Assert.Equal(prior.StoredKind, snapshot.Value.StoredKind);
            Assert.Equal(priorBits, BitConverter.DoubleToInt64Bits(snapshot.Value.StoredNumber));
            Assert.Equal(priorBits, BitConverter.DoubleToInt64Bits(prior.StoredNumber));
        }
    }

    [Fact]
    public void NumericOwnerWritesRejectOtherRolesWithoutChangingTheirPayload()
    {
        var text = AwkText.FromSourceLiteralString("0007");
        var origin = new AwkUntypedScalar();
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("local"), long.MaxValue);
        var array = new AwkArrayIdentity(new(AwkArrayKeyComparer.Instance));
        AwkFunctionLocalSlot[] cells = [AwkFunctionLocalSlot.Scalar(AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumericString(text)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromPreparedNumericInput(text, 7)),
            AwkFunctionLocalSlot.Scalar(AwkValue.Unassigned()),
            AwkFunctionLocalSlot.ArrayAlias(name, array),
            AwkFunctionLocalSlot.PotentialArrayAlias(name, AwkValue.FromUntypedScalar(origin)),
            AwkFunctionLocalSlot.UntypedScalar(AwkValue.FromUntypedScalar(origin)),
            AwkFunctionLocalSlot.PreparedKey(new("key"))];
        foreach (var initial in cells)
        {
            var cell = initial;
            Assert.False(cell.TryWriteNumber(42));
            Assert.Equal(initial.Kind, cell.Kind);
            Assert.Equal(initial.ArrayName, cell.ArrayName);
            Assert.Equal(initial.Key, cell.Key);
            Assert.Equal(initial.Value.StoredKind, cell.Value.StoredKind);
            Assert.Same(initial.Value.StoredReference, cell.Value.StoredReference);
            Assert.Equal(BitConverter.DoubleToInt64Bits(initial.Value.StoredNumber),
                BitConverter.DoubleToInt64Bits(cell.Value.StoredNumber));
        }
        Assert.False(origin.IsScalar);
    }

    [Fact]
    public void ScalarCellsPreserveBitsBackingAndOriginsWithoutBoxing()
    {
        foreach (var bits in new long[] { 0, long.MinValue, 0x3ff4000000000000, 0x7ff0000000000000,
            unchecked((long)0xfff0000000000000UL), 0x7ff8000000000123, 0x7ff0000000000001 })
        {
            var cell = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(BitConverter.Int64BitsToDouble(bits)));
            Assert.True(cell.Value.TryGetNumber(out var number));
            Assert.Equal(bits, BitConverter.DoubleToInt64Bits(number));
            Assert.Equal(default, cell.ArrayName);
        }
        var text = AwkText.FromSourceLiteralString("0007");
        var origin = new AwkUntypedScalar();
        AwkValue[] values = [AwkValue.FromNumber(7), AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            AwkValue.FromNumericString(text), AwkValue.Unassigned(),
            AwkValue.FromPreparedNumericInput(text, 7),
            AwkValue.FromUntypedScalar(origin)];
        foreach (var value in values)
        {
            var actual = RoundTrip(value);
            Assert.Equal(value.StoredKind, actual.StoredKind);
            Assert.Equal(value.RetainedTextMemoryBytes, actual.RetainedTextMemoryBytes);
            if (value.StoredKind is AwkValueKind.DeferredDecimalText or AwkValueKind.NumericString)
                Assert.True(text.HasSameStorage(actual.ByteText));
            var initialized = AwkFunctionLocalSlot.MissingScalar();
            var prior = initialized;
            initialized.InitializeScalar(in value);
            Assert.Equal(value.StoredKind, initialized.Value.StoredKind);
            Assert.Same(value.StoredReference, initialized.Value.StoredReference);
            Assert.Equal(BitConverter.DoubleToInt64Bits(value.StoredNumber),
                BitConverter.DoubleToInt64Bits(initialized.Value.StoredNumber));
            Assert.Equal(value.RetainedTextMemoryBytes, initialized.Value.RetainedTextMemoryBytes);
            Assert.Equal(default, initialized.ArrayName);
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, prior.Kind);
            Assert.Equal(AwkValueKind.Unassigned, prior.Value.StoredKind);
        }
        var untouched = RoundTrip(values[^1]);
        Assert.True(untouched.TryGetUntypedScalar(out var captured));
        Assert.Same(origin, captured);
        for (var index = 0; index < 100; index++) RoundTrip(values[index % values.Length]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var last = default(AwkValue);
        for (var index = 0; index < 10_000; index++) last = RoundTrip(values[index % values.Length]);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(allocated, 0, 1024);
        Assert.Equal(values[9_999 % values.Length].StoredKind, last.StoredKind);

        static AwkValue RoundTrip(AwkValue value) => RoundTripStorage(value);

        static AwkValue RoundTripStorage(AwkValue value)
        {
            var cell = AwkFunctionLocalSlot.MissingScalar();
            cell.InitializeScalar(in value);
            return cell.Value;
        }
    }

    [Fact]
    public void KeyCellsPreservePackedWordsOwnedTextAndRoleWithoutBoxing()
    {
        AwkArrayKey[] keys = [default, AwkArrayKey.FromOrdinal(int.MinValue), AwkArrayKey.FromOrdinal(0),
            AwkArrayKey.FromOrdinal(int.MaxValue), new("0007"), new("a\0b"), new("abc😀"), new("é😀"),
            new("abc😀x"), new("long owned key spelling")];
        foreach (var key in keys)
        {
            var cell = AwkFunctionLocalSlot.PreparedKey(key);
            var actual = cell.Key;
            Assert.Equal(AwkFunctionLocalSlotKind.Key, cell.Kind);
            Assert.Equal(default, cell.ArrayName);
            Assert.Equal(AwkValueKind.Unassigned, cell.Value.StoredKind);
            Assert.False(cell.TryReadNumber(out _));
            Assert.Equal(key, actual);
            Assert.Equal(key.GetHashCode(), actual.GetHashCode());
            Assert.Equal(key.AsString(AwkRuntimeCounters.Disabled), actual.AsString(AwkRuntimeCounters.Disabled));
            Assert.Equal(key.RetainedTextMemoryBytes, actual.RetainedTextMemoryBytes);
            Assert.True(key.StoredText.HasSameStorage(actual.StoredText));
        }
        for (var index = 0; index < 100; index++) Copy(keys[index % keys.Length]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var last = default(AwkArrayKey);
        for (var index = 0; index < 10_000; index++) last = Copy(keys[index % keys.Length]);
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);
        Assert.Equal(keys[^1], last);

        static AwkArrayKey Copy(AwkArrayKey key) => AwkFunctionLocalSlot.PreparedKey(key).Key;
    }

    [Theory]
    [InlineData(1L)] [InlineData(9007199254740993L)] [InlineData(long.MaxValue)]
    public void AliasCellsKeepFullIdentityAndMutableOrigin(long frameId)
    {
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("local"), frameId);
        var array = new AwkArrayIdentity(new(AwkArrayKeyComparer.Instance));
        var alias = AwkFunctionLocalSlot.ArrayAlias(name, array);
        Assert.Equal(name, alias.ArrayName);
        Assert.True(alias.ArrayName.IsOwnedByFunctionFrame(frameId));
        Assert.True(alias.Value.TryGetArrayBinding(out var capturedArray));
        Assert.Same(array, capturedArray);
        var origin = new AwkUntypedScalar();
        var potential = AwkFunctionLocalSlot.PotentialArrayAlias(name, AwkValue.FromUntypedScalar(origin));
        Assert.Equal(name, potential.ArrayName);
        Assert.True(potential.Value.IsUntypedValue);
        origin.Promote();
        Assert.False(potential.Value.IsUntypedValue);
        Assert.True(potential.Value.TryGetUntypedScalar(out var capturedOrigin));
        Assert.Same(origin, capturedOrigin);
        Assert.Equal(name, potential.ArrayName);
        var unnamed = AwkFunctionLocalSlot.ArrayAlias(array);
        Assert.Equal(default, unnamed.ArrayName);
        Assert.True(unnamed.Value.TryGetArrayBinding(out var unnamedArray));
        Assert.Same(array, unnamedArray);
    }

    [Fact]
    public void ShortOwnedAsciiCopiesDoNotAllocateMandatoryMetadata()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var sample = "field001"u8.ToArray();
        for (var index = 0; index < 100; index++) Copy();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var last = AwkText.Empty;
        for (var index = 0; index < 10_000; index++) last = Copy();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsType<byte[]>(last.Backing);
        Assert.InRange(allocated, 0, 64L * 10_000);
        Assert.Equal(10_100, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);

        AwkText Copy() => AwkText.CopyTrustedBytes(sample, counters, AwkMemoryAccounting.Disabled);
    }

    [Fact]
    public void OwnedTextTransportKeepsBackingAndCompactValueLayoutsWithoutBoxing()
    {
        Assert.Equal(8, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkText>());
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkValue>());
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkVariableSlot>());
        Assert.Equal(40, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkFunctionLocalSlot>());
        Assert.Equal(16, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayKey>());
        Assert.Equal(40, System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayUpdateTarget>());

        var text = AwkText.FromSourceLiteralString("0012");
        var value = AwkValue.FromNumericString(text);
        for (var index = 0; index < 100; index++) value = RoundTrip(value);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++) value = RoundTrip(value);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(allocated, 0, 1024);
        Assert.True(text.HasSameStorage(value.ByteText));
        Assert.Equal(AwkRuntimeValueKind.NumericString, value.RuntimeKind);
        Assert.Equal(12, value.AsNumber());
        var counters = AwkRuntimeCounters.CreateEnabled();
        Assert.Equal("0012", text.AsString(counters));
        Assert.Same(text.PreparedString, value.ByteText.AsString(counters));
        Assert.Equal(1, counters.AwkTextAsStringDecodeMisses);

        static AwkValue RoundTrip(AwkValue value) => AwkVariableSlot.FromValue(value)
            .ToValue(AwkValue.FromNumericString, AwkValue.FromText);
    }

    [Fact]
    public void GlobalBindingScopeRetainsChargesPhysicalDepthAndNestedBindings()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForMemory = new CooperativeTestMemory(1 << 20, AwkRuntimeCounters.Disabled);
        var memory = memoryForMemory.Accounting;
        var stack = new AwkFunctionCallStack(2, memory, AwkNumberParsingMode.Decimal);
        var name = AwkVariableName.FromText("x");
        var slot = new AwkFunctionLocalSlotId(0);
        var layout = new AwkFunctionLocalLayout([name]);
        var initial = memory.CurrentCommittedBytes;
        stack.PushFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var retained = memory.CurrentCommittedBytes;
        Assert.True(retained > initial);
        using (stack.EnterGlobalBindings())
        {
            Assert.True(stack.IsInsideFunction);
            Assert.Equal(retained, memory.CurrentCommittedBytes);
            Assert.False(stack.TryReadBoundLocalValue(slot, out _));
            Assert.False(stack.TryResolveLocal(name, out _));
            Assert.False(stack.TryResolveLocal(slot, out _));
            Assert.False(stack.TryResolveLocalValue(name, out _));
            Assert.False(stack.TryResolveLocalValue(slot, out _));
            Assert.False(stack.TryResolveArrayArgument(name, out _));
            Assert.False(stack.TryResolveOrPromoteArrayName(name, out _));
            Assert.False(stack.TryGetArrayReadName(name, out _));
            Assert.False(stack.TryAssignLocal(name, AwkValue.FromNumber(99)));
            Assert.False(stack.TryAssignLocal(slot, AwkValue.FromNumber(99)));
            // Owned payload is refunded; numeric inline storage stays in the cached frame.
            stack.PushFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumericString("003"))]);
            var nestedBytes = memory.CurrentCommittedBytes;
            try
            {
                Assert.True(stack.TryResolveLocalValue(name, out var nested));
                Assert.Equal(3, nested.Value.AsNumber());
                Assert.Equal("003", nested.Value.ByteText.AsString());
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
            Assert.InRange(memory.CurrentCommittedBytes, retained, nestedBytes - 1);
        }
        Assert.True(stack.TryResolveLocalValue(name, out var restored));
        Assert.Equal(7, restored.Value.AsNumber());
        stack.PopFrame(static _ => { });
        Assert.True(memory.CurrentCommittedBytes > initial); // Cached frames remain funded.
        memoryForMemory.Dispose();
        Assert.Equal(0, memoryForMemory.Budget.Reserved);
    }

    [Fact]
    public void GlobalBindingScopeKeepsHiddenAliasesInPromotion()
    {
        var stack = CreateCallStack();
        var outer = AwkVariableName.FromText("outer");
        var inner = AwkVariableName.FromText("inner");
        var array = AwkArrayName.FromText("shared");
        stack.PushFrame(new([outer]), [AwkFunctionLocalSlot.PotentialArrayAlias(array)]);
        using (stack.EnterGlobalBindings())
        {
            stack.PushFrame(new([inner]), [AwkFunctionLocalSlot.PotentialArrayAlias(array)]);
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
        var stack = CreateCallStack();
        var name = AwkVariableName.FromText("x");
        stack.PushFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
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
    public void ScalarValueOrdinalsRespectCurrentKindsAndActiveLayout()
    {
        var stack = CreateCallStack();
        var first = new AwkFunctionLocalSlotId(0);
        Assert.False(stack.TryReadBoundLocalValue(first, out _));
        AwkVariableName[] names = [AwkVariableName.FromText("x"), AwkVariableName.FromText("y"),
            AwkVariableName.FromText("potential"), AwkVariableName.FromText("array"), AwkVariableName.FromText("missing")];
        var array = AwkArrayName.FromText("a");
        stack.PushFrame(new AwkFunctionLocalLayout(names), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumericString("007")),
            AwkFunctionLocalSlot.PotentialArrayAlias(array), AwkFunctionLocalSlot.ArrayAlias(array)]);
        try
        {
            Assert.True(stack.TryReadBoundLocalValue(first, out var number));
            Assert.True(number.TryGetNumber(out var numeric));
            Assert.Equal(7, numeric);
            Assert.True(stack.TryReadBoundLocalValue(new AwkFunctionLocalSlotId(1), out var input));
            Assert.Equal(AwkValueKind.NumericString, input.StoredKind);
            Assert.Equal("007", input.ByteText.AsString());
            foreach (var index in new[] { -1, 2, 3, 4, 5, int.MaxValue })
                Assert.False(stack.TryReadBoundLocalValue(new AwkFunctionLocalSlotId(index), out _));
            Assert.True(stack.TryResolveLocal(names[4], out var missing));
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, missing.Kind);
            Assert.True(stack.TryAssignLocal(new AwkFunctionLocalSlotId(4), AwkValue.FromNumber(9)));
            Assert.True(stack.TryReadBoundLocalValue(new AwkFunctionLocalSlotId(4), out var assigned));
            Assert.True(assigned.TryGetNumber(out var assignedNumber));
            Assert.Equal(9, assignedNumber);
            stack.PushFrame(new AwkFunctionLocalLayout([names[0]]), [AwkFunctionLocalSlot.ArrayAlias(array)]);
            try { Assert.False(stack.TryReadBoundLocalValue(first, out _)); }
            finally { stack.PopFrame(static _ => { }); }
            Assert.True(stack.TryReadBoundLocalValue(first, out var restored));
            Assert.True(restored.TryGetNumber(out var restoredNumber));
            Assert.Equal(7, restoredNumber);
        }
        finally { stack.PopFrame(static _ => { }); }
        Assert.False(stack.TryReadBoundLocalValue(first, out _));
    }

    [Fact]
    public void AwkFunctionCallStack_ReusesFrameAndSlotStorage()
    {
        var stack = CreateCallStack();
        var layout = new AwkFunctionLocalLayout([AwkVariableName.FromText("x")]);
        AwkFunctionLocalSlot[] arguments =
            [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("value"))];
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
        var stack = CreateCallStack();
        var localName = AwkVariableName.FromText("x");
        var layout = new AwkFunctionLocalLayout([localName]);
        var slotId = new AwkFunctionLocalSlotId(0);
        stack.PushFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);

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
        var stack = CreateCallStack();
        var names = new AwkVariableName[aliasCount];
        var arguments = new AwkFunctionLocalSlot[aliasCount];
        for (var index = 0; index < aliasCount; index++)
        {
            names[index] = AwkVariableName.FromText("parameter" + index);
            arguments[index] = AwkFunctionLocalSlot.PotentialArrayAlias(
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
        var stack = CreateCallStack();
        var outerName = AwkVariableName.FromText("outer");
        var innerName = AwkVariableName.FromText("inner");
        var sharedArrayName = AwkArrayName.FromText("shared");
        stack.PushFrame(
            new AwkFunctionLocalLayout([outerName]),
            [AwkFunctionLocalSlot.PotentialArrayAlias(sharedArrayName)]);

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
        var stack = CreateCallStack();
        var firstName = AwkVariableName.FromText("first");
        var secondName = AwkVariableName.FromText("second");
        var sharedArrayName = AwkArrayName.FromText("shared");
        var value = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        stack.PushFrame(
            new AwkFunctionLocalLayout([firstName, secondName]),
            [
                AwkFunctionLocalSlot.PotentialArrayAlias(sharedArrayName, value),
                AwkFunctionLocalSlot.PotentialArrayAlias(sharedArrayName, value)
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

    private static AwkFunctionCallStack CreateCallStack() =>
        new(
            maxFunctionCallDepth: 8,
            AwkMemoryAccounting.Disabled,
            AwkNumberParsingMode.Decimal);
}
