namespace Lokad.Awk.Tests;

public sealed class AwkFieldModeTests
{
    private static readonly AwkArrayName ProcInfo = AwkArrayName.FromText("PROCINFO");
    private static readonly AwkArrayKey ModeKey = new(AwkText.FromSourceLiteralString("FS"));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FieldModeInitializationHonorsProfileWithoutDecoding(int profile)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State((AwkCompatibilityMode)profile, counters);
        var state = memoryForState.State;
        state.InitializeProcInfo();
        Assert.Equal(profile == 0, state.TryPeekArrayAwkValue(ProcInfo, ModeKey, out var value));
        if (profile == 0)
        {
            Assert.True(value.ByteText.EqualsAscii("FS"));
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes > 0);
        }
        Assert.Equal(0, counters.StringDecodes);
    }

    [Theory]
    [InlineData("FIELDWIDTHS", "", false)]
    [InlineData("FIELDWIDTHS", "", true)]
    [InlineData("FIELDWIDTHS", "1 1", false)]
    [InlineData("FIELDWIDTHS", "1 1", true)]
    [InlineData("FPAT", ".", false)]
    [InlineData("FPAT", ".", true)]
    public void CombinedMemoryPreflightPreservesScalarMetadataAndFutureSplitting(
        string name, string text, bool throughSymbolTable)
    {
        using var memoryForMeasured = PreparedState();
        var measured = memoryForMeasured.State;
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(text));
        _ = measured.ResolveVariableValue(AwkVariableName.FromText(name));
        var before = measured.MemoryAccounting.CurrentCommittedBytes;
        Assign(measured, name, value, throughSymbolTable);
        var growth = measured.MemoryAccounting.CurrentCommittedBytes - before;
        Assert.True(growth > 0);

        using var memoryForState = PreparedState();
        var state = memoryForState.State;
        var oldValue = state.ResolveVariableValue(AwkVariableName.FromText(name));
        before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = memoryForState.Budget.Limit - before - growth + 1;
        state.MemoryAccounting.Reserve(pressure);
        var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, name, value, throughSymbolTable));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(oldValue.ByteText, state.ResolveVariableValue(AwkVariableName.FromText(name)).ByteText);
        Assert.True(state.GetArrayAwkValue(ProcInfo, ModeKey).ByteText.EqualsAscii("FS"));
        state.MemoryAccounting.ReleaseReserved(pressure);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("ab cd"));
        Assert.Equal(2, state.FieldCount);
    }

    [Theory]
    [InlineData("FIELDWIDTHS", "")]
    [InlineData("FPAT", ".")]
    [InlineData("RS", "")]
    public void MetadataAdmissionRefusalRejectsTransitionBeforeScalarOrSplitterMutation(string name, string text)
    {
        using var memoryForState = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        state.SetArrayAwkValue(ProcInfo, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var old = state.ResolveVariableValue(AwkVariableName.FromText(name));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, name,
            AwkValue.FromText(AwkText.FromSourceLiteralString(text)), false));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(old.ByteText, state.ResolveVariableValue(AwkVariableName.FromText(name)).ByteText);
        Assert.False(state.TryPeekArrayAwkValue(ProcInfo, ModeKey, out _));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("ab cd"));
        Assert.Equal(2, state.FieldCount);
    }

    [Theory]
    [InlineData("FIELDWIDTHS", "0", "awk: invalid FIELDWIDTHS value\n")]
    [InlineData("FIELDWIDTHS", "* 1", "awk: `*' must be the last designator in FIELDWIDTHS\n")]
    [InlineData("FPAT", "[", "awk: invalid regular expression\n")]
    [InlineData("FS", "[a", "awk: invalid regular expression\n")]
    public void InvalidSplittingAssignmentPreservesState(string name, string text, string diagnostic)
    {
        using var memoryForState = PreparedState();
        var state = memoryForState.State;
        var old = state.ResolveVariableValue(AwkVariableName.FromText(name));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, name,
            AwkValue.FromText(AwkText.FromSourceLiteralString(text)), false));
        Assert.Equal(diagnostic, AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(old.ByteText, state.ResolveVariableValue(AwkVariableName.FromText(name)).ByteText);
        Assert.True(state.GetArrayAwkValue(ProcInfo, ModeKey).ByteText.EqualsAscii("FS"));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("ab cd"));
        Assert.Equal(2, state.FieldCount);
    }

    [Fact]
    public void MetadataAndFixedWidthsStayByteNativeAndReleaseUnusedBufferCharge()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(AwkCompatibilityMode.GnuDefault, counters);
        var state = memoryForState.State;
        state.InitializeProcInfo();
        Assign(state, "FIELDWIDTHS", AwkValue.FromText(AwkText.FromSourceLiteralString("1 *")), false);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("abc"));
        Assert.Equal(2, state.FieldCount);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assign(state, "FS", AwkValue.FromText(AwkText.FromSourceLiteralString(" ")), false);
        Assert.True(state.MemoryAccounting.CurrentCommittedBytes < before);
        Assert.True(state.GetArrayAwkValue(ProcInfo, ModeKey).ByteText.EqualsAscii("FS"));
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void InitialMetadataCapacityFailurePreservesExistingArray()
    {
        using var memoryForState = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault());
        var state = memoryForState.State;
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(ProcInfo, key, AwkValue.FromNumber(7));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memoryForState.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        Assert.Throws<AwkRuntimeException>(state.InitializeProcInfo);
        Assert.Equal(7, state.GetArrayAwkValue(ProcInfo, key).AsNumber());
        Assert.False(state.TryPeekArrayAwkValue(ProcInfo, ModeKey, out _));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static void Assign(AwkRuntimeState state, string name, AwkValue value, bool throughSymbolTable)
    {
        if (throughSymbolTable)
            state.SetArrayAwkValue(AwkArrayName.FromText("SYMTAB"),
                new AwkArrayKey(AwkText.FromSourceLiteralString(name)), value);
        else
            state.AssignVariableValue(AwkVariableName.FromText(name), value);
    }

    private static CooperativeTestMemory PreparedState()
    {
        var memoryForState = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled);
        var state = memoryForState.State;
        state.InitializeProcInfo();
        Assign(state, "FPAT", AwkValue.FromText(AwkText.FromSourceLiteralString(".")), false);
        Assign(state, "FS", AwkValue.FromText(AwkText.FromSourceLiteralString(" ")), false);
        return memoryForState;
    }

    private static CooperativeTestMemory State(AwkCompatibilityMode mode, AwkRuntimeCounters counters,
        AwkRuntimeLimits? limits = null)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: (long)(1 << 20) != long.MaxValue);
        var resolvedLimits = (limits ?? AwkRuntimeLimits.CreateDefault());
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, resolvedLimits, AwkRegexEngineKind.DotNetCompatibility);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options),
            new AwkRuntimeResourceMonitor(resolvedLimits, counters), AwkCompatibilityProfile.Default.WithMode(mode)));
    }
}
