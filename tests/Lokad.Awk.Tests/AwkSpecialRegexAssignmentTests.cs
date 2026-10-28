using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lokad.Awk.Tests;

public sealed class AwkSpecialRegexAssignmentTests
{
    [Theory]
    [InlineData("FS")] [InlineData("FPAT")] [InlineData("RS")]
    public void FieldSettingOwnsItsTextWithoutRetainingTheOldRecord(string variable)
    {
        using var memory = State(AwkRuntimeCounters.CreateEnabled());
        var state = memory.State;
        state.InitializeProcInfo();
        var backing = AssignFromField(state, variable);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(IsAlive(backing));
        Assert.Equal(",", state.ResolveVariableValue(AwkVariableName.FromText(variable)).AsString());
        if (variable == "FS") Assert.Equal("b", state.GetFieldValue(2).AsString());
        if (variable == "FPAT") Assert.Equal(",", state.GetFieldValue(1).AsString());
        GC.KeepAlive(state);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<byte[]> AssignFromField(AwkRuntimeState state, string variable)
    {
        var record = AwkText.FromString(", " + new string('x', 16_384));
        Assert.True(MemoryMarshal.TryGetArray(record.AsMemory(), out var storage));
        var bytes = storage.Array ?? throw new InvalidOperationException("Expected flat record storage.");
        state.SetCurrentRecord(record);
        state.AssignVariableValue(AwkVariableName.FromText(variable), state.GetFieldValue(1));
        state.SetCurrentRecord(AwkText.FromString("a,b"));
        return new(bytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAlive(WeakReference<byte[]> backing) => backing.TryGetTarget(out _);

    public static IEnumerable<object[]> Assignments()
    {
        foreach (var variable in new[] { "FS", "FPAT", "RS" })
        foreach (var throughSymbolTable in new[] { false, true })
            yield return [variable, throughSymbolTable];
    }

    [Theory]
    [MemberData(nameof(Assignments))]
    public void BothPreparedVariantsAreReusedAcrossSameValueAndCaseChanges(string variable, bool throughSymbolTable)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
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

    private static CooperativeTestMemory State(AwkRuntimeCounters counters)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options),
            new AwkRuntimeResourceMonitor(limits, counters), AwkCompatibilityProfile.Default));
    }
}
