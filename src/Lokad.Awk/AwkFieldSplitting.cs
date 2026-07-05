using System.Collections.Generic;
using System.Globalization;

namespace Lokad.Awk;

internal enum AwkFieldSplittingMode
{
    FieldSeparator,
    FieldPattern,
    FieldWidths
}

internal readonly record struct AwkFieldSkip(int Value)
{
    public static bool TryParse(string text, out AwkFieldSkip skip)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) &&
            value >= 0)
        {
            skip = new AwkFieldSkip(value);
            return true;
        }

        skip = default;
        return false;
    }
}

internal readonly record struct AwkFieldWidthLength(int Value)
{
    public static bool TryParse(string text, out AwkFieldWidthLength width)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) &&
            value >= 0)
        {
            width = new AwkFieldWidthLength(value);
            return true;
        }

        width = default;
        return false;
    }
}

internal readonly struct AwkFieldWidth(AwkFieldSkip skip, AwkFieldWidthLength width)
{
    public AwkFieldSkip Skip { get; } = skip;

    public AwkFieldWidthLength Width { get; } = width;
}

internal readonly struct AwkTextSplitResult(IReadOnlyList<AwkText> fields, IReadOnlyList<AwkText> separators)
{
    public IReadOnlyList<AwkText> Fields { get; } = fields;

    public IReadOnlyList<AwkText> Separators { get; } = separators;
}
