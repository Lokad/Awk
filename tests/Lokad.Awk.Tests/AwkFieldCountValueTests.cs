using System.Threading;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldCountValueTests
{
    [Theory]
    [InlineData(1e30, "awk: memory limit exceeded\n")]
    [InlineData(-1.8, "awk: negative field index\n")]
    public void FailedCountAssignmentPreservesValueRecordAndMemory(double value, string diagnostic)
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("a b c"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, AwkValue.FromText("02.8"));
        Assert.Equal("a b", state.CurrentRecordByteText.AsString());
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() =>
            state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, AwkValue.FromNumber(value)));

        Assert.Equal(diagnostic, AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("02.8", state.ResolveSpecialVariableText(AwkSpecialVariableId.FieldCount).AsString());
        Assert.Equal("a b", state.CurrentRecordByteText.AsString());
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void FailedTextRetentionDoesNotTruncateTheRecord()
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("a b"));
        Assert.Equal(2, state.FieldCount);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => state.AssignSpecialVariableValue(
            AwkSpecialVariableId.FieldCount, AwkValue.FromText(new string('x', 1000))));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(2, state.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount).AsNumber());
        Assert.Equal("a b", state.CurrentRecordByteText.AsString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void RetainedFieldCountTextSurvivesRecompositionAndIsReleasedAtReset()
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        var assigned = AwkValue.FromText("foo");
        var fileName = AwkText.FromString("next");
        state.BeginInputFile(fileName, new AwkArgvIndex(1));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        state.SetCurrentRecord(AwkText.FromString("a b"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldCount, assigned);

        Assert.Equal("", state.CurrentRecordByteText.AsString());
        Assert.Equal(baseline + assigned.RetainedTextMemoryBytes, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal("foo", state.ResolveSpecialVariableText(AwkSpecialVariableId.FieldCount).AsString());
        state.BeginInputFile(fileName, new AwkArgvIndex(1));
        Assert.Equal(0, state.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount).AsNumber());
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void GenericNamesAndGlobalSlotsShareTheComputedFieldCount()
    {
        using var memory = State(AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        var name = AwkVariableName.FromText("NF");
        state.SetCurrentRecord(AwkText.FromString("a b c"));
        state.AssignVariableValue(name, AwkValue.FromText("01.8"));

        Assert.Equal("01.8", state.ResolveVariableValue(name).AsString());
        var slot = state.GetOrCreateGlobalScalarSlot(name);
        Assert.Equal("01.8", state.GetGlobalScalarValue(slot).AsString());
        Assert.Equal(1, state.FieldCount);
        Assert.Equal("a", state.CurrentRecordByteText.AsString());
        state.AssignVariableValue(slot, AwkValue.FromText("02.8"));
        Assert.Equal("02.8", state.ResolveSpecialVariableText(AwkSpecialVariableId.FieldCount).AsString());
        Assert.Equal(2, state.FieldCount);
        Assert.Equal("a ", state.CurrentRecordByteText.AsString());
    }

    private static CooperativeTestMemory State(AwkRuntimeLimits limits)
    {
        var memory = new CooperativeTestMemory(1 << 20);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memory.Configure(new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, limits,
                AwkRegexEngineKind.DotNetCompatibility))));
    }
}
