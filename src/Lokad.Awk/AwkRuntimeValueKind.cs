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
    private static readonly AwkValue Unassigned = Name("unassigned");
    private static readonly AwkValue Number = Name("number");
    private static readonly AwkValue NumericString = Name("strnum");
    private static readonly AwkValue String = Name("string");
    private static readonly AwkValue Array = Name("array");
    private static readonly AwkValue Untyped = Name("untyped");

    private static AwkValue Name(string text) => AwkValue.FromText(AwkText.FromSourceLiteralString(text));

    internal static AwkValue Value(AwkRuntimeValueKind kind) =>
        kind switch
        {
            AwkRuntimeValueKind.Unassigned => Unassigned,
            AwkRuntimeValueKind.Number => Number,
            AwkRuntimeValueKind.NumericString => NumericString,
            AwkRuntimeValueKind.String => String,
            AwkRuntimeValueKind.Array => Array,
            AwkRuntimeValueKind.Untyped => Untyped,
            _ => throw new System.InvalidOperationException("Unknown awk runtime value kind.")
        };
}
