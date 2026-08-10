using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkCompatibilityProfile
{
    public AwkCompatibilityProfile(
        AwkCompatibilityMode mode,
        bool charactersAsBytes,
        bool nonDecimalData,
        AwkLintMode lintMode)
    {
        Mode = mode;
        CharactersAsBytes = charactersAsBytes;
        NonDecimalData = nonDecimalData;
        LintMode = lintMode;
    }

    public static AwkCompatibilityProfile Default =>
        new(
            mode: AwkCompatibilityMode.GnuDefault,
            charactersAsBytes: false,
            nonDecimalData: false,
            lintMode: AwkLintMode.Off);

    public AwkCompatibilityMode Mode { get; }

    public bool CharactersAsBytes { get; }

    public AwkStringUnitMode StringUnitMode =>
        CharactersAsBytes ? AwkStringUnitMode.Byte : AwkStringUnitMode.Utf8Rune;

    public bool NonDecimalData { get; }

    public AwkLintMode LintMode { get; }

    public AwkCompatibilityProfile WithMode(AwkCompatibilityMode mode) =>
        new(
            mode,
            CharactersAsBytes,
            NonDecimalData,
            LintMode);

    public AwkCompatibilityProfile WithCharactersAsBytes() =>
        new(Mode, true, NonDecimalData, LintMode);

    public AwkCompatibilityProfile WithNonDecimalData() =>
        new(Mode, CharactersAsBytes, true, LintMode);

    public AwkCompatibilityProfile WithLintMode(AwkLintMode lintMode) =>
        new(Mode, CharactersAsBytes, NonDecimalData, lintMode);
}

internal enum AwkStringUnitMode
{
    Utf8Rune,
    Byte
}

internal readonly record struct AwkAcceptedCompatibilityOptions(
    bool ReInterval,
    bool UseLcNumeric,
    bool Optimize,
    bool Sandbox)
{
    public static AwkAcceptedCompatibilityOptions Default { get; } =
        new(
            ReInterval: false,
            UseLcNumeric: false,
            Optimize: true,
            Sandbox: false);

    public AwkAcceptedCompatibilityOptions WithReInterval() =>
        this with { ReInterval = true };

    public AwkAcceptedCompatibilityOptions WithUseLcNumeric() =>
        this with { UseLcNumeric = true };

    public AwkAcceptedCompatibilityOptions WithOptimize(bool optimize) =>
        this with { Optimize = optimize };

    public AwkAcceptedCompatibilityOptions WithSandbox() =>
        this with { Sandbox = true };
}

internal static class AwkCompatibilityOptionFacts
{
    private static readonly AwkCompatibilityFieldAudit[] FieldAuditEntries =
    [
        new(
            AwkCompatibilityField.Mode,
            AwkCompatibilityFieldStatus.Enforced,
            "Compatibility mode gates GNU-only language features and numeric parsing."),
        new(
            AwkCompatibilityField.CharactersAsBytes,
            AwkCompatibilityFieldStatus.Enforced,
            "Byte mode changes string length/indexing and rejects unsupported byte-regex paths."),
        new(
            AwkCompatibilityField.ReInterval,
            AwkCompatibilityFieldStatus.AcceptedNoOp,
            "Interval regular expressions are always accepted; the flag is retained for gawk CLI compatibility."),
        new(
            AwkCompatibilityField.NonDecimalData,
            AwkCompatibilityFieldStatus.Enforced,
            "Non-decimal data changes runtime string-to-number parsing."),
        new(
            AwkCompatibilityField.UseLcNumeric,
            AwkCompatibilityFieldStatus.AcceptedNoOp,
            "Lokad awk uses invariant numeric parsing and formatting; locale numeric parsing is not implemented."),
        new(
            AwkCompatibilityField.LintMode,
            AwkCompatibilityFieldStatus.Enforced,
            "Off, normal, and fatal lint modes are enforced; compatibility aliases normalize to normal lint."),
        new(
            AwkCompatibilityField.Optimize,
            AwkCompatibilityFieldStatus.AcceptedNoOp,
            "There is no optimizer pass; scripts are lowered directly to the executable model."),
        new(
            AwkCompatibilityField.Sandbox,
            AwkCompatibilityFieldStatus.AcceptedNoOp,
            "Lokad host policy is enforced independently; the flag is retained for gawk CLI compatibility.")
    ];

    public static IReadOnlyList<AwkCompatibilityFieldAudit> FieldAudits => FieldAuditEntries;
}

internal enum AwkCompatibilityMode
{
    GnuDefault,
    Posix,
    Traditional
}

internal enum AwkLintMode
{
    Off,
    Normal,
    Fatal
}

internal readonly struct AwkCompatibilityFieldAudit
{
    public AwkCompatibilityFieldAudit(
        AwkCompatibilityField field,
        AwkCompatibilityFieldStatus status,
        string rationale)
    {
        Field = field;
        Status = status;
        Rationale = rationale;
    }

    public AwkCompatibilityField Field { get; }

    public AwkCompatibilityFieldStatus Status { get; }

    public string Rationale { get; }
}

internal enum AwkCompatibilityField
{
    Mode,
    CharactersAsBytes,
    ReInterval,
    NonDecimalData,
    UseLcNumeric,
    LintMode,
    Optimize,
    Sandbox
}

internal enum AwkCompatibilityFieldStatus
{
    Enforced,
    AcceptedNoOp
}
