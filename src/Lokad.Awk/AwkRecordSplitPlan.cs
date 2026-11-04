namespace Lokad.Awk;

/// <summary>Immutable splitting configuration retained while a record's fields are lazy.</summary>
internal sealed record AwkRecordSplitPlan
{
    public static AwkRecordSplitPlan Empty { get; } = new(AwkFieldSplittingMode.FieldSeparator,
        default, AwkText.Empty, [], AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary);

    public AwkRecordSplitPlan(
        AwkFieldSplittingMode mode,
        AwkFieldSeparatorSpec separator,
        AwkText fieldPattern,
        IReadOnlyList<AwkFieldWidth> fieldWidths,
        AwkRegexCaseSensitivity caseSensitivity,
        AwkRecordFieldSplittingMode recordMode)
    {
        ArgumentNullException.ThrowIfNull(fieldPattern);
        ArgumentNullException.ThrowIfNull(fieldWidths);
        Mode = mode;
        Separator = separator;
        FieldPattern = fieldPattern;
        FieldWidths = fieldWidths;
        CaseSensitivity = caseSensitivity;
        RecordMode = recordMode;
    }

    public AwkFieldSplittingMode Mode { get; }
    public AwkFieldSeparatorSpec Separator { get; }
    public AwkText FieldPattern { get; }
    public IReadOnlyList<AwkFieldWidth> FieldWidths { get; }
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
}

internal enum AwkRecordFieldSplittingMode
{
    Ordinary,
    Paragraph
}
