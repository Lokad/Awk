using System.Buffers.Text;
using System.Numerics;

namespace Lokad.Awk;

internal enum AwkNumberParseStatus
{
    NoNumber,
    Number,
    MissingHexExponent
}

internal readonly struct AwkNumberParseResult(AwkNumberParseStatus status, double number, int end)
{
    // Keep the double together and the two integer facts adjacent: the result
    // crosses parser/value boundaries without the positional record's padding.
    public double Number { get; } = number;
    public AwkNumberParseStatus Status { get; } = status;
    public int End { get; } = end;

    public void ThrowIfError()
    {
        if (Status == AwkNumberParseStatus.MissingHexExponent)
            throw new AwkRuntimeException(AwkMessages.ErrorHexadecimalFloatExponent);
    }
}

internal static class AwkNumberParser
{
    public static int SkipWhitespace(ReadOnlySpan<byte> text, int index, Action? checkpoint = null)
    {
        while (index < text.Length && text[index] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' or (byte)'\v' or (byte)'\f')
        {
            CheckScanProgress(index, checkpoint);
            index++;
        }
        return index;
    }

    public static void Parse(ReadOnlySpan<byte> text, AwkNumberParsingMode mode,
        out AwkNumberParseResult result, Action? checkpoint = null)
    {
        // Short nonnumeric text needs no scan checkpoint; execution boundaries already
        // check cancellation, and cleanup inspections can still read existing text.
        if (text.Length >= AwkRuntimeLimits.CancellationCheckBytes)
            checkpoint?.Invoke();
        var start = SkipWhitespace(text, 0, checkpoint);
        if (TryParseSpecial(text, start, mode, out result, checkpoint))
            return;

        var digits = start;
        if (digits < text.Length && text[digits] is (byte)'+' or (byte)'-')
            digits++;
        if (IsHexPrefix(text, digits))
        {
            if (mode == AwkNumberParsingMode.Decimal)
                return;
            if (mode == AwkNumberParsingMode.NonDecimalData && digits == start)
                ParseNonDecimal(text, start, 16, out result, checkpoint);
            else
                ParseHexFloat(text, start, out result, checkpoint);
            return;
        }
        if (mode == AwkNumberParsingMode.NonDecimalData && GetBase(text[start..], checkpoint) == 8)
            ParseNonDecimal(text, start, 8, out result, checkpoint);
        else
            ParseDecimal(text, start, out result, checkpoint);
    }

    // strtonum chooses a base from the original bytes, before sign/whitespace trimming.
    public static int GetBase(ReadOnlySpan<byte> text, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (text.Length < 2 || text[0] != (byte)'0')
            return 10;
        if (text[1] is (byte)'x' or (byte)'X')
            return 16;
        for (var index = 0; index < text.Length; index++)
        {
            CheckScanProgress(index, checkpoint);
            if (text[index] is (byte)'.' or (byte)'e' or (byte)'E')
                return 10;
            if (!IsDigit(text[index]))
                break;
        }
        return text[1] is >= (byte)'0' and <= (byte)'7' ? 8 : 10;
    }

    public static void ParseNonDecimal(ReadOnlySpan<byte> text, int start, int numberBase,
        out AwkNumberParseResult result, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var index = start + (numberBase == 16 ? 2 : 0);
        if (numberBase == 16 && index == text.Length)
        {
            result = new(AwkNumberParseStatus.NoNumber, 0, start);
            return;
        }
        double value = 0;
        while (index < text.Length)
        {
            CheckScanProgress(index, checkpoint);
            var digit = HexValue(text[index]);
            if (numberBase == 16 && text[index] is (byte)'.' or (byte)'p' or (byte)'P')
            {
                // GNU requires a p/P somewhere in the remaining C string, even for an incomplete exponent.
                if (!ContainsExponentMarker(text[start..], checkpoint))
                    result = new(AwkNumberParseStatus.MissingHexExponent, 0, start);
                else
                    ParseHexFloat(text, start, out result, checkpoint);
                return;
            }
            if (digit < 0 || digit >= numberBase)
            {
                if (numberBase == 8 && text[index] is (byte)'8' or (byte)'9')
                {
                    ParseDecimal(text, start, out result, checkpoint);
                    return;
                }
                break;
            }
            // GNU nondec2awknum rounds each multiply/add, rather than through a signed integer carrier.
            value = value * numberBase + digit;
            index++;
        }
        result = new(AwkNumberParseStatus.Number, value, index);

        static bool ContainsExponentMarker(ReadOnlySpan<byte> value, Action? checkpoint)
        {
            for (var index = 0; index < value.Length; index++)
            {
                CheckScanProgress(index, checkpoint);
                var character = value[index];
                if (character == 0)
                    return false;
                if (character is (byte)'p' or (byte)'P')
                    return true;
            }
            return false;
        }
    }

    private static void ParseDecimal(ReadOnlySpan<byte> text, int start,
        out AwkNumberParseResult result, Action? checkpoint)
    {
        var index = start;
        if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
            index++;
        var digitStart = index;
        var integerLimit = index + Math.Min(15, text.Length - index);
        ulong integer = 0;
        while (index < integerLimit && IsDigit(text[index]))
        {
            CheckScanProgress(index, checkpoint);
            integer = integer * 10 + (uint)(text[index] - (byte)'0');
            index++;
        }
        // Longer input still needs its extent, but no unused accumulator arithmetic.
        while (index < text.Length && IsDigit(text[index]))
        {
            CheckScanProgress(index, checkpoint);
            index++;
        }
        var digits = index - digitStart;
        var integerEnd = index;
        if (index < text.Length && text[index] == (byte)'.')
        {
            index++;
            while (index < text.Length && IsDigit(text[index]))
            {
                CheckScanProgress(index, checkpoint);
                digits++;
                index++;
            }
        }
        if (digits == 0)
        {
            result = default;
            return;
        }
        if (index < text.Length && text[index] is (byte)'e' or (byte)'E')
        {
            var marker = index++;
            if (index < text.Length && text[index] is (byte)'+' or (byte)'-')
                index++;
            var exponentDigits = index;
            while (index < text.Length && IsDigit(text[index]))
            {
                CheckScanProgress(index, checkpoint);
                index++;
            }
            if (index == exponentDigits)
                index = marker;
        }
        // Short scans contain no admission or host callback. The interpreter's
        // region and pre-effect boundaries cover them; long scans still poll
        // during recognition and before the non-cooperative conversion below.
        if (text.Length >= AwkRuntimeLimits.CancellationCheckBytes)
            checkpoint?.Invoke();
        // Up to fifteen decimal digits are exact in a double. Reuse the recognized
        // digits instead of sending the same integer bytes through a second parser.
        // Longer integers and every fraction/exponent retain ordinary rounding.
        if (index == integerEnd && digits <= 15)
        {
            var value = (double)integer;
            result = new(AwkNumberParseStatus.Number, text[start] == (byte)'-' ? -value : value, index);
            return;
        }
        result = Utf8Parser.TryParse(text[start..index], out double number, out var consumed) && consumed == index - start
            ? new(AwkNumberParseStatus.Number, number, index)
            : default;
    }

    private static void ParseHexFloat(ReadOnlySpan<byte> text, int start,
        out AwkNumberParseResult result, Action? checkpoint)
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
            CheckScanProgress(index, checkpoint);
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
        {
            ParseDecimal(text, start, out result, checkpoint);
            return;
        }
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
                CheckScanProgress(index, checkpoint);
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
        result = new(AwkNumberParseStatus.Number, RoundHex(leading, keptBits, sticky, scale, negative), index);
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
        out AwkNumberParseResult result, Action? checkpoint)
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
            if (!signed || SkipWhitespace(text, index, checkpoint) != text.Length)
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
            {
                CheckScanProgress(payloadEnd, checkpoint);
                payloadEnd++;
            }
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

    private static void CheckScanProgress(int offset, Action? checkpoint)
    {
        if (offset > 0 && (offset & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
            checkpoint?.Invoke();
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
