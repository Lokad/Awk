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
        var state = State((AwkCompatibilityMode)profile, counters);
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
        var measured = PreparedState();
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(text));
        _ = measured.ResolveVariableValue(AwkVariableName.FromText(name));
        var before = measured.MemoryAccounting.CurrentCommittedBytes;
        Assign(measured, name, value, throughSymbolTable);
        var growth = measured.MemoryAccounting.CurrentCommittedBytes - before;
        Assert.True(growth > 0);

        var state = PreparedState();
        var oldValue = state.ResolveVariableValue(AwkVariableName.FromText(name));
        before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = state.Limits.MaxExecutionMemoryBytes - before - growth + 1;
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
    public void FullMetadataArrayRejectsTransitionBeforeScalarOrSplitterMutation(string name, string text)
    {
        var state = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1));
        state.SetArrayAwkValue(ProcInfo, AwkArrayKey.FromOrdinal(1), AwkValue.FromNumber(7));
        var old = state.ResolveVariableValue(AwkVariableName.FromText(name));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, name,
            AwkValue.FromText(AwkText.FromSourceLiteralString(text)), false));
        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
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
        var state = PreparedState();
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
        var state = State(AwkCompatibilityMode.GnuDefault, counters);
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
        var state = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1));
        var key = AwkArrayKey.FromOrdinal(1);
        state.SetArrayAwkValue(ProcInfo, key, AwkValue.FromNumber(7));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Throws<AwkRuntimeException>(state.InitializeProcInfo);
        Assert.Equal(7, state.GetArrayAwkValue(ProcInfo, key).AsNumber());
        Assert.False(state.TryPeekArrayAwkValue(ProcInfo, ModeKey, out _));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void ModeMetadataHonorsValueByteLimitBeforeMutation()
    {
        var state = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxValueBytes(9));
        state.InitializeProcInfo();
        var old = state.ResolveSpecialVariableText(AwkSpecialVariableId.FieldWidths);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, "FIELDWIDTHS",
            AwkValue.FromText(AwkText.FromSourceLiteralString("1")), false));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(old, state.ResolveSpecialVariableText(AwkSpecialVariableId.FieldWidths));
        Assert.True(state.GetArrayAwkValue(ProcInfo, ModeKey).ByteText.EqualsAscii("FS"));
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

    private static AwkRuntimeState PreparedState()
    {
        var state = State(AwkCompatibilityMode.GnuDefault, AwkRuntimeCounters.Disabled);
        state.InitializeProcInfo();
        Assign(state, "FPAT", AwkValue.FromText(AwkText.FromSourceLiteralString(".")), false);
        Assign(state, "FS", AwkValue.FromText(AwkText.FromSourceLiteralString(" ")), false);
        return state;
    }

    private static AwkRuntimeState State(AwkCompatibilityMode mode, AwkRuntimeCounters counters,
        AwkRuntimeLimits? limits = null)
    {
        var resolvedLimits = (limits ?? AwkRuntimeLimits.CreateDefault()).WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, resolvedLimits, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(resolvedLimits, counters), AwkCompatibilityProfile.Default.WithMode(mode));
    }
}
