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

    internal AwkIncrementalFieldKind IncrementalKind
    {
        get
        {
            var kind = _configuration?.IncrementalKind ?? AwkIncrementalFieldKind.Whitespace;
            return kind == AwkIncrementalFieldKind.SingleByte && RecordMode != AwkRecordFieldSplittingMode.Ordinary
                ? AwkIncrementalFieldKind.None : kind;
        }
    }

    public bool SupportsIncrementalFields => IncrementalKind != AwkIncrementalFieldKind.None;

    public bool RequiresRegexSubject => _configuration?.RequiresRegexSubject ?? false;

    public long RetainedMemoryBytes => _configuration?.RetainedMemoryBytes ?? 0;

    internal sealed record Configuration
    {
        public Configuration(AwkFieldSplittingMode mode, AwkFieldSeparatorSpec separator,
            AwkText fieldPattern, IReadOnlyList<AwkFieldWidth> fieldWidths)
        {
            ArgumentNullException.ThrowIfNull(fieldWidths);
            Mode = mode;
            Separator = separator;
            FieldPattern = fieldPattern;
            FieldWidths = fieldWidths;
            var separates = mode == AwkFieldSplittingMode.FieldSeparator;
            var whitespace = separates && separator.UsesWhitespaceSemantics;
            var separatorLength = separates && !whitespace ? separator.ByteText.ByteLength : 0;
            IncrementalKind = whitespace ? AwkIncrementalFieldKind.Whitespace :
                separates && separatorLength == 1 ? AwkIncrementalFieldKind.SingleByte : AwkIncrementalFieldKind.None;
            RequiresRegexSubject = mode == AwkFieldSplittingMode.FieldPattern ||
                (separates && !whitespace && separatorLength > 1);
        }

        public AwkFieldSplittingMode Mode { get; }
        public AwkFieldSeparatorSpec Separator { get; }
        public AwkText FieldPattern { get; }
        public IReadOnlyList<AwkFieldWidth> FieldWidths { get; }
        public bool RequiresRegexSubject { get; }
        public AwkIncrementalFieldKind IncrementalKind { get; }

        public long RetainedMemoryBytes => Mode switch
        {
            AwkFieldSplittingMode.FieldPattern => AwkRuntimeMemory.EstimateTextBytes(FieldPattern),
            AwkFieldSplittingMode.FieldWidths => AwkRuntimeMemory.EstimateFieldWidthListBytes(
                FieldWidths is List<AwkFieldWidth> list ? list.Capacity : FieldWidths.Count),
            _ => Separator.UsesWhitespaceSemantics ? 0 : AwkRuntimeMemory.EstimateTextBytes(Separator.ByteText)
        };
    }
}

internal enum AwkIncrementalFieldKind : byte { Whitespace, SingleByte, None }

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
