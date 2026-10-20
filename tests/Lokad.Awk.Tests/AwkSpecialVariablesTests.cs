using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSpecialVariablesTests
{
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
