namespace Lokad.Awk;

internal readonly struct AwkCompatibilityProfile
{
    public AwkCompatibilityProfile(
        AwkCompatibilityMode mode,
        AwkStringUnitMode stringUnitMode,
        bool nonDecimalData,
        AwkLintMode lintMode)
    {
        Mode = mode;
        StringUnitMode = stringUnitMode;
        NonDecimalData = nonDecimalData;
        LintMode = lintMode;
    }

    public static AwkCompatibilityProfile Default =>
        new(
            mode: AwkCompatibilityMode.GnuDefault,
            stringUnitMode: AwkStringUnitMode.Utf8Rune,
            nonDecimalData: false,
            lintMode: AwkLintMode.Off);

    public AwkCompatibilityMode Mode { get; }

    public AwkStringUnitMode StringUnitMode { get; }

    public bool NonDecimalData { get; }

    public AwkLintMode LintMode { get; }

    public AwkCompatibilityProfile WithMode(AwkCompatibilityMode mode) =>
        new(
            mode,
            StringUnitMode,
            NonDecimalData,
            LintMode);

    public AwkCompatibilityProfile WithCharactersAsBytes() =>
        new(Mode, AwkStringUnitMode.Byte, NonDecimalData, LintMode);

    public AwkCompatibilityProfile WithNonDecimalData() =>
        new(Mode, StringUnitMode, true, LintMode);

    public AwkCompatibilityProfile WithLintMode(AwkLintMode lintMode) =>
        new(Mode, StringUnitMode, NonDecimalData, lintMode);
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
