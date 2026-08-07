namespace Lokad.Awk;

internal readonly record struct AwkVariableName(AwkIdentifier Identifier)
{
    public string Text => Identifier.Text;

    public static AwkVariableName FromText(string text) =>
        new(AwkIdentifier.FromTrustedName(text));

    public override string ToString() => Text;
}

internal readonly record struct AwkArrayName(AwkIdentifier Identifier)
{
    public string Text => Identifier.Text;

    public static AwkArrayName FromText(string text) =>
        new(AwkIdentifier.FromTrustedName(text));

    public override string ToString() => Text;
}

internal readonly record struct AwkFunctionName(AwkIdentifier Identifier)
{
    public string Text => Identifier.Text;

    public static AwkFunctionName FromText(string text) =>
        new(AwkIdentifier.FromTrustedName(text));

    public override string ToString() => Text;
}

internal readonly record struct AwkSpecialVariableName(AwkSpecialVariableId Id)
{
    public string Text => AwkSpecialVariables.Name(Id);

    public AwkVariableName VariableName => AwkSpecialVariables.VariableName(Id);

    public override string ToString() => Text;
}
