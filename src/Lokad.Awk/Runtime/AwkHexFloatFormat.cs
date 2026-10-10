using System.Globalization;

namespace Lokad.Awk;

internal readonly record struct AwkHexFloatFormat(
    ulong Fraction, int LeadingDigit, int Exponent, int Digits, char Sign,
    bool Uppercase, bool Point)
{
    private const ulong FractionMask = (1UL << 52) - 1;

    public static AwkHexFloatFormat CreateFinite(double value, PrintfPrecisionValue precision, PrintfFlags flags, bool uppercase)
    {
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var sign = (bits >> 63) != 0 ? '-' : flags.ForceSign ? '+' : flags.LeadingSpace ? ' ' : '\0';
        var binaryExponent = (int)((bits >> 52) & 2047);
        var leading = binaryExponent == 0 ? 0 : 1;
        var exponent = binaryExponent == 0 ? (value == 0 ? 0 : -1022) : binaryExponent - 1023;
        var fraction = bits & FractionMask;
        var digits = 13;
        if (precision.HasValue)
        {
            digits = precision.Value;
            if (digits < 13)
            {
                var discardedBits = 52 - digits * 4;
                var significand = ((ulong)leading << 52) | fraction;
                var discarded = significand & ((1UL << discardedBits) - 1);
                var half = 1UL << (discardedBits - 1);
                var retained = significand >> discardedBits;
                if (discarded > half || discarded == half && (retained & 1) != 0)
                    retained++;
                significand = retained << discardedBits;
                leading = (int)(significand >> 52);
                fraction = significand & FractionMask;
            }
        }
        else
        {
            var tail = fraction;
            while (digits > 0 && (tail & 15) == 0)
            {
                tail >>= 4;
                digits--;
            }
        }
        return new(fraction, leading, exponent, digits, sign, uppercase, digits > 0 || flags.AlternateForm);
    }

    public int ByteCount
    {
        get
        {
            var magnitude = Math.Abs(Exponent);
            var exponentDigits = magnitude >= 1000 ? 4 : magnitude >= 100 ? 3 : magnitude >= 10 ? 2 : 1;
            return AwkRuntimeMemory.CheckedAdd(Digits,
                5 + exponentDigits + (Point ? 1 : 0) + (Sign == '\0' ? 0 : 1));
        }
    }

    // Callers preflight ByteCount before allocating this compatibility-boundary string.
    public string Format() => string.Create(ByteCount, this, static (output, value) =>
    {
        var index = 0;
        if (value.Sign != '\0')
            output[index++] = value.Sign;
        output[index++] = '0';
        output[index++] = value.Uppercase ? 'X' : 'x';
        output[index++] = (char)('0' + value.LeadingDigit);
        if (value.Point)
            output[index++] = '.';
        var alphabet = value.Uppercase ? "0123456789ABCDEF" : "0123456789abcdef";
        var significant = Math.Min(value.Digits, 13);
        for (var digit = 0; digit < significant; digit++)
            output[index++] = alphabet[(int)((value.Fraction >> (48 - digit * 4)) & 15)];
        output.Slice(index, value.Digits - significant).Fill('0');
        index += value.Digits - significant;
        output[index++] = value.Uppercase ? 'P' : 'p';
        output[index++] = value.Exponent < 0 ? '-' : '+';
        Math.Abs(value.Exponent).TryFormat(output[index..], out _, provider: CultureInfo.InvariantCulture);
    });
}
