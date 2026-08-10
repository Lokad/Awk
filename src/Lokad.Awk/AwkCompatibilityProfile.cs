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

// These flags are recorded for GNU command-line compatibility but deliberately do not alter execution.
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
