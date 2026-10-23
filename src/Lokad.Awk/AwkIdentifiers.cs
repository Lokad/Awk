using System;

namespace Lokad.Awk;

internal readonly record struct AwkIdentifier
{
    private readonly string? _text;

    private AwkIdentifier(string text)
    {
        _text = text;
    }

    public string Text => _text ?? string.Empty;

    public static bool TryParse(string text, out AwkIdentifier identifier)
    {
        if (AwkIdentifiers.IsValidName(text))
        {
            identifier = new AwkIdentifier(text);
            return true;
        }

        identifier = default;
        return false;
    }

    public static AwkIdentifier FromTrustedName(string text)
    {
        if (TryParse(text, out var identifier))
            return identifier;

        throw new ArgumentException("Invalid awk identifier.", nameof(text));
    }

    public override string ToString() => Text;
}

internal readonly record struct AwkOptionalIdentifier
{
    private readonly AwkIdentifier _identifier;

    private AwkOptionalIdentifier(AwkIdentifier identifier)
    {
        HasValue = true;
        _identifier = identifier;
    }

    public static AwkOptionalIdentifier None => default;

    public static AwkOptionalIdentifier Some(AwkIdentifier identifier) => new(identifier);

    public static AwkOptionalIdentifier FromText(string text) =>
        AwkIdentifier.TryParse(text, out var identifier) ? new AwkOptionalIdentifier(identifier) : None;

    public bool HasValue { get; }

    public AwkIdentifier Value =>
        HasValue ? _identifier : throw new InvalidOperationException("No awk identifier is present.");

    public string Text => HasValue ? _identifier.Text : string.Empty;

    public override string ToString() => Text;
}

internal readonly record struct AwkVariableAssignment
{
    private readonly string? _value;
    private readonly string? _rawValue;

    private AwkVariableAssignment(AwkIdentifier identifier, string value, AwkVariableAssignmentSource source,
        string? rawValue = null)
    {
        Identifier = identifier;
        _value = value;
        _rawValue = rawValue ?? value;
        Source = source;
    }

    public AwkIdentifier Identifier { get; }

    public string Value => _value ?? string.Empty;
    public string RawValue => _rawValue ?? string.Empty;

    public AwkVariableAssignmentSource Source { get; }

    public static AwkVariableAssignment Option(AwkIdentifier identifier, string rawValue) =>
        new(identifier, rawValue, AwkVariableAssignmentSource.Option);

    public static AwkVariableAssignment FieldSeparatorOption(string rawValue) =>
        new(AwkIdentifier.FromTrustedName("FS"), rawValue, AwkVariableAssignmentSource.FieldSeparatorOption);

    public AwkVariableAssignment WithCompatibilityMode(AwkCompatibilityMode mode, Action? checkpoint) =>
        Source == AwkVariableAssignmentSource.InputArgument ? this :
        new(Identifier, Source == AwkVariableAssignmentSource.FieldSeparatorOption &&
            mode == AwkCompatibilityMode.Traditional && RawValue == "t" ? "\t" :
            AwkEscapes.DecodeSourceString(RawValue, mode, checkpoint), Source, RawValue);

    public static AwkVariableAssignment InputArgument(AwkIdentifier identifier, string rawValue) =>
        new(identifier, rawValue, AwkVariableAssignmentSource.InputArgument);

    public static bool TryParse(
        string assignment,
        AwkVariableAssignmentSource source,
        out AwkVariableAssignment parsed)
    {
        parsed = default;

        var equalsIndex = assignment.IndexOf('=', StringComparison.Ordinal);
        if (equalsIndex <= 0)
            return false;

        var name = assignment[..equalsIndex];
        if (!AwkIdentifier.TryParse(name, out var identifier))
            return false;

        var rawValue = assignment[(equalsIndex + 1)..];
        parsed = source == AwkVariableAssignmentSource.Option
            ? Option(identifier, rawValue)
            : InputArgument(identifier, rawValue);
        return true;
    }
}

internal enum AwkVariableAssignmentSource
{
    Option,
    InputArgument,
    FieldSeparatorOption
}

internal static class AwkIdentifiers
{
    public static bool IsValidName(string name) =>
        AwkNameFacts.IsValidVariableName(name);

    public static bool IsReservedName(string name) =>
        IsLanguageKeyword(name) || AwkBuiltins.IsKnownFunctionOrSpecialForm(name);

    public static bool IsLanguageKeyword(string name) =>
        AwkBuiltins.IsName(name, AwkBuiltinId.Getline) ||
        name is "BEGIN" or "END" or "BEGINFILE" or "ENDFILE" or
            "function" or "func" or "if" or "else" or "while" or "do" or
            "for" or "in" or "break" or "continue" or "next" or "nextfile" or
            "delete" or "exit" or "return" or "print" or "printf" or
            "switch" or "case" or "default";
}
