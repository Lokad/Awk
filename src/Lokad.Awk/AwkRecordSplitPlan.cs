namespace Lokad.Awk;

/// <summary>Immutable splitting configuration retained while a record's fields are lazy.</summary>
internal readonly record struct AwkRecordSplitPlan
{
    private readonly AwkText? _fieldPattern;
    private readonly IReadOnlyList<AwkFieldWidth>? _fieldWidths;

    public AwkRecordSplitPlan(
        AwkFieldSplittingMode mode,
        AwkFieldSeparatorSpec separator,
        AwkText fieldPattern,
        IReadOnlyList<AwkFieldWidth> fieldWidths,
        AwkRegexCaseSensitivity caseSensitivity,
        AwkRecordFieldSplittingMode recordMode)
    {
        Mode = mode;
        Separator = separator;
        _fieldPattern = fieldPattern;
        _fieldWidths = fieldWidths;
        CaseSensitivity = caseSensitivity;
        RecordMode = recordMode;
    }

    public AwkFieldSplittingMode Mode { get; }
    public AwkFieldSeparatorSpec Separator { get; }
    public AwkText FieldPattern => _fieldPattern ?? AwkText.Empty;
    public IReadOnlyList<AwkFieldWidth> FieldWidths => _fieldWidths ?? [];
    public AwkRegexCaseSensitivity CaseSensitivity { get; }
    public AwkRecordFieldSplittingMode RecordMode { get; }

    public long RetainedMemoryBytes => Mode switch
    {
        AwkFieldSplittingMode.FieldPattern => AwkRuntimeMemory.EstimateTextBytes(FieldPattern),
        AwkFieldSplittingMode.FieldWidths => AwkRuntimeMemory.EstimateFieldWidthListBytes(
            FieldWidths is List<AwkFieldWidth> list ? list.Capacity : FieldWidths.Count),
        _ => Separator.UsesWhitespaceSemantics ? 0 : AwkRuntimeMemory.EstimateTextBytes(Separator.ByteText)
    };
}

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
