using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lokad.Awk;

internal static class AwkSpecialVariables
{
    private static readonly IReadOnlyDictionary<AwkSpecialVariableId, AwkSpecialVariableDefinition> DefinitionsById =
        new Dictionary<AwkSpecialVariableId, AwkSpecialVariableDefinition>
        {
            [AwkSpecialVariableId.ArgumentCount] = Scalar(AwkSpecialVariableId.ArgumentCount, "ARGC"),
            [AwkSpecialVariableId.ArgumentIndex] = Scalar(AwkSpecialVariableId.ArgumentIndex, "ARGIND"),
            [AwkSpecialVariableId.BinMode] = Scalar(AwkSpecialVariableId.BinMode, "BINMODE", AwkValue.FromText("0")),
            [AwkSpecialVariableId.ConversionFormat] = Scalar(AwkSpecialVariableId.ConversionFormat, "CONVFMT", AwkValue.FromText("%.6g")),
            [AwkSpecialVariableId.Errno] = Scalar(AwkSpecialVariableId.Errno, "ERRNO"),
            [AwkSpecialVariableId.FieldWidths] = Scalar(AwkSpecialVariableId.FieldWidths, "FIELDWIDTHS"),
            [AwkSpecialVariableId.FileName] = Scalar(AwkSpecialVariableId.FileName, "FILENAME"),
            [AwkSpecialVariableId.FileRecordNumber] = Scalar(AwkSpecialVariableId.FileRecordNumber, "FNR"),
            [AwkSpecialVariableId.FieldPattern] = Scalar(AwkSpecialVariableId.FieldPattern, "FPAT", AwkValue.FromText("[^ \t\n]+")),
            [AwkSpecialVariableId.FieldSeparator] = Scalar(AwkSpecialVariableId.FieldSeparator, "FS"),
            [AwkSpecialVariableId.IgnoreCase] = Scalar(AwkSpecialVariableId.IgnoreCase, "IGNORECASE", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.Lint] = Scalar(AwkSpecialVariableId.Lint, "LINT", AwkValue.FromText("0")),
            [AwkSpecialVariableId.FieldCount] = Scalar(AwkSpecialVariableId.FieldCount, "NF"),
            [AwkSpecialVariableId.RecordNumber] = Scalar(AwkSpecialVariableId.RecordNumber, "NR"),
            [AwkSpecialVariableId.OutputFormat] = Scalar(AwkSpecialVariableId.OutputFormat, "OFMT", AwkValue.FromText("%.6g")),
            [AwkSpecialVariableId.OutputFieldSeparator] = Scalar(AwkSpecialVariableId.OutputFieldSeparator, "OFS", AwkValue.FromText(" ")),
            [AwkSpecialVariableId.OutputRecordSeparator] = Scalar(AwkSpecialVariableId.OutputRecordSeparator, "ORS", AwkValue.FromText("\n")),
            [AwkSpecialVariableId.Precision] = Scalar(AwkSpecialVariableId.Precision, "PREC", AwkValue.FromText("53")),
            [AwkSpecialVariableId.RegexLength] = Scalar(AwkSpecialVariableId.RegexLength, "RLENGTH", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.RoundingMode] = Scalar(AwkSpecialVariableId.RoundingMode, "ROUNDMODE", AwkValue.FromText("N")),
            [AwkSpecialVariableId.RecordSeparator] = Scalar(AwkSpecialVariableId.RecordSeparator, "RS", AwkValue.FromText("\n")),
            [AwkSpecialVariableId.RegexStart] = Scalar(AwkSpecialVariableId.RegexStart, "RSTART", AwkValue.FromNumber(0)),
            [AwkSpecialVariableId.RecordTerminator] = Scalar(AwkSpecialVariableId.RecordTerminator, "RT"),
            [AwkSpecialVariableId.SubscriptSeparator] = Scalar(AwkSpecialVariableId.SubscriptSeparator, "SUBSEP", AwkValue.FromText("\u001c")),
            [AwkSpecialVariableId.TextDomain] = Scalar(AwkSpecialVariableId.TextDomain, "TEXTDOMAIN", AwkValue.FromText("messages")),
            [AwkSpecialVariableId.ArgumentVector] = Array(AwkSpecialVariableId.ArgumentVector, "ARGV", readOnly: false, nonReplaceable: false),
            [AwkSpecialVariableId.Environment] = Array(AwkSpecialVariableId.Environment, "ENVIRON", readOnly: false, nonReplaceable: false),
            [AwkSpecialVariableId.FunctionTable] = Array(AwkSpecialVariableId.FunctionTable, "FUNCTAB", readOnly: true, nonReplaceable: false),
            [AwkSpecialVariableId.ProcInfo] = Array(AwkSpecialVariableId.ProcInfo, "PROCINFO", readOnly: false, nonReplaceable: false),
            [AwkSpecialVariableId.SymbolTable] = Array(AwkSpecialVariableId.SymbolTable, "SYMTAB", readOnly: false, nonReplaceable: true)
        };

    private static readonly IReadOnlyDictionary<string, AwkSpecialVariableDefinition> DefinitionsByName =
        DefinitionsById.Values.ToDictionary(definition => definition.Name, StringComparer.Ordinal);

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

    private static AwkSpecialScalarVariableDefinition Scalar(AwkSpecialVariableId id, string name) =>
        new(id, name, AwkSpecialScalarInitialValue.None);

    private static AwkSpecialScalarVariableDefinition Scalar(AwkSpecialVariableId id, string name, AwkValue initialValue) =>
        new(id, name, AwkSpecialScalarInitialValue.Some(initialValue));

    private static AwkSpecialArrayVariableDefinition Array(
        AwkSpecialVariableId id,
        string name,
        bool readOnly,
        bool nonReplaceable)
    {
        var policy = (readOnly, nonReplaceable) switch
        {
            (false, false) => AwkSpecialArrayMutationPolicy.Mutable,
            (true, false) => AwkSpecialArrayMutationPolicy.ReadOnly,
            (false, true) => AwkSpecialArrayMutationPolicy.NonReplaceable,
            (true, true) => throw new ArgumentException("A special array cannot be both read-only and non-replaceable.")
        };

        return new AwkSpecialArrayVariableDefinition(id, name, policy);
    }
}
