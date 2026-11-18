using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

public sealed class AwkUntypedStateTests
{
    [Fact]
    public void UntypedOriginsPreserveValueLayoutAndScalarCopyIndependence()
    {
        Assert.Equal(24, Unsafe.SizeOf<AwkValue>());
        var origin = AwkValue.FromUntypedScalar(new AwkUntypedScalar());
        var argument = AwkFunctionLocalSlot.FromArgumentValue(origin);
        var copy = AwkValue.ForVariableStorage(origin, AwkNumberParsingMode.Decimal);

        Assert.True(origin.IsUntypedValue);
        Assert.True(argument.Value.IsUntypedValue);
        Assert.Equal(AwkValueKind.Unassigned, copy.StoredKind);
        Assert.True(origin.ForceScalar().IsUnassignedValue);
        Assert.False(argument.Value.IsUntypedValue);
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
        var before = state.MemoryAccounting.LiveBytes;
        var first = state.Arrays.GetAwkValue(name, key);

        var admitted = state.MemoryAccounting.LiveBytes;
        Assert.True(admitted > before);
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.Arrays.GetElementRuntimeKind(name, key));
        Assert.True(state.Arrays.GetAwkValue(name, key).IsUntypedValue);
        Assert.True(first.ByteText.AsSpan().IsEmpty);
        Assert.Equal(admitted, state.MemoryAccounting.LiveBytes);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
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
            using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.LiveBytes -
                AwkRuntimeMemory.EstimateUntypedArrayElementBytes(key) + 1);
            var before = state.MemoryAccounting.LiveBytes;
            var error = Assert.Throws<AwkRuntimeException>(() => state.Arrays.GetElementRuntimeKind(name, key));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(state.IsArray(name));
            Assert.False(state.IsArray(new AwkArrayName(source.Identifier)));
            Assert.False(state.HasScalar(source));
            Assert.False(state.HasScalar(parameter));
            Assert.False(state.TryGetExistingArrayCount(name, out _));
            Assert.Equal(before, state.MemoryAccounting.LiveBytes);
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
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.Arrays.GetElementRuntimeKind(name, first));
        var before = state.MemoryAccounting.LiveBytes;

        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.Arrays.GetElementRuntimeKind(name, second));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(AwkRuntimeValueKind.Untyped, state.Arrays.GetElementRuntimeKind(name, first));
        Assert.False(state.Arrays.ContainsValue(name, second));
        Assert.True(state.TryGetExistingArrayCount(name, out var count));
        Assert.Equal(1, count);
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void ParameterAssignmentFailurePreservesOriginAndFrameMemory()
    {
        const int limit = 1 << 20;
        using var memoryForState = State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var name = AwkArrayName.FromText("values");
        var key = AwkArrayKey.FromOrdinal(1);
        var origin = state.Arrays.GetAwkValue(name, key);
        var parameter = AwkVariableName.FromText("parameter");
        state.PushFunctionFrame(new AwkFunctionLocalLayout([parameter]), [AwkFunctionLocalSlot.FromArgumentValue(origin)]);
        try
        {
            using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.LiveBytes);
            var before = state.MemoryAccounting.LiveBytes;
            var error = Assert.Throws<AwkRuntimeException>(() => state.AssignVariable(parameter, new string('x', 256)));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(origin.IsUntypedValue);
            Assert.False(state.HasScalar(parameter));
            Assert.Equal(AwkRuntimeValueKind.Untyped, state.Arrays.GetElementRuntimeKind(name, key));
            Assert.Equal(before, state.MemoryAccounting.LiveBytes);
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
        using var pressure = state.MemoryAccounting.ReserveTransient(limit - state.MemoryAccounting.LiveBytes);
        var before = state.MemoryAccounting.LiveBytes;

        Assert.Throws<AwkRuntimeException>(() => state.TryResolveArrayArgument(name, out _));

        Assert.False(state.HasScalar(name));
        Assert.False(state.IsArray(new AwkArrayName(name.Identifier)));
        Assert.False(state.Arrays.ContainsValue(AwkArrayName.FromText("SYMTAB"), new AwkArrayKey("fresh")));
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
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
