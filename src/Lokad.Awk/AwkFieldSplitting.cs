using System.Collections.Generic;

namespace Lokad.Awk;

internal enum AwkFieldSplittingMode
{
    FieldSeparator,
    FieldPattern,
    FieldWidths
}

internal readonly record struct AwkFieldSkip(int Value);

internal readonly record struct AwkFieldWidthLength(int Value);

internal readonly struct AwkFieldWidth(AwkFieldSkip skip, AwkFieldWidthLength width)
{
    public AwkFieldSkip Skip { get; } = skip;

    public AwkFieldWidthLength Width { get; } = width;
}

internal readonly struct AwkTextSplitResult(
    IReadOnlyList<AwkText>? fields,
    IReadOnlyList<AwkText>? separators)
{
    public IReadOnlyList<AwkText> Fields => fields ?? [];

    public IReadOnlyList<AwkText> Separators => separators ?? [];
}
