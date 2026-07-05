namespace Lokad.Awk;

internal enum AwkRuntimeValueKind
{
    Unassigned,
    Number,
    NumericString,
    String,
    Array,
    Untyped
}

internal static class AwkRuntimeValueKindNames
{
    public static string Format(AwkRuntimeValueKind kind) =>
        kind switch
        {
            AwkRuntimeValueKind.Unassigned => "unassigned",
            AwkRuntimeValueKind.Number => "number",
            AwkRuntimeValueKind.NumericString => "strnum",
            AwkRuntimeValueKind.String => "string",
            AwkRuntimeValueKind.Array => "array",
            AwkRuntimeValueKind.Untyped => "untyped",
            _ => throw new System.InvalidOperationException("Unknown awk runtime value kind.")
        };
}
