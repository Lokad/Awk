namespace Lokad.Awk;

internal readonly struct AwkCompatibilityProfile
{
    private readonly AwkStringUnitMode _requestedStringUnitMode;

    public AwkCompatibilityProfile(
        AwkCompatibilityMode mode,
        AwkStringUnitMode stringUnitMode,
        bool nonDecimalData,
        AwkLintMode lintMode)
    {
        Mode = mode;
        _requestedStringUnitMode = stringUnitMode;
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

    public AwkStringUnitMode StringUnitMode =>
        Mode == AwkCompatibilityMode.Posix ? AwkStringUnitMode.Utf8Rune : _requestedStringUnitMode;

    public bool OverridesCharactersAsBytes =>
        Mode == AwkCompatibilityMode.Posix && _requestedStringUnitMode == AwkStringUnitMode.Byte;

    public bool NonDecimalData { get; }

    public AwkLintMode LintMode { get; }

    public AwkCompatibilityProfile WithMode(AwkCompatibilityMode mode) =>
        new(
            mode,
            _requestedStringUnitMode,
            NonDecimalData,
            LintMode);

    public AwkCompatibilityProfile WithCharactersAsBytes() =>
        new(Mode, AwkStringUnitMode.Byte, NonDecimalData, LintMode);

    public AwkCompatibilityProfile WithNonDecimalData() =>
        new(Mode, _requestedStringUnitMode, true, LintMode);

    public AwkCompatibilityProfile WithLintMode(AwkLintMode lintMode) =>
        new(Mode, _requestedStringUnitMode, NonDecimalData, lintMode);
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
