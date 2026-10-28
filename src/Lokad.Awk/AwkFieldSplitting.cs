using System.Collections.Generic;

namespace Lokad.Awk;

internal enum AwkFieldSplittingMode
{
    FieldSeparator,
    FieldPattern,
    FieldWidths,
    Csv
}

internal readonly record struct AwkFieldSkip(int Value);

internal readonly record struct AwkFieldWidthLength(int Value);

internal readonly struct AwkFieldWidth(AwkFieldSkip skip, AwkFieldWidthLength width)
{
    public AwkFieldSkip Skip { get; } = skip;

    public AwkFieldWidthLength Width { get; } = width;
}

internal readonly struct AwkCapturedSeparator(int fieldIndex, AwkText? text)
{
    public int FieldIndex { get; } = fieldIndex;

    public AwkText Text => text ?? AwkText.Empty;
}

internal readonly struct AwkTextSplitResult(
    IReadOnlyList<AwkText>? fields,
    IReadOnlyList<AwkCapturedSeparator>? separators)
{
    public IReadOnlyList<AwkText> Fields => fields ?? [];

    public IReadOnlyList<AwkCapturedSeparator> Separators => separators ?? [];
}

internal enum AwkSeparatorCaptureMode
{
    Omit,
    Capture
}
