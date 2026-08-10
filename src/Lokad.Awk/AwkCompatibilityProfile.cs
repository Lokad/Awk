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
