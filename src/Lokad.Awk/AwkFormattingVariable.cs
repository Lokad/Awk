using System;

namespace Lokad.Awk;

internal enum AwkFormattingVariable
{
    OutputFormat,
    ConversionFormat
}

internal static class AwkFormattingVariableFacts
{
    public static AwkSpecialVariableId SpecialVariableId(AwkFormattingVariable variable) =>
        variable switch
        {
            AwkFormattingVariable.OutputFormat => AwkSpecialVariableId.OutputFormat,
            AwkFormattingVariable.ConversionFormat => AwkSpecialVariableId.ConversionFormat,
            _ => throw new ArgumentOutOfRangeException(nameof(variable), variable, null)
        };

    public static string DefaultFormat(AwkFormattingVariable variable) =>
        variable switch
        {
            AwkFormattingVariable.OutputFormat => "%.6g",
            AwkFormattingVariable.ConversionFormat => "%.6g",
            _ => throw new ArgumentOutOfRangeException(nameof(variable), variable, null)
        };
}
