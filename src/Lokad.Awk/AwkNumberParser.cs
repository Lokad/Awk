using System.Buffers.Text;
using System.Numerics;

namespace Lokad.Awk;

internal enum AwkNumberParseStatus
{
    NoNumber,
    Number,
    MissingHexExponent
}

internal readonly record struct AwkNumberParseResult(AwkNumberParseStatus Status, double Number, int End)
{
    public void ThrowIfError()
    {
        if (Status == AwkNumberParseStatus.MissingHexExponent)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
    }
}

internal static class AwkNumberParser
{
    public static int SkipWhitespace(ReadOnlySpan<byte> text, int index)
    {
        while (index < text.Length && text[index] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' or (byte)'\v' or (byte)'\f')
            index++;
        return index;
    }

    public static AwkNumberParseResult Parse(ReadOnlySpan<byte> text, AwkNumberParsingMode mode)
    {
        var start = SkipWhitespace(text, 0);
        if (TryParseSpecial(text, start, mode, out var special))
            return special;

        var digits = start;
        if (digits < text.Length && text[digits] is (byte)'+' or (byte)'-')
            digits++;
        if (IsHexPrefix(text, digits))
        {
            if (mode == AwkNumberParsingMode.Decimal)
                return default;
            if (mode == AwkNumberParsingMode.NonDecimalData && digits == start)
                return ParseNonDecimal(text, start, 16);
            return ParseHexFloat(text, start);
        }
        if (mode == AwkNumberParsingMode.NonDecimalData && GetBase(text[start..]) == 8)
            return ParseNonDecimal(text, start, 8);
        return ParseDecimal(text, start);
    }

    // strtonum chooses a base from the original bytes, before sign/whitespace trimming.
    public static int GetBase(ReadOnlySpan<byte> text)
    {
        if (text.Length < 2 || text[0] != (byte)'0')
            return 10;
        if (text[1] is (byte)'x' or (byte)'X')
            return 16;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is (byte)'.' or (byte)'e' or (byte)'E')
                return 10;
            if (!IsDigit(text[index]))
                break;
        }
        return text[1] is >= (byte)'0' and <= (byte)'7' ? 8 : 10;
    }

    public static AwkNumberParseResult ParseNonDecimal(ReadOnlySpan<byte> text, int start, int numberBase)
    {
        var index = start + (numberBase == 16 ? 2 : 0);
        if (numberBase == 16 && index == text.Length)
            return new(AwkNumberParseStatus.NoNumber, 0, start);
        double value = 0;
        while (index < text.Length)
        {
            var digit = HexValue(text[index]);
            if (numberBase == 16 && text[index] is (byte)'.' or (byte)'p' or (byte)'P')
            {
                // GNU requires a p/P somewhere in the remaining C string, even for an incomplete exponent.
                if (!ContainsExponentMarker(text[start..]))
                    return new(AwkNumberParseStatus.MissingHexExponent, 0, start);
                return ParseHexFloat(text, start);
            }
            if (digit < 0 || digit >= numberBase)
            {
                if (numberBase == 8 && text[index] is (byte)'8' or (byte)'9')
                    return ParseDecimal(text, start);
                break;
            }
            // GNU nondec2awknum rounds each multiply/add, rather than through a signed integer carrier.
            value = value * numberBase + digit;
            index++;
        }
        return new(AwkNumberParseStatus.Number, value, index);

        static bool ContainsExponentMarker(ReadOnlySpan<byte> value)
        {
            foreach (var character in value)
            {
                if (character == 0)
                    return false;
                if (character is (byte)'p' or (byte)'P')
                    return true;
            }
            return false;
        }
    }

    private static AwkNumberParseResult ParseDecimal(ReadOnlySpan<byte> text, int start)
    {
        var index = start;
        if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
            index++;
        var digits = 0;
        while (index < text.Length && IsDigit(text[index]))
        {
            digits++;
            index++;
        }
        if (index < text.Length && text[index] == (byte)'.')
        {
            index++;
            while (index < text.Length && IsDigit(text[index]))
            {
                digits++;
                index++;
            }
        }
        if (digits == 0)
            return default;
        if (index < text.Length && text[index] is (byte)'e' or (byte)'E')
        {
            var marker = index++;
            if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
                index++;
            var exponentDigits = index;
            while (index < text.Length && IsDigit(text[index]))
                index++;
            if (index == exponentDigits)
                index = marker;
        }
        return Utf8Parser.TryParse(text[start..index], out double number, out var consumed) && consumed == index - start
            ? new(AwkNumberParseStatus.Number, number, index)
            : default;
    }

    private static AwkNumberParseResult ParseHexFloat(ReadOnlySpan<byte> text, int start)
    {
        var index = start;
        var negative = index < text.Length && text[index] == (byte)'-';
        if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
            index++;
        index += 2;
        ulong leading = 0;
        var keptBits = 0;
        long significantBits = 0;
        long fractionDigits = 0;
        var sticky = false;
        var hasDigits = false;
        var fraction = false;
        while (index < text.Length)
        {
            var digit = HexValue(text[index]);
            if (digit < 0)
            {
                if (!fraction && text[index] == (byte)'.')
                {
                    fraction = true;
                    index++;
                    continue;
                }
                break;
            }
            hasDigits = true;
            if (fraction)
                fractionDigits++;
            if (significantBits != 0 || digit != 0)
            {
                var bits = significantBits == 0 ? 32 - BitOperations.LeadingZeroCount((uint)digit) : 4;
                significantBits += bits;
                var available = 64 - keptBits;
                if (available >= bits)
                {
                    leading = (leading << bits) | (uint)digit;
                    keptBits += bits;
                }
                else
                {
                    leading = (leading << available) | ((uint)digit >> (bits - available));
                    sticky |= (digit & ((1 << (bits - available)) - 1)) != 0;
                    keptBits = 64;
                }
            }
            index++;
        }
        if (!hasDigits)
            return ParseDecimal(text, start);
        long exponent = 0;
        if (index < text.Length && text[index] is (byte)'p' or (byte)'P')
        {
            var marker = index++;
            var exponentNegative = index < text.Length && text[index] == (byte)'-';
            if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
                index++;
            var exponentStart = index;
            while (index < text.Length && IsDigit(text[index]))
            {
                // More than the maximum possible significand displacement is already decisive.
                exponent = Math.Min(1L << 40, exponent * 10 + text[index] - (byte)'0');
                index++;
            }
            if (index == exponentStart)
                index = marker;
            if (exponentNegative)
                exponent = -exponent;
        }
        var scale = exponent - 4 * fractionDigits + significantBits - 1;
        return new(AwkNumberParseStatus.Number, RoundHex(leading, keptBits, sticky, scale, negative), index);
    }

    private static double RoundHex(ulong leading, int keptBits, bool sticky, long exponent, bool negative)
    {
        var sign = negative ? 1UL << 63 : 0;
        if (keptBits == 0 || exponent < -1075)
            return BitConverter.UInt64BitsToDouble(sign);
        if (exponent > 1023)
            return BitConverter.UInt64BitsToDouble(sign | 0x7ff0000000000000);
        // Round directly to the destination precision, including subnormals, to avoid double rounding.
        var targetBits = exponent >= -1022 ? 53 : (int)(exponent + 1075);
        ulong rounded;
        if (keptBits <= targetBits)
            rounded = leading << (targetBits - keptBits);
        else
        {
            var shift = keptBits - targetBits;
            rounded = shift == 64 ? 0 : leading >> shift;
            var halfway = 1UL << (shift - 1);
            var remainder = shift == 64 ? leading : leading & ((1UL << shift) - 1);
            if (remainder > halfway || remainder == halfway && (sticky || (rounded & 1) != 0))
                rounded++;
        }
        if (exponent < -1022)
            return BitConverter.UInt64BitsToDouble(sign | rounded);
        if (rounded == 1UL << 53)
        {
            rounded >>= 1;
            exponent++;
        }
        return BitConverter.UInt64BitsToDouble(sign | ((ulong)(exponent + 1023) << 52) | (rounded & 0xfffffffffffff));
    }

    private static bool TryParseSpecial(ReadOnlySpan<byte> text, int start, AwkNumberParsingMode mode,
        out AwkNumberParseResult result)
    {
        result = default;
        var index = start;
        var signed = index < text.Length && text[index] is (byte)'+' or (byte)'-';
        var negative = signed && text[index] == (byte)'-';
        if (signed)
            index++;
        var infinity = MatchesAsciiIgnoreCase(text[index..], "inf"u8);
        if (!infinity && !MatchesAsciiIgnoreCase(text[index..], "nan"u8))
            return false;
        index += 3;
        if (mode != AwkNumberParsingMode.HexadecimalInput)
        {
            if (!signed || SkipWhitespace(text, index) != text.Length)
                return false;
        }
        else if (infinity)
        {
            if (MatchesAsciiIgnoreCase(text[index..], "inity"u8))
                index += 5;
        }
        else if (index < text.Length && text[index] == (byte)'(')
        {
            var payloadEnd = index + 1;
            while (payloadEnd < text.Length && text[payloadEnd] is
                   >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or
                   >= (byte)'0' and <= (byte)'9' or (byte)'_')
                payloadEnd++;
            if (payloadEnd < text.Length && text[payloadEnd] == (byte)')')
                index = payloadEnd + 1;
        }
        result = new(AwkNumberParseStatus.Number,
            Math.CopySign(infinity ? double.PositiveInfinity : double.NaN, negative ? -1 : 1), index);
        return true;
    }

    private static bool MatchesAsciiIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> prefix)
    {
        if (text.Length < prefix.Length)
            return false;
        for (var index = 0; index < prefix.Length; index++)
        {
            if (AwkByteFacts.ToAsciiLower(text[index]) != prefix[index])
                return false;
        }
        return true;
    }

    private static bool IsHexPrefix(ReadOnlySpan<byte> text, int start) =>
        start + 1 < text.Length && text[start] == (byte)'0' && text[start + 1] is (byte)'x' or (byte)'X';

    private static bool IsDigit(byte value) => value is >= (byte)'0' and <= (byte)'9';

    private static int HexValue(byte value) => value switch
    {
        >= (byte)'0' and <= (byte)'9' => value - (byte)'0',
        >= (byte)'a' and <= (byte)'f' => value - (byte)'a' + 10,
        >= (byte)'A' and <= (byte)'F' => value - (byte)'A' + 10,
        _ => -1
    };
}
