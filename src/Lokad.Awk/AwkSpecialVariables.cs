using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lokad.Awk;

internal static class AwkSpecialVariables
{
    private static readonly IReadOnlyDictionary<AwkSpecialVariableId, AwkSpecialVariableDefinition> DefinitionsById;

    private static readonly Dictionary<string, AwkSpecialVariableDefinition> DefinitionsByName;

    static AwkSpecialVariables()
    {
        DefinitionsById =
        new Dictionary<AwkSpecialVariableId, AwkSpecialVariableDefinition>
        {
            [AwkSpecialVariableId.ArgumentCount] = Scalar(AwkSpecialVariableId.ArgumentCount, "ARGC"),
            [AwkSpecialVariableId.ArgumentIndex] = Scalar(AwkSpecialVariableId.ArgumentIndex, "ARGIND"),
            [AwkSpecialVariableId.BinMode] = ScalarWithInitialValue(AwkSpecialVariableId.BinMode, "BINMODE", AwkValue.FromText("0")),
            [AwkSpecialVariableId.ConversionFormat] = ScalarWithInitialValue(AwkSpecialVariableId.ConversionFormat, "CONVFMT", AwkValue.FromText("%.6g")),
            [AwkSpecialVariableId.Errno] = Scalar(AwkSpecialVariableId.Errno, "ERRNO"),
            [AwkSpecialVariableId.FieldWidths] = Scalar(AwkSpecialVariableId.FieldWidths, "FIELDWIDTHS"),
            [AwkSpecialVariableId.FileName] = Scalar(AwkSpecialVariableId.FileName, "FILENAME"),
            [AwkSpecialVariableId.FileRecordNumber] = ScalarWithInitialValue(AwkSpecialVariableId.FileRecordNumber, "FNR", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.FieldPattern] = ScalarWithInitialValue(AwkSpecialVariableId.FieldPattern, "FPAT", AwkValue.FromText("[^ \t\n]+")),
            [AwkSpecialVariableId.FieldSeparator] = Scalar(AwkSpecialVariableId.FieldSeparator, "FS"),
            [AwkSpecialVariableId.IgnoreCase] = ScalarWithInitialValue(AwkSpecialVariableId.IgnoreCase, "IGNORECASE", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.Lint] = ScalarWithInitialValue(AwkSpecialVariableId.Lint, "LINT", AwkValue.FromText("0")),
            [AwkSpecialVariableId.FieldCount] = ScalarWithInitialValue(AwkSpecialVariableId.FieldCount, "NF", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.RecordNumber] = ScalarWithInitialValue(AwkSpecialVariableId.RecordNumber, "NR", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.OutputFormat] = ScalarWithInitialValue(AwkSpecialVariableId.OutputFormat, "OFMT", AwkValue.FromText("%.6g")),
            [AwkSpecialVariableId.OutputFieldSeparator] = ScalarWithInitialValue(AwkSpecialVariableId.OutputFieldSeparator, "OFS", AwkValue.FromText(" ")),
            [AwkSpecialVariableId.OutputRecordSeparator] = ScalarWithInitialValue(AwkSpecialVariableId.OutputRecordSeparator, "ORS", AwkValue.FromText("\n")),
            [AwkSpecialVariableId.Precision] = ScalarWithInitialValue(AwkSpecialVariableId.Precision, "PREC", AwkValue.FromText("53")),
            [AwkSpecialVariableId.RegexLength] = ScalarWithInitialValue(AwkSpecialVariableId.RegexLength, "RLENGTH", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.RoundingMode] = ScalarWithInitialValue(AwkSpecialVariableId.RoundingMode, "ROUNDMODE", AwkValue.FromText("N")),
            [AwkSpecialVariableId.RecordSeparator] = ScalarWithInitialValue(AwkSpecialVariableId.RecordSeparator, "RS", AwkValue.FromText("\n")),
            [AwkSpecialVariableId.RegexStart] = ScalarWithInitialValue(AwkSpecialVariableId.RegexStart, "RSTART", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.RecordTerminator] = Scalar(AwkSpecialVariableId.RecordTerminator, "RT"),
            [AwkSpecialVariableId.SubscriptSeparator] = ScalarWithInitialValue(AwkSpecialVariableId.SubscriptSeparator, "SUBSEP", AwkValue.FromText("\u001c")),
            [AwkSpecialVariableId.TextDomain] = ScalarWithInitialValue(AwkSpecialVariableId.TextDomain, "TEXTDOMAIN", AwkValue.FromText("messages")),
            [AwkSpecialVariableId.ArgumentVector] = Array(AwkSpecialVariableId.ArgumentVector, "ARGV", AwkSpecialArrayMutationPolicy.Mutable),
            [AwkSpecialVariableId.Environment] = Array(AwkSpecialVariableId.Environment, "ENVIRON", AwkSpecialArrayMutationPolicy.Mutable),
            [AwkSpecialVariableId.FunctionTable] = Array(AwkSpecialVariableId.FunctionTable, "FUNCTAB", AwkSpecialArrayMutationPolicy.ReadOnly),
            [AwkSpecialVariableId.ProcInfo] = Array(AwkSpecialVariableId.ProcInfo, "PROCINFO", AwkSpecialArrayMutationPolicy.Mutable),
            [AwkSpecialVariableId.SymbolTable] = Array(AwkSpecialVariableId.SymbolTable, "SYMTAB", AwkSpecialArrayMutationPolicy.NonReplaceable)
        };

        DefinitionsByName = DefinitionsById.Values.ToDictionary(definition => definition.Name, StringComparer.Ordinal);

        static AwkSpecialScalarVariableDefinition Scalar(AwkSpecialVariableId id, string name) =>
            new(id, name, AwkSpecialScalarInitialValue.None);

        static AwkSpecialScalarVariableDefinition ScalarWithInitialValue(
            AwkSpecialVariableId id,
            string name,
            AwkValue initialValue) =>
            new(id, name, AwkSpecialScalarInitialValue.Some(initialValue));

        static AwkSpecialArrayVariableDefinition Array(
            AwkSpecialVariableId id,
            string name,
            AwkSpecialArrayMutationPolicy mutationPolicy) =>
            new(id, name, mutationPolicy);
    }

    public static IEnumerable<AwkSpecialVariableDefinition> All => DefinitionsById.Values;

    public static IEnumerable<AwkSpecialScalarVariableDefinition> Scalars =>
        DefinitionsById.Values.OfType<AwkSpecialScalarVariableDefinition>();

    public static IEnumerable<AwkSpecialArrayVariableDefinition> Arrays =>
        DefinitionsById.Values.OfType<AwkSpecialArrayVariableDefinition>();

    public static string Name(AwkSpecialVariableId id) =>
        DefinitionsById[id].Name;

    public static AwkVariableName VariableName(AwkSpecialVariableId id)
    {
        if (DefinitionsById[id] is AwkSpecialScalarVariableDefinition scalar)
            return scalar.VariableName;

        throw new ArgumentException("The special variable is not a scalar.", nameof(id));
    }

    public static AwkArrayName ArrayName(AwkSpecialVariableId id)
    {
        if (DefinitionsById[id] is AwkSpecialArrayVariableDefinition array)
            return array.ArrayName;

        throw new ArgumentException("The special variable is not an array.", nameof(id));
    }

    public static bool IsName(string name, AwkSpecialVariableId id) =>
        string.Equals(name, Name(id), StringComparison.Ordinal);

    public static bool TryGet(string name, [NotNullWhen(true)] out AwkSpecialVariableDefinition? definition) =>
        DefinitionsByName.TryGetValue(name, out definition);

    public static bool TryGetArray(string name, [NotNullWhen(true)] out AwkSpecialArrayVariableDefinition? definition)
    {
        if (TryGet(name, out var special) && special is AwkSpecialArrayVariableDefinition array)
        {
            definition = array;
            return true;
        }

        definition = default;
        return false;
    }

    public static bool TryGetScalar(string name, out AwkSpecialVariableId id)
    {
        if (TryGet(name, out var special) && special is AwkSpecialScalarVariableDefinition scalar)
        {
            id = scalar.Id;
            return true;
        }

        id = default;
        return false;
    }

    public static bool TryGetScalar(AwkVariableName name, out AwkSpecialVariableId id) =>
        TryGetScalar(name.Text, out id);

}
