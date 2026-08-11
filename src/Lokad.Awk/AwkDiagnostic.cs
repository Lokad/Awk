using System;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkDiagnostic
{
    private readonly string? _message;

    public AwkDiagnostic(AwkTextSpan span, string message)
        : this(span, message, AwkDiagnosticId.ParseSyntax)
    {
    }

    public AwkDiagnostic(AwkTextSpan span, string message, AwkDiagnosticId id)
        : this(span, message, id, null)
    {
    }

    private AwkDiagnostic(
        AwkTextSpan span,
        string message,
        AwkDiagnosticId id,
        AwkSourceLocation? sourceLocation)
    {
        Span = span;
        _message = message;
        Id = id;
        Category = AwkDiagnosticFacts.GetCategory(id);
        SourceLocation = sourceLocation;
    }

    public AwkTextSpan Span { get; }

    public string Message => _message ?? string.Empty;

    public AwkDiagnosticId Id { get; }

    public AwkDiagnosticCategory Category { get; }

    public AwkSourceLocation? SourceLocation { get; }

    public bool IsWarning => Category == AwkDiagnosticCategory.Warning;

    public AwkDiagnostic WithSourceLocation(AwkSourceLocation sourceLocation) =>
        new(Span, Message, Id, sourceLocation);
}

internal readonly struct AwkSourceLocation
{
    public AwkSourceLocation(AwkSourceName sourceName, AwkLineNumber line, AwkColumnNumber column)
    {
        SourceName = sourceName;
        LineNumber = line;
        ColumnNumber = column;
    }

    public AwkSourceName SourceName { get; }

    public AwkLineNumber LineNumber { get; }

    public AwkColumnNumber ColumnNumber { get; }
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

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
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

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
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
