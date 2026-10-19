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
    public void BothPreparedVariantsAreReusedAcrossSameValueAndCaseChanges(string variable, bool throughSymbolTable, bool utf8)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(utf8, counters);
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
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static void Assign(AwkRuntimeState state, string variable, AwkValue value, bool throughSymbolTable)
    {
        if (throughSymbolTable)
            state.SetArrayAwkValue(AwkArrayName.FromText("SYMTAB"),
                new AwkArrayKey(AwkText.FromSourceLiteralString(variable)), value);
        else
            state.AssignVariableValue(AwkVariableName.FromText(variable), value);
    }

    private static AwkRuntimeState State(bool utf8, AwkRuntimeCounters counters)
    {
        var limits = AwkRuntimeLimits.CreateDefault()
            .WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits,
            utf8 ? AwkRegexEngineKind.Utf8Adapter : AwkRegexEngineKind.DotNetCompatibility,
regexEngine: AwkUtf8RegexEngine.Instance);
        return new(AwkFieldSeparatorSpec.Default, CancellationToken.None, options,
            new AwkRuntimeResourceMonitor(limits, counters), AwkCompatibilityProfile.Default);
    }
}
