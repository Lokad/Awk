using System;

namespace Lokad.Awk;

internal readonly struct AwkDiagnostic
{
    public AwkDiagnostic(AwkTextSpan span, string message)
        : this(span, message, AwkDiagnosticKind.Syntax)
    {
    }

    public AwkDiagnostic(AwkTextSpan span, string message, AwkDiagnosticKind kind)
        : this(span, message, kind, AwkDiagnosticFacts.GetId(kind))
    {
    }

    public AwkDiagnostic(AwkTextSpan span, string message, AwkDiagnosticId id)
        : this(span, message, AwkDiagnosticFacts.GetKind(id), id)
    {
    }

    private AwkDiagnostic(AwkTextSpan span, string message, AwkDiagnosticKind kind, AwkDiagnosticId id)
        : this(span, message, kind, id, null)
    {
    }

    private AwkDiagnostic(
        AwkTextSpan span,
        string message,
        AwkDiagnosticKind kind,
        AwkDiagnosticId id,
        AwkSourceLocation? sourceLocation)
    {
        Span = span;
        Message = message;
        Kind = kind;
        Id = id;
        Category = AwkDiagnosticFacts.GetCategory(id);
        SourceLocation = sourceLocation;
    }

    public AwkTextSpan Span { get; }

    public string Message { get; }

    public AwkDiagnosticKind Kind { get; }

    public AwkDiagnosticId Id { get; }

    public AwkDiagnosticCategory Category { get; }

    public AwkSourceLocation? SourceLocation { get; }

    public bool IsWarning => Category == AwkDiagnosticCategory.Warning;

    public AwkDiagnostic WithSourceLocation(AwkSourceLocation sourceLocation) =>
        new(Span, Message, Kind, Id, sourceLocation);
}

internal readonly struct AwkSourceLocation
{
    public AwkSourceLocation(string name, AwkLineNumber line, AwkColumnNumber column)
    {
        Name = name;
        LineNumber = line;
        ColumnNumber = column;
    }

    public string Name { get; }

    public AwkLineNumber LineNumber { get; }

    public int Line => LineNumber.Value;

    public AwkColumnNumber ColumnNumber { get; }

    public int Column => ColumnNumber.Value;
}

internal readonly record struct AwkLineNumber
{
    public AwkLineNumber(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Line numbers are one-based.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString();
}

internal readonly record struct AwkColumnNumber
{
    public AwkColumnNumber(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Column numbers are one-based.");

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString();
}

internal enum AwkDiagnosticKind
{
    Syntax,
    Semantic,
    InvalidRegex,
    StringEscapeWarning
}

internal enum AwkDiagnosticId
{
    Unknown,
    SourceLoadFailure,
    ParseSyntax,
    ParseInvalidRegex,
    BindInvalidProgram,
    RuntimeError,
    RuntimeResourceLimit,
    RuntimeInvalidRegex,
    RuntimeRegexTimeout,
    OptionError,
    OptionInvalid,
    OptionMissingScript,
    OptionMissingValue,
    OptionInvalidVariableAssignment,
    HostIoFailure,
    WarningStringEscape,
    WarningRuntime,
    UnsupportedPolicy
}

internal enum AwkDiagnosticCategory
{
    Unknown,
    Source,
    Parse,
    Bind,
    Runtime,
    Option,
    HostIo,
    Warning,
    UnsupportedPolicy
}

internal static class AwkDiagnosticFacts
{
    public static AwkDiagnosticId GetId(AwkDiagnosticKind kind) =>
        kind switch
        {
            AwkDiagnosticKind.Syntax => AwkDiagnosticId.ParseSyntax,
            AwkDiagnosticKind.Semantic => AwkDiagnosticId.BindInvalidProgram,
            AwkDiagnosticKind.InvalidRegex => AwkDiagnosticId.ParseInvalidRegex,
            AwkDiagnosticKind.StringEscapeWarning => AwkDiagnosticId.WarningStringEscape,
            _ => AwkDiagnosticId.Unknown
        };

    public static AwkDiagnosticKind GetKind(AwkDiagnosticId id) =>
        id switch
        {
            AwkDiagnosticId.BindInvalidProgram => AwkDiagnosticKind.Semantic,
            AwkDiagnosticId.ParseInvalidRegex => AwkDiagnosticKind.InvalidRegex,
            AwkDiagnosticId.WarningStringEscape or AwkDiagnosticId.WarningRuntime => AwkDiagnosticKind.StringEscapeWarning,
            _ => AwkDiagnosticKind.Syntax
        };

    public static AwkDiagnosticCategory GetCategory(AwkDiagnosticId id) =>
        id switch
        {
            AwkDiagnosticId.SourceLoadFailure => AwkDiagnosticCategory.Source,
            AwkDiagnosticId.ParseSyntax or
                AwkDiagnosticId.ParseInvalidRegex => AwkDiagnosticCategory.Parse,
            AwkDiagnosticId.BindInvalidProgram => AwkDiagnosticCategory.Bind,
            AwkDiagnosticId.RuntimeError or
                AwkDiagnosticId.RuntimeResourceLimit or
                AwkDiagnosticId.RuntimeInvalidRegex or
                AwkDiagnosticId.RuntimeRegexTimeout => AwkDiagnosticCategory.Runtime,
            AwkDiagnosticId.OptionError or
                AwkDiagnosticId.OptionInvalid or
                AwkDiagnosticId.OptionMissingScript or
                AwkDiagnosticId.OptionMissingValue or
                AwkDiagnosticId.OptionInvalidVariableAssignment => AwkDiagnosticCategory.Option,
            AwkDiagnosticId.HostIoFailure => AwkDiagnosticCategory.HostIo,
            AwkDiagnosticId.WarningStringEscape or
                AwkDiagnosticId.WarningRuntime => AwkDiagnosticCategory.Warning,
            AwkDiagnosticId.UnsupportedPolicy => AwkDiagnosticCategory.UnsupportedPolicy,
            _ => AwkDiagnosticCategory.Unknown
        };
}
