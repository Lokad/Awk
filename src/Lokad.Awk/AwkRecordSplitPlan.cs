namespace Lokad.Awk;

/// <summary>Immutable splitting configuration retained while a record's fields are lazy.</summary>
internal readonly record struct AwkRecordSplitPlan
{
    private readonly Configuration? _configuration;

    public AwkRecordSplitPlan(
        AwkFieldSplittingMode mode,
        AwkFieldSeparatorSpec separator,
        AwkText fieldPattern,
        IReadOnlyList<AwkFieldWidth> fieldWidths,
        AwkRegexCaseSensitivity caseSensitivity,
        AwkRecordFieldSplittingMode recordMode)
        : this(new Configuration(mode, separator, fieldPattern, fieldWidths), caseSensitivity, recordMode)
    {
    }

    internal AwkRecordSplitPlan(Configuration configuration, AwkRegexCaseSensitivity caseSensitivity,
        AwkRecordFieldSplittingMode recordMode)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
        CaseSensitivity = caseSensitivity;
        RecordMode = recordMode;
    }

    // A default descriptor is the empty whitespace plan. Configuration is shared
    // across the four mode variants and remains immutable while records retain it.
    public AwkFieldSplittingMode Mode => _configuration?.Mode ?? default;
    public AwkFieldSeparatorSpec Separator => _configuration?.Separator ?? default;
    public AwkText FieldPattern => _configuration?.FieldPattern ?? AwkText.Empty;
    public IReadOnlyList<AwkFieldWidth> FieldWidths => _configuration?.FieldWidths ?? [];
    public AwkRegexCaseSensitivity CaseSensitivity { get; }
    public AwkRecordFieldSplittingMode RecordMode { get; }

    public bool SupportsIncrementalFields =>
        Mode == AwkFieldSplittingMode.FieldSeparator &&
        (Separator.UsesWhitespaceSemantics ||
         (RecordMode == AwkRecordFieldSplittingMode.Ordinary && Separator.ByteText.ByteLength == 1));

    public long RetainedMemoryBytes => Mode switch
    {
        AwkFieldSplittingMode.FieldPattern => AwkRuntimeMemory.EstimateTextBytes(FieldPattern),
        AwkFieldSplittingMode.FieldWidths => AwkRuntimeMemory.EstimateFieldWidthListBytes(
            FieldWidths is List<AwkFieldWidth> list ? list.Capacity : FieldWidths.Count),
        _ => Separator.UsesWhitespaceSemantics ? 0 : AwkRuntimeMemory.EstimateTextBytes(Separator.ByteText)
    };

    internal sealed record Configuration
    {
        public Configuration(AwkFieldSplittingMode mode, AwkFieldSeparatorSpec separator,
            AwkText fieldPattern, IReadOnlyList<AwkFieldWidth> fieldWidths)
        {
            ArgumentNullException.ThrowIfNull(fieldPattern);
            ArgumentNullException.ThrowIfNull(fieldWidths);
            Mode = mode;
            Separator = separator;
            FieldPattern = fieldPattern;
            FieldWidths = fieldWidths;
        }

        public AwkFieldSplittingMode Mode { get; }
        public AwkFieldSeparatorSpec Separator { get; }
        public AwkText FieldPattern { get; }
        public IReadOnlyList<AwkFieldWidth> FieldWidths { get; }
    }
}

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
