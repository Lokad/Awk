namespace Lokad.Awk;

internal readonly record struct AwkVariableName(AwkIdentifier Identifier)
{
    public string Text => Identifier.Text;

    public static AwkVariableName FromText(string text) =>
        new(AwkIdentifier.FromTrustedName(text));

    public override string ToString() => Text;
}

internal readonly record struct AwkArrayName
{
    private readonly long _functionFrameId;

    public AwkArrayName(AwkIdentifier identifier)
    {
        Identifier = identifier;
        _functionFrameId = 0;
    }

    private AwkArrayName(AwkIdentifier identifier, long functionFrameId)
    {
        Identifier = identifier;
        _functionFrameId = functionFrameId;
    }

    public AwkIdentifier Identifier { get; }

    public string Text => Identifier.Text;

    public bool IsFunctionLocal => _functionFrameId != 0;

    public static AwkArrayName FromText(string text) =>
        new(AwkIdentifier.FromTrustedName(text));

    public static AwkArrayName FunctionLocal(AwkIdentifier identifier, long functionFrameId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(functionFrameId);
        return new AwkArrayName(identifier, functionFrameId);
    }

    public bool IsOwnedByFunctionFrame(long functionFrameId) =>
        _functionFrameId == functionFrameId;

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

    public override string ToString() => Text;
}
