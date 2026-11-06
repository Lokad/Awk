using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSpecialVariablesTests
{
    [Fact]
    public void TextOnlySpecialReadsAvoidReclassificationAndNumbersKeepCurrentFormatting()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault()))));
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.SubscriptSeparator, AwkValue.FromNumericString("0007"));
        var created = counters.ValuesCreated;
        for (var i = 0; i < 128; i++)
        {
            Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.SubscriptSeparator).EqualsAscii("0007"));
            Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.ConversionFormat).EqualsAscii("%.6g"));
        }
        Assert.Equal(created, counters.ValuesCreated);
        Assert.True(counters.BoundSpecialScalarReads >= 256);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        state.AssignSpecialVariableValue(AwkSpecialVariableId.ConversionFormat, AwkValue.FromText("%.2f"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromNumber(1.25));
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).EqualsAscii("1.25"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.ConversionFormat, AwkValue.FromText("%.3f"));
        Assert.True(state.ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator).EqualsAscii("1.250"));
        Assert.True(state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator).IsNumberValue);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(nameof(AwkCompatibilityMode.GnuDefault))]
    [InlineData(nameof(AwkCompatibilityMode.Posix))]
    [InlineData(nameof(AwkCompatibilityMode.Traditional))]
    public void SpecialBindingsReferenceRealCellsAndRespectLazyAndDisabledNames(string modeName)
    {
        var mode = Enum.Parse<AwkCompatibilityMode>(modeName);
        using var memory = new CooperativeTestMemory(1 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, AwkValue.FromNumericString, text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal),
            static (_, _) => { }, mode: mode);
        Assert.Null(variables.GetSpecialScalarCell(AwkSpecialVariableId.RecordTerminator));
        var name = AwkSpecialVariables.VariableName(AwkSpecialVariableId.OutputFieldSeparator);
        variables.SetScalarValue(name, AwkValue.FromText("0007"));
        var cell = variables.GetScalarCell(variables.GetOrCreateScalarSlot(name));
        Assert.Same(cell, variables.GetSpecialScalarCell(AwkSpecialVariableId.OutputFieldSeparator));
        variables.CaptureScalarMemoryBaseline();
        variables.SetScalarValue(name, AwkValue.FromNumber(1.25));
        Assert.Same(cell, variables.GetSpecialScalarCell(AwkSpecialVariableId.OutputFieldSeparator));
        Assert.True(variables.GetScalarValue(cell).IsNumberValue);
        variables.SetScalarValue(AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator), AwkValue.FromText("\n"));
        Assert.Equal(mode == AwkCompatibilityMode.GnuDefault,
            variables.GetSpecialScalarCell(AwkSpecialVariableId.RecordTerminator) is not null);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ImplicitSeparatorsShareBoundCellsWithoutTextConversions(bool governed)
    {
        const int records = 128;
        const string source = "{a[$1,$2]+=$3;$2=\"changed\";print $0,$1}END{print a[\"left\",\"right\"]}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("left right 2\n", records)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(host, CancellationToken.None, options));
        Assert.Equal(string.Concat(Enumerable.Repeat("left changed 2 left\n", records)) + "256\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.BoundSpecialScalarReads >= records * 4);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public void AwkSpecialVariables_RegistersMetadata()
    {
        Assert.True(AwkSpecialVariables.TryGet("FS", out var fieldSeparator));
        var fieldSeparatorScalar = Assert.IsType<AwkSpecialScalarVariableDefinition>(fieldSeparator);
        Assert.Equal("FS", fieldSeparatorScalar.VariableName.Text);

        Assert.True(AwkSpecialVariables.TryGet("IGNORECASE", out var ignoreCase));
        var ignoreCaseScalar = Assert.IsType<AwkSpecialScalarVariableDefinition>(ignoreCase);
        Assert.True(ignoreCaseScalar.InitialValue.HasValue);
        Assert.True(ignoreCaseScalar.InitialValue.Value.IsNumberValue);
        Assert.Equal("0", ignoreCaseScalar.InitialValue.Value.AsString());

        Assert.True(AwkSpecialVariables.TryGet("FUNCTAB", out var functionTable));
        var functionTableArray = Assert.IsType<AwkSpecialArrayVariableDefinition>(functionTable);
        Assert.False(functionTableArray.CanModifyArrayElements);
        Assert.False(functionTableArray.CanReplaceArray);

        Assert.True(AwkSpecialVariables.TryGet("SYMTAB", out var symbolTable));
        var symbolTableArray = Assert.IsType<AwkSpecialArrayVariableDefinition>(symbolTable);
        Assert.True(symbolTableArray.CanModifyArrayElements);
        Assert.False(symbolTableArray.CanReplaceArray);
    }
}
