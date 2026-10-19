using System.Collections.Frozen;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkSpecialArrayReadTests
{
    [Theory]
    [InlineData("SYMTAB", false)]
    [InlineData("SYMTAB", true)]
    [InlineData("FUNCTAB", false)]
    [InlineData("FUNCTAB", true)]
    public void FailedReadsDoNotCreateElementsOrConsumeRetainedMemory(string table, bool peek)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters);
        state.InitializeFunctions(new Dictionary<AwkFunctionName, AwkUserFunction>());
        var name = AwkArrayName.FromText(table);
        var key = new AwkArrayKey("absent");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.True(state.TryGetExistingArrayCount(name, out var count));

        var error = Assert.Throws<AwkRuntimeException>(() =>
        {
            if (peek)
                state.TryPeekArrayAwkValue(name, key, out _);
            else
                state.GetArrayAwkValue(name, key);
        });

        Assert.Equal("awk: reference to uninitialized element `" + table + "[\"absent\"]' is not allowed\n",
            Encoding.UTF8.GetString(error.MessageBytes.Span));
        Assert.False(state.ContainsArrayValue(name, key));
        Assert.True(state.TryGetExistingArrayCount(name, out var afterCount));
        Assert.Equal(count, afterCount);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void DeclaredSymbolReadPreservesUntypedScalarAndMemory()
    {
        var state = State(AwkRuntimeCounters.Disabled);
        var global = AwkVariableName.FromText("untouched");
        state.InitializeGlobalNames(new[] { global }.ToFrozenSet());
        var table = AwkArrayName.FromText("SYMTAB");
        var key = new AwkArrayKey("untouched");
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        Assert.True(state.ContainsArrayValue(table, key));
        Assert.True(state.GetArrayAwkValue(table, key).IsUnassignedValue);
        Assert.False(state.HasScalar(global));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void GlobalInventoryExcludesParametersAndSurvivesScalarArrayUse()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "function f(local){local=1;global=2;a[1]=3}BEGIN{other=4}"));
        Assert.True(binding.Program.HasProgram);
        var program = binding.Program.Program;
        Assert.False(program.GlobalNames.Contains(AwkVariableName.FromText("local")));
        Assert.True(program.GlobalNames.Contains(AwkVariableName.FromText("global")));
        Assert.True(program.GlobalNames.Contains(AwkVariableName.FromText("a")));
        Assert.True(program.GlobalNames.Contains(AwkVariableName.FromText("other")));
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(),
                AwkRegexEngineKind.DotNetCompatibility));
}
