namespace Lokad.Awk;

internal enum AwkControlFlowRequirement
{
    None,
    Breakable,
    Loop,
    Function,
    MainInput,
    MainOrBeginFileInput
}

internal static class AwkStatementKindFacts
{
    public static bool IsOptionalValue(AwkStatementKind kind) =>
        kind is AwkStatementKind.Return or AwkStatementKind.Exit;
}

internal static class AwkStatementNames
{
    public static bool TryGetKeyword(AwkStatementKind kind, out string keyword)
    {
        keyword = kind switch
        {
            AwkStatementKind.Break => "break",
            AwkStatementKind.Continue => "continue",
            AwkStatementKind.Next => "next",
            AwkStatementKind.NextFile => "nextfile",
            AwkStatementKind.Return => "return",
            AwkStatementKind.Exit => "exit",
            AwkStatementKind.If => "if",
            AwkStatementKind.While => "while",
            AwkStatementKind.DoWhile => "do-while",
            AwkStatementKind.Switch => "switch",
            AwkStatementKind.Delete => "delete",
            AwkStatementKind.Getline => AwkBuiltins.Name(AwkBuiltinId.Getline),
            AwkStatementKind.Print => "print",
            AwkStatementKind.Printf => "printf",
            _ => string.Empty
        };
        return keyword.Length > 0;
    }
}

internal static class AwkStatementValidationFacts
{
    public static bool IsArgumentless(AwkStatementKind kind) =>
        kind is AwkStatementKind.Break or
            AwkStatementKind.Continue or
            AwkStatementKind.Next or
            AwkStatementKind.NextFile;

    public static string ArgumentlessDiagnostic(AwkStatementKind kind) =>
        AwkStatementNames.TryGetKeyword(kind, out var keyword)
            ? "`" + keyword + "' does not take an argument."
            : string.Empty;

    public static bool TryGetMissingExpressionDiagnostic(AwkStatementKind kind, out string message)
    {
        message = kind switch
        {
            AwkStatementKind.If => "Expected if condition.",
            AwkStatementKind.While => "Expected while condition.",
            AwkStatementKind.DoWhile => "Expected do-while condition.",
            AwkStatementKind.Switch => "Expected switch expression.",
            _ => string.Empty
        };
        return message.Length > 0;
    }

    public static bool TryGetControlFlowRequirement(
        AwkStatementKind kind,
        out AwkControlFlowRequirement requirement,
        out string diagnostic)
    {
        requirement = kind switch
        {
            AwkStatementKind.Break => AwkControlFlowRequirement.Breakable,
            AwkStatementKind.Continue => AwkControlFlowRequirement.Loop,
            AwkStatementKind.Return => AwkControlFlowRequirement.Function,
            AwkStatementKind.Next => AwkControlFlowRequirement.MainInput,
            AwkStatementKind.NextFile => AwkControlFlowRequirement.MainOrBeginFileInput,
            _ => AwkControlFlowRequirement.None
        };
        diagnostic = kind switch
        {
            AwkStatementKind.Break => "Break used outside loop or switch",
            AwkStatementKind.Continue => "Continue used outside loop",
            AwkStatementKind.Return => "Return used outside function",
            AwkStatementKind.Next => "Next used outside input action",
            AwkStatementKind.NextFile => "Nextfile used outside input action",
            _ => string.Empty
        };
        return requirement != AwkControlFlowRequirement.None;
    }

    public static bool IsLoopBodyKind(AwkStatementKind kind) =>
        kind is AwkStatementKind.For or AwkStatementKind.While or AwkStatementKind.DoWhile;

    public static bool IsSwitchBodyKind(AwkStatementKind kind) =>
        kind == AwkStatementKind.Switch;
}
