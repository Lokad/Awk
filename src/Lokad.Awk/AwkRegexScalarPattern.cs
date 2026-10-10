using System.Globalization;
using System.Text;

namespace Lokad.Awk;

/// <summary>Translates character atoms into expressions that consume complete Unicode scalars.</summary>
internal static class AwkRegexScalarPattern
{
    internal const string AnyScalar = @"(?:[^\uD800-\uDFFF]|[\uD800-\uDBFF][\uDC00-\uDFFF])";
    private const string BmpBoundary = @"(?![\uD800-\uDFFF])";
    private static readonly Lazy<CategoryRange[]> SupplementaryCategories = new(BuildCategoryRanges);

    public static string Translate(string pattern)
    {
        StringBuilder? builder = null;
        var copied = 0;
        for (var index = 0; index < pattern.Length; index++)
        {
            var start = index;
            string? replacement = null;
            var end = index + 1;
            switch (pattern[index])
            {
                case '\\':
                    if (++index < pattern.Length)
                    {
                        end = index + 1;
                        if (TryReadCategory(pattern, index, out var mask, out end))
                        {
                            replacement = CategoryAtom(pattern[(index - 1)..end], mask);
                        }
                        else if (TryShorthandCategory(pattern[index], out mask))
                        {
                            replacement = CategoryAtom(pattern[(index - 1)..end], mask);
                        }
                    }
                    break;
                case '.':
                    replacement = AnyScalar;
                    break;
                case '$':
                    // Unlike the backend's $, this assertion cannot match before a final LF.
                    replacement = @"\z";
                    break;
                case '[':
                    end = FindBracketEnd(pattern, index + 1);
                    if (end < 0)
                        return pattern;

                    end++;
                    replacement = TranslateBracket(pattern[(index + 1)..(end - 1)]);
                    break;
                case '(' when end < pattern.Length && pattern[end] == ')':
                    // Keep the observable empty capture while avoiding an empty-group
                    // construction defect in the UTF-8 adapter.
                    replacement = @"(\x00{0})";
                    end++;
                    break;
                default:
                    if (char.IsHighSurrogate(pattern[index]) &&
                        end < pattern.Length && char.IsLowSurrogate(pattern[end]))
                    {
                        end++;
                        replacement = "(?:" + pattern[index..end] + ")";
                    }
                    break;
            }

            if (replacement is not null)
            {
                builder ??= new StringBuilder(pattern.Length);
                builder.Append(pattern, copied, start - copied);
                builder.Append(replacement);
                copied = end;
            }

            index = end - 1;
        }

        if (builder is null)
            return pattern;

        builder.Append(pattern, copied, pattern.Length - copied);
        return builder.ToString();
    }

    private static string? TranslateBracket(string content)
    {
        // The adapter's value-match prefilter folds a one-letter ASCII class even
        // without IgnoreCase. Canonical literal atoms avoid that ambiguity while
        // retaining byte-native matching and the class's exact set of characters.
        if (content.Length > 0 && char.IsAsciiLetter(content[0]))
        {
            var lower = char.ToLowerInvariant(content[0]);
            var hasLower = false;
            var hasUpper = false;
            var singleLetter = true;
            foreach (var value in content)
            {
                if (!char.IsAsciiLetter(value) || char.ToLowerInvariant(value) != lower)
                {
                    singleLetter = false;
                    break;
                }
                hasLower |= value == lower;
                hasUpper |= value != lower;
            }
            if (singleLetter)
            {
                var upper = char.ToUpperInvariant(lower);
                return hasLower && hasUpper
                    ? "(?:" + lower + "|" + upper + ")"
                    : (hasLower ? lower : upper).ToString();
            }
        }

        var negated = content.StartsWith('^');
        var index = negated ? 1 : 0;
        var bmp = new StringBuilder(content.Length);
        var supplementary = new List<ScalarRange>();
        var requiresTranslation = negated;
        while (index < content.Length)
        {
            var start = index;
            var scalar = ReadAtom(content, ref index, out var categoryMask);
            if (scalar >= 0 && index + 1 < content.Length && content[index] == '-')
            {
                var rangeEnd = index + 1;
                var last = ReadAtom(content, ref rangeEnd, out _);
                if (last >= 0)
                {
                    if (last < scalar)
                        throw new ArgumentException("Invalid character range.");

                    AppendRange(scalar, last);
                    index = rangeEnd;
                    continue;
                }
            }

            if (scalar >= 0)
            {
                AppendRange(scalar, scalar);
            }
            else
            {
                bmp.Append(content, start, index - start);
                if (categoryMask != 0)
                {
                    requiresTranslation = true;
                    AddCategoryRanges(supplementary, categoryMask);
                }
            }
        }

        if (!requiresTranslation)
            return null;

        var positive = new StringBuilder();
        if (bmp.Length > 0)
            positive.Append(BmpBoundary).Append('[').Append(bmp).Append(']');
        AppendSupplementaryRanges(positive, supplementary);
        if (positive.Length == 0)
            positive.Append("(?!)");

        return negated
            ? "(?:(?!(?:" + positive + "))" + AnyScalar + ")"
            : "(?:" + positive + ")";

        void AppendRange(int first, int last)
        {
            if (first <= 0xFFFF)
            {
                AppendCodeUnitRange(bmp, first, Math.Min(last, 0xFFFF));
            }

            if (last >= 0x10000)
            {
                requiresTranslation = true;
                supplementary.Add(new ScalarRange(Math.Max(first, 0x10000), last));
            }
        }
    }

    private static int ReadAtom(string content, ref int index, out uint categoryMask)
    {
        categoryMask = 0;
        var value = content[index++];
        if (value != '\\')
        {
            if (char.IsHighSurrogate(value) && index < content.Length && char.IsLowSurrogate(content[index]))
                return char.ConvertToUtf32(value, content[index++]);

            return value;
        }

        if (index >= content.Length)
            return -1;

        var escaped = content[index++];
        if (TryReadCategory(content, index - 1, out categoryMask, out var end))
        {
            index = end;
            return -1;
        }

        if (TryShorthandCategory(escaped, out categoryMask))
            return -1;

        if (escaped is 'p' or 'P' && index < content.Length && content[index] == '{')
        {
            var closing = content.IndexOf('}', index + 1);
            if (closing >= 0)
                index = closing + 1;
            return -1;
        }

        // Escaped punctuation denotes an ordinary literal inside brackets.
        return char.IsAsciiLetterOrDigit(escaped) ? -1 : escaped;
    }

    private static int FindBracketEnd(string pattern, int index)
    {
        if (index < pattern.Length && pattern[index] == '^')
            index++;
        if (index < pattern.Length && pattern[index] == ']')
            index++;
        for (; index < pattern.Length; index++)
        {
            if (pattern[index] == '\\')
                index++;
            else if (pattern[index] == ']')
                return index;
        }

        return -1;
    }

    private static string CategoryAtom(string bmpPattern, uint categoryMask)
    {
        var supplementary = new List<ScalarRange>();
        AddCategoryRanges(supplementary, categoryMask);
        var atom = new StringBuilder();
        atom.Append(BmpBoundary).Append(bmpPattern);
        AppendSupplementaryRanges(atom, supplementary);
        return "(?:" + atom + ")";
    }

    private static bool TryReadCategory(string pattern, int index, out uint mask, out int end)
    {
        mask = 0;
        end = index + 1;
        if (pattern[index] is not ('p' or 'P') || end >= pattern.Length || pattern[end] != '{')
            return false;
        var closing = pattern.IndexOf('}', end + 1);
        if (closing < 0)
            return false;

        mask = CategoryMask(pattern.AsSpan(end + 1, closing - end - 1));
        if (mask == 0)
            return false;

        if (pattern[index] == 'P')
            mask = ~mask;
        end = closing + 1;
        return true;
    }

    private static bool TryShorthandCategory(char value, out uint mask)
    {
        mask = value switch
        {
            'w' or 'W' => CategoryMask("L") | Bit(UnicodeCategory.NonSpacingMark) |
                Bit(UnicodeCategory.DecimalDigitNumber) | Bit(UnicodeCategory.ConnectorPunctuation),
            'd' or 'D' => Bit(UnicodeCategory.DecimalDigitNumber),
            's' or 'S' => CategoryMask("Z"),
            _ => 0
        };
        if (value is 'W' or 'D' or 'S')
            mask = ~mask;
        return mask != 0;
    }

    private static uint CategoryMask(ReadOnlySpan<char> name) => name switch
    {
        "L" => Bit(UnicodeCategory.UppercaseLetter) | Bit(UnicodeCategory.LowercaseLetter) |
            Bit(UnicodeCategory.TitlecaseLetter) | Bit(UnicodeCategory.ModifierLetter) | Bit(UnicodeCategory.OtherLetter),
        "Lu" => Bit(UnicodeCategory.UppercaseLetter),
        "Ll" => Bit(UnicodeCategory.LowercaseLetter),
        "Lt" => Bit(UnicodeCategory.TitlecaseLetter),
        "Lm" => Bit(UnicodeCategory.ModifierLetter),
        "Lo" => Bit(UnicodeCategory.OtherLetter),
        "M" => Bit(UnicodeCategory.NonSpacingMark) | Bit(UnicodeCategory.SpacingCombiningMark) |
            Bit(UnicodeCategory.EnclosingMark),
        "Mn" => Bit(UnicodeCategory.NonSpacingMark),
        "Mc" => Bit(UnicodeCategory.SpacingCombiningMark),
        "Me" => Bit(UnicodeCategory.EnclosingMark),
        "N" => Bit(UnicodeCategory.DecimalDigitNumber) | Bit(UnicodeCategory.LetterNumber) |
            Bit(UnicodeCategory.OtherNumber),
        "Nd" => Bit(UnicodeCategory.DecimalDigitNumber),
        "Nl" => Bit(UnicodeCategory.LetterNumber),
        "No" => Bit(UnicodeCategory.OtherNumber),
        "P" => Bit(UnicodeCategory.ConnectorPunctuation) | Bit(UnicodeCategory.DashPunctuation) |
            Bit(UnicodeCategory.OpenPunctuation) | Bit(UnicodeCategory.ClosePunctuation) |
            Bit(UnicodeCategory.InitialQuotePunctuation) | Bit(UnicodeCategory.FinalQuotePunctuation) |
            Bit(UnicodeCategory.OtherPunctuation),
        "Pc" => Bit(UnicodeCategory.ConnectorPunctuation),
        "Pd" => Bit(UnicodeCategory.DashPunctuation),
        "Ps" => Bit(UnicodeCategory.OpenPunctuation),
        "Pe" => Bit(UnicodeCategory.ClosePunctuation),
        "Pi" => Bit(UnicodeCategory.InitialQuotePunctuation),
        "Pf" => Bit(UnicodeCategory.FinalQuotePunctuation),
        "Po" => Bit(UnicodeCategory.OtherPunctuation),
        "S" => Bit(UnicodeCategory.MathSymbol) | Bit(UnicodeCategory.CurrencySymbol) |
            Bit(UnicodeCategory.ModifierSymbol) | Bit(UnicodeCategory.OtherSymbol),
        "Sm" => Bit(UnicodeCategory.MathSymbol),
        "Sc" => Bit(UnicodeCategory.CurrencySymbol),
        "Sk" => Bit(UnicodeCategory.ModifierSymbol),
        "So" => Bit(UnicodeCategory.OtherSymbol),
        "Z" => Bit(UnicodeCategory.SpaceSeparator) | Bit(UnicodeCategory.LineSeparator) |
            Bit(UnicodeCategory.ParagraphSeparator),
        "Zs" => Bit(UnicodeCategory.SpaceSeparator),
        "Zl" => Bit(UnicodeCategory.LineSeparator),
        "Zp" => Bit(UnicodeCategory.ParagraphSeparator),
        "C" => Bit(UnicodeCategory.Control) | Bit(UnicodeCategory.Format) |
            Bit(UnicodeCategory.Surrogate) | Bit(UnicodeCategory.PrivateUse) | Bit(UnicodeCategory.OtherNotAssigned),
        "Cc" => Bit(UnicodeCategory.Control),
        "Cf" => Bit(UnicodeCategory.Format),
        "Cs" => Bit(UnicodeCategory.Surrogate),
        "Co" => Bit(UnicodeCategory.PrivateUse),
        "Cn" => Bit(UnicodeCategory.OtherNotAssigned),
        _ => 0
    };

    private static uint Bit(UnicodeCategory category) => 1U << (int)category;

    private static CategoryRange[] BuildCategoryRanges()
    {
        var ranges = new List<CategoryRange>();
        var first = 0x10000;
        var category = Rune.GetUnicodeCategory(new Rune(first));
        for (var scalar = first + 1; scalar <= 0x10FFFF; scalar++)
        {
            var next = Rune.GetUnicodeCategory(new Rune(scalar));
            if (next == category)
                continue;
            ranges.Add(new CategoryRange(first, scalar - 1, category));
            first = scalar;
            category = next;
        }

        ranges.Add(new CategoryRange(first, 0x10FFFF, category));
        return ranges.ToArray();
    }

    private static void AddCategoryRanges(List<ScalarRange> ranges, uint mask)
    {
        foreach (var range in SupplementaryCategories.Value)
        {
            if ((mask & Bit(range.Category)) != 0)
                ranges.Add(new ScalarRange(range.First, range.Last));
        }
    }

    private static void AppendSupplementaryRanges(StringBuilder builder, List<ScalarRange> ranges)
    {
        ranges.Sort(static (left, right) => left.First.CompareTo(right.First));
        for (var index = 0; index < ranges.Count; index++)
        {
            var first = ranges[index].First;
            var last = ranges[index].Last;
            while (index + 1 < ranges.Count && ranges[index + 1].First <= last + 1)
                last = Math.Max(last, ranges[++index].Last);

            var firstHigh = 0xD800 + ((first - 0x10000) >> 10);
            var lastHigh = 0xD800 + ((last - 0x10000) >> 10);
            var firstLow = 0xDC00 + (first & 0x3FF);
            var lastLow = 0xDC00 + (last & 0x3FF);
            if (firstHigh == lastHigh)
            {
                AppendPair(firstHigh, firstHigh, firstLow, lastLow);
                continue;
            }

            if (firstLow != 0xDC00)
            {
                AppendPair(firstHigh, firstHigh, firstLow, 0xDFFF);
                firstHigh++;
            }
            var completeLastHigh = lastLow == 0xDFFF ? lastHigh : lastHigh - 1;
            if (firstHigh <= completeLastHigh)
                AppendPair(firstHigh, completeLastHigh, 0xDC00, 0xDFFF);
            if (lastLow != 0xDFFF)
                AppendPair(lastHigh, lastHigh, 0xDC00, lastLow);
        }

        void AppendPair(int firstHigh, int lastHigh, int firstLow, int lastLow)
        {
            if (builder.Length > 0)
                builder.Append('|');
            AppendCodeUnitAtom(builder, firstHigh, lastHigh);
            AppendCodeUnitAtom(builder, firstLow, lastLow);
        }
    }

    private static void AppendCodeUnitAtom(StringBuilder builder, int first, int last)
    {
        if (first == last)
        {
            AppendCodeUnit(builder, first);
            return;
        }

        builder.Append('[');
        AppendCodeUnitRange(builder, first, last);
        builder.Append(']');
    }

    private static void AppendCodeUnitRange(StringBuilder builder, int first, int last)
    {
        AppendCodeUnit(builder, first);
        if (first != last)
        {
            builder.Append('-');
            AppendCodeUnit(builder, last);
        }
    }

    private static void AppendCodeUnit(StringBuilder builder, int value) =>
        builder.Append(@"\u").Append(value.ToString("X4", CultureInfo.InvariantCulture));

    private readonly record struct ScalarRange(int First, int Last);
    private readonly record struct CategoryRange(int First, int Last, UnicodeCategory Category);
}
