using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkUntypedStateTests
{
    [Fact]
    public void UntypedOriginsPreserveValueLayoutAndScalarCopyIndependence()
    {
        Assert.Equal(24, Unsafe.SizeOf<AwkValue>());
        Assert.Equal(24, Unsafe.SizeOf<AwkVariableSlot>());
        var origin = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        var argument = AwkVariableSlot.FromArgumentValue(origin);
        var copy = AwkVariableSlot.FromValue(origin);

        Assert.True(origin.IsUntypedValue);
        Assert.True(argument.IsUntyped);
        Assert.Equal(AwkVariableSlotKind.Unassigned, copy.Kind);
        Assert.True(origin.ForceScalar().IsUnassignedValue);
        Assert.False(argument.IsUntyped);
        Assert.True(origin.IsUnassignedValue);
        Assert.Equal(AwkRuntimeValueKind.String, default(AwkValue).RuntimeKind);
    }

    [Fact]
    public void MissingElementReadChargesOneCellAndStaysByteNative()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var key = AwkArrayKey.FromOrdinal(1);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var first = state.GetArrayAwkValue(name, key);

        var admitted = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(admitted > before);
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.GetArrayElementRuntimeKind(name, key));
        Assert.True(state.GetArrayAwkValue(name, key).IsUntypedValue);
        Assert.True(first.ByteText.AsSpan().IsEmpty);
        Assert.Equal(admitted, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingReadMemoryFailurePreservesArrayAndAliasTyping(bool alias)
    {
        const int limit = 1 << 20;
        using var memoryForState = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var source = AwkVariableName.FromText("source");
        var parameter = AwkVariableName.FromText("parameter");
        if (alias)
        {
            Assert.True(state.TryResolveArrayArgument(source, out var argument));
            state.PushFunctionFrame(new AwkFunctionLocalLayout([parameter]), [argument]);
        }
        var name = AwkArrayName.FromText(alias ? "parameter" : "source");
        var key = AwkArrayKey.FromOrdinal(1);
        try
        {
            using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.CurrentCommittedBytes -
                AwkRuntimeMemory.EstimateUntypedArrayElementBytes(key) + 1);
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayElementRuntimeKind(name, key));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(state.IsArray(name));
            Assert.False(state.IsArray(new AwkArrayName(source.Identifier)));
            Assert.False(state.HasScalar(source));
            Assert.False(state.HasScalar(parameter));
            Assert.False(state.TryGetExistingArrayCount(name, out _));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            if (alias)
                state.PopFunctionFrame();
        }
    }

    [Fact]
    public void MissingReadAdmissionRefusalPreservesExistingUntypedCell()
    {
        using var memoryForState = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var first = AwkArrayKey.FromOrdinal(1);
        var second = AwkArrayKey.FromOrdinal(2);
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.GetArrayElementRuntimeKind(name, first));
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayElementRuntimeKind(name, second));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.GetArrayElementRuntimeKind(name, first));
        Assert.False(state.ContainsArrayValue(name, second));
        Assert.True(state.TryGetExistingArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void ParameterAssignmentFailurePreservesOriginAndFrameMemory()
    {
        const int limit = 1 << 20;
        using var memoryForState = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var key = AwkArrayKey.FromOrdinal(1);
        var origin = state.GetArrayAwkValue(name, key);
        var parameter = AwkVariableName.FromText("parameter");
        state.PushFunctionFrame(new AwkFunctionLocalLayout([parameter]), [AwkFunctionArgument.FromValue(origin)]);
        try
        {
            using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.CurrentCommittedBytes);
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var error = Assert.Throws<AwkRuntimeException>(() => state.AssignVariable(parameter, new string('x', 256)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(origin.IsUntypedValue);
            Assert.False(state.HasScalar(parameter));
            Assert.Equal(AwkRuntimeValueKind.Untyped, state.GetArrayElementRuntimeKind(name, key));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            state.PopFunctionFrame();
        }
    }

    [Fact]
    public void UntouchedGlobalArgumentFailureDoesNotCreateSymbolMetadata()
    {
        const int limit = 1 << 20;
        using var memoryForState = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("fresh");
        using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.CurrentCommittedBytes);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        Assert.Throws<AwkRuntimeException>(() => state.TryResolveArrayArgument(name, out _));

        Assert.False(state.HasScalar(name));
        Assert.False(state.IsArray(new AwkArrayName(name.Identifier)));
        Assert.False(state.ContainsArrayValue(AwkArrayName.FromText("SYMTAB"), new AwkArrayKey("fresh")));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static CooperativeTestMemory State(AwkRuntimeCounters counters, AwkRuntimeLimits? limits = null)
    {
        var testMemory = new CooperativeTestMemory(1 << 20);
        return testMemory.Initialize(AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
                    testMemory.Configure(new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                        new AwkRandomGenerator(), counters,
                        limits ?? AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance))));
    }
}
