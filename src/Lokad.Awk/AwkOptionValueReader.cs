
namespace Lokad.Awk;

internal static class AwkOptionValueReader
{
    public static bool TryReadShortAttachedOrRaw(
        CommandLineOptionParser parser,
        out string value)
    {
        if (parser.TryReadShortGroupRemainder(out value))
            return true;

        return parser.TryReadRaw(out value);
    }

    public static bool TryReadShortRaw(
        CommandLineOptionParser parser,
        out string value) =>
        parser.TryReadRaw(out value);

    public static bool TryReadOptionalShortAttachedOrRaw(
        CommandLineOptionParser parser,
        out string value)
    {
        if (TryReadShortAttachedOrRaw(parser, out value))
            return true;

        value = string.Empty;
        return false;
    }

    public static bool TryDiscardShortAttached(CommandLineOptionParser parser) =>
        parser.TryReadShortGroupRemainder(out _);

    public static bool TryReadLong(
        CommandLineOptionParser parser,
        CommandLineOptionItem.LongOption item,
        out string value)
    {
        if (item.Value.HasInlineValue)
        {
            value = item.Value.Text;
            return true;
        }

        return parser.TryReadRaw(out value);
    }

    public static bool TryReadW(
        CommandLineOptionParser parser,
        CommandLineOptionValue inlineValue,
        out string value)
    {
        if (inlineValue.HasInlineValue)
        {
            value = inlineValue.Text;
            return true;
        }

        return parser.TryReadRaw(out value);
    }
}
