namespace Lokad.Awk;

internal static class AwkCompatibilityNames
{
    public static bool EnablesBuiltin(AwkBuiltinDefinition definition, AwkCompatibilityMode mode) =>
        definition.Compatibility == AwkBuiltinCompatibility.Posix || mode == AwkCompatibilityMode.GnuDefault;

    public static bool EnablesSpecial(AwkSpecialVariableId id, AwkCompatibilityMode mode) =>
        mode == AwkCompatibilityMode.GnuDefault || id is
            AwkSpecialVariableId.ArgumentCount or AwkSpecialVariableId.ArgumentVector or
            AwkSpecialVariableId.Environment or AwkSpecialVariableId.ConversionFormat or
            AwkSpecialVariableId.FileName or AwkSpecialVariableId.FileRecordNumber or
            AwkSpecialVariableId.FieldSeparator or AwkSpecialVariableId.FieldCount or
            AwkSpecialVariableId.RecordNumber or AwkSpecialVariableId.OutputFormat or
            AwkSpecialVariableId.OutputFieldSeparator or AwkSpecialVariableId.OutputRecordSeparator or
            AwkSpecialVariableId.RecordSeparator or AwkSpecialVariableId.RegexStart or
            AwkSpecialVariableId.RegexLength or AwkSpecialVariableId.SubscriptSeparator;

    public static bool IsGnuKeyword(string name) =>
        name is "BEGINFILE" or "ENDFILE" or "switch" or "case" or "default";

    public static bool IsReservedName(string name, AwkCompatibilityMode mode) =>
        IsKeyword(name, mode) ||
        (AwkBuiltins.TryGetCallable(name, out var definition) && EnablesBuiltin(definition, mode));

    public static bool IsKeyword(string name, AwkCompatibilityMode mode) =>
        AwkIdentifiers.IsLanguageKeyword(name) &&
        (mode == AwkCompatibilityMode.GnuDefault || !IsGnuKeyword(name)) &&
        (mode != AwkCompatibilityMode.Posix || name != "func");

    public static bool IsReservedParameter(string name, AwkCompatibilityMode mode) =>
        (!IsGnuKeyword(name) && IsKeyword(name, mode)) ||
        (AwkBuiltins.TryGetCallable(name, out var builtin) && builtin.Compatibility == AwkBuiltinCompatibility.Posix) ||
        (AwkSpecialVariables.TryGet(name, out var special) && EnablesSpecial(special.Id, mode) &&
            special.Id is not (AwkSpecialVariableId.FunctionTable or AwkSpecialVariableId.SymbolTable));

    public static bool IsReservedFunction(string name, AwkCompatibilityMode mode) =>
        IsReservedName(name, mode) ||
        (AwkSpecialVariables.TryGet(name, out var special) && EnablesSpecial(special.Id, mode));
}
