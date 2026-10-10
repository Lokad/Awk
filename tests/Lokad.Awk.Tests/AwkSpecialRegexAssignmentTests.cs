namespace Lokad.Awk.Tests;

public sealed class AwkSpecialRegexAssignmentTests
{
    public static IEnumerable<object[]> Assignments()
    {
        foreach (var variable in new[] { "FS", "FPAT", "RS" })
        foreach (var throughSymbolTable in new[] { false, true })
        foreach (var utf8 in new[] { false, true })
            yield return [variable, throughSymbolTable, utf8];
    }

    [Theory]
    [MemberData(nameof(Assignments))]
    public void SecondVariantAdmissionFailurePreservesAssignment(string variable, bool throughSymbolTable, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(utf8, counters, 1);
        state.InitializeProcInfo();
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("a b"));
        Assert.Equal(2, state.FieldCount);
        var name = AwkVariableName.FromText(variable);
        var previous = state.ResolveVariableValue(name).ByteText;
        var version = state.RecordSeparatorVersion;
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString("[ab]+"));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = Assert.Throws<AwkRuntimeException>(() => Assign(state, variable, value, throughSymbolTable));
            Assert.Equal("awk: regular expression compilation limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(previous, state.ResolveVariableValue(name).ByteText);
            Assert.Equal(version, state.RecordSeparatorVersion);
            Assert.Equal(2, state.FieldCount);
            Assert.True(state.GetArrayAwkValue(AwkArrayName.FromText("PROCINFO"),
                new AwkArrayKey(AwkText.FromSourceLiteralString("FS"))).ByteText.EqualsAscii("FS"));
            Assert.Equal(1, counters.RegexCompiles);
            Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        }
    }

    [Theory]
    [MemberData(nameof(Assignments))]
    public void BothPreparedVariantsAreReusedAcrossSameValueAndCaseChanges(string variable, bool throughSymbolTable, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(utf8, counters, 2);
        state.InitializeProcInfo();
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString("[ab]+"));
        Assign(state, variable, value, throughSymbolTable);
        Assert.Equal(2, counters.RegexCompiles);
        state.AssignVariableValue(AwkVariableName.FromText("IGNORECASE"), AwkValue.FromNumber(1));
        Assign(state, variable, value, throughSymbolTable);
        state.AssignVariableValue(AwkVariableName.FromText("IGNORECASE"), AwkValue.FromNumber(0));
        Assign(state, variable, value, throughSymbolTable);
        Assert.Equal(value.ByteText, state.ResolveVariableValue(AwkVariableName.FromText(variable)).ByteText);
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    private static void Assign(AwkRuntimeState state, string variable, AwkValue value, bool throughSymbolTable)
    {
        if (throughSymbolTable)
            state.SetArrayAwkValue(AwkArrayName.FromText("SYMTAB"),
                new AwkArrayKey(AwkText.FromSourceLiteralString(variable)), value);
        else
            state.AssignVariableValue(AwkVariableName.FromText(variable), value);
    }

    private static AwkRuntimeState State(bool utf8, AwkRuntimeCounters counters, int compilations)
    {
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxRegexCompilations(compilations)
            .WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, counters), AwkCompatibilityProfile.Default);
    }
}
