namespace Lokad.Awk;

internal enum AwkRuleKind
{
    Main,
    Begin,
    End,
    BeginFile,
    EndFile
}

internal enum AwkDirectiveKind
{
    Include,
    Load,
    Namespace,
    Unknown
}

internal enum AwkStatementKind
{
    Unknown,
    Block,
    Expression,
    Print,
    Printf,
    If,
    While,
    DoWhile,
    For,
    Switch,
    Break,
    Continue,
    Next,
    NextFile,
    Delete,
    Getline,
    Exit,
    Return
}

internal enum AwkExpressionKind
{
    Unknown,
    Number,
    String,
    Regex,
    Variable,
    Field,
    ArrayElement,
    FunctionCall,
    Group,
    Unary,
    Postfix,
    Binary,
    Concatenation,
    ExpressionList,
    Conditional,
    Assignment
}
