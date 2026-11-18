using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Numerics;

namespace Lokad.Awk;

internal static class AwkNumberFormatter
{
    internal const int MaxFormattedDoubleBytes = 32;
    // A finite binary64 integer has at most 309 decimal digits plus its sign.
    private const int MaxFormattedIntegerBytes = 310;

    public static string ToString(double value)
    {
        if (double.IsPositiveInfinity(value))
            return "+inf";

        if (double.IsNegativeInfinity(value))
            return "-inf";

        if (double.IsNaN(value))
            return "nan";

        if (double.IsInteger(value))
            return FitsSignedInteger(value)
                ? ((long)value).ToString(CultureInfo.InvariantCulture)
                : new BigInteger(value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    public static AwkText ToAwkText(double value)
    {
        Span<byte> buffer = stackalloc byte[FormattedByteCapacity(value)];
        return AwkText.FromTrustedOwnedBytes(Format(value, buffer).ToArray());
    }

    internal static void Append(double value, AwkUtf8Builder destination, Action? checkpoint)
    {
        Span<byte> buffer = stackalloc byte[FormattedByteCapacity(value)];
        destination.Append(Format(value, buffer), checkpoint);
    }

    public static string ToDefaultFormatString(double value, Action? checkpoint)
    {
        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        var bytes = FormatDefault(value, buffer);
        checkpoint?.Invoke();
        var text = AwkUtf8Codec.DecodeRuntimeText(bytes);
        checkpoint?.Invoke();
        return text;
    }

    internal static ReadOnlySpan<byte> FormatDefault(double value, Span<byte> buffer)
    {
        if (double.IsPositiveInfinity(value))
            return "+inf"u8;

        if (double.IsNegativeInfinity(value))
            return "-inf"u8;

        if (double.IsNaN(value))
            return BitConverter.DoubleToInt64Bits(value) < 0 ? "-nan"u8 : "+nan"u8;

        if (!Utf8Formatter.TryFormat(value, buffer, out var formattedBytes, new StandardFormat('g', 6)))
            throw new InvalidOperationException("Unable to format an awk numeric value.");

        return buffer[..formattedBytes];
    }

    private static bool FitsSignedInteger(double value) =>
        value >= long.MinValue && value < -(double)long.MinValue;

    internal static int FormattedByteCapacity(double value) =>
        FitsSignedInteger(value) || !double.IsInteger(value) ? MaxFormattedDoubleBytes : MaxFormattedIntegerBytes;

    internal static ReadOnlySpan<byte> Format(double value, Span<byte> buffer)
    {
        if (double.IsPositiveInfinity(value))
            return "+inf"u8;

        if (double.IsNegativeInfinity(value))
            return "-inf"u8;

        if (double.IsNaN(value))
            return "nan"u8;

        if (double.IsInteger(value))
        {
            var formatted = FitsSignedInteger(value)
                ? Utf8Formatter.TryFormat((long)value, buffer, out var bytesWritten)
                : TryFormatLargeInteger(value, buffer, out bytesWritten);
            if (!formatted)
                throw new InvalidOperationException("Unable to format an awk integer value.");

            return buffer[..bytesWritten];
        }

        if (!Utf8Formatter.TryFormat(value, buffer, out var formattedBytes, new StandardFormat('G')))
            throw new InvalidOperationException("Unable to format an awk numeric value.");

        return buffer[..formattedBytes];
    }

    private static bool TryFormatLargeInteger(double value, Span<byte> destination, out int bytesWritten)
    {
        Span<char> digits = stackalloc char[MaxFormattedIntegerBytes];
        if (!new BigInteger(value).TryFormat(digits, out bytesWritten, provider: CultureInfo.InvariantCulture) ||
            bytesWritten > destination.Length)
            return false;
        // Invariant integer formatting contains only ASCII digits and the optional minus sign.
        for (var index = 0; index < bytesWritten; index++)
            destination[index] = (byte)digits[index];
        return true;
    }

}
