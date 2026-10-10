using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Numerics;

namespace Lokad.Awk;

internal static class AwkNumberFormatter
{
    private const int MaxFormattedDoubleBytes = 32;
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

        if (IsInteger(value))
            return FitsSignedInteger(value)
                ? ((long)value).ToString(CultureInfo.InvariantCulture)
                : new BigInteger(value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    public static AwkText ToAwkText(double value) =>
        ToAwkText(value, AwkRuntimeCounters.Disabled);

    public static AwkText ToAwkText(double value, AwkRuntimeCounters counters)
    {
        Span<byte> buffer = stackalloc byte[FormattedByteCapacity(value)];
        return AwkText.FromTrustedOwnedBytes(Format(value, buffer).ToArray(), counters);
    }

    public static AwkText ToAwkText(
        double value,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        AwkMemoryLease? retainedTextLease = null)
    {
        Span<byte> buffer = stackalloc byte[FormattedByteCapacity(value)];
        var bytes = Format(value, buffer);
        return CopyAscii(bytes, counters, governor, retainedTextLease);
    }

    public static AwkText ToDefaultFormatAwkText(
        double value,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        AwkMemoryLease? retainedTextLease = null)
    {
        static ReadOnlySpan<byte> NormalizeLowercaseExponent(
            ReadOnlySpan<byte> bytes,
            Span<byte> destination)
        {
            var exponentIndex = bytes.IndexOfAny((byte)'e', (byte)'E');
            if (exponentIndex < 0)
                return bytes;

            bytes[..exponentIndex].CopyTo(destination);
            var writeIndex = exponentIndex;
            destination[writeIndex++] = (byte)'e';

            var exponent = bytes[(exponentIndex + 1)..];
            if (exponent.Length > 0 && exponent[0] is (byte)'+' or (byte)'-')
            {
                destination[writeIndex++] = exponent[0];
                exponent = exponent[1..];
            }

            while (exponent.Length > 2 && exponent[0] == (byte)'0')
                exponent = exponent[1..];

            while (exponent.Length < 2)
                destination[writeIndex++] = (byte)'0';

            exponent.CopyTo(destination[writeIndex..]);
            writeIndex += exponent.Length;
            return destination[..writeIndex];
        }

        if (double.IsPositiveInfinity(value))
            return CopyAscii("+inf"u8, counters, governor, retainedTextLease);

        if (double.IsNegativeInfinity(value))
            return CopyAscii("-inf"u8, counters, governor, retainedTextLease);

        if (double.IsNaN(value))
            return CopyAscii(BitConverter.DoubleToInt64Bits(value) < 0 ? "-nan"u8 : "+nan"u8, counters, governor, retainedTextLease);

        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        if (!Utf8Formatter.TryFormat(value, buffer, out var formattedBytes, new StandardFormat('G', 6)))
            throw new InvalidOperationException("Unable to format an awk numeric value.");

        var bytes = buffer[..formattedBytes];
        Span<byte> normalized = stackalloc byte[MaxFormattedDoubleBytes];
        var output = NormalizeLowercaseExponent(bytes, normalized);
        return CopyAscii(output, counters, governor, retainedTextLease);
    }

    private static AwkText CopyAscii(
        ReadOnlySpan<byte> bytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        AwkMemoryLease? retainedTextLease)
    {
        retainedTextLease?.ReserveAdditional(AwkRuntimeMemory.EstimateTextBytes(bytes.Length));
        // A supplied lease already admits the complete owned text, including its bytes.
        var owned = AwkText.AllocateOwnedBytes(bytes.Length,
            retainedTextLease is null ? governor : AwkMemoryGovernor.Disabled);
        bytes.CopyTo(owned);
        return AwkText.FromTrustedOwnedBytes(owned, counters);
    }

    public static void AppendTo(AwkUtf8Builder builder, double value)
    {
        Span<byte> buffer = stackalloc byte[FormattedByteCapacity(value)];
        builder.Append(Format(value, buffer));
    }

    public static bool IsInteger(double value) =>
        Math.Abs(value % 1) < double.Epsilon;

    private static bool FitsSignedInteger(double value) =>
        value >= long.MinValue && value < -(double)long.MinValue;

    private static int FormattedByteCapacity(double value) =>
        FitsSignedInteger(value) || !IsInteger(value) ? MaxFormattedDoubleBytes : MaxFormattedIntegerBytes;

    private static ReadOnlySpan<byte> Format(double value, Span<byte> buffer)
    {
        if (double.IsPositiveInfinity(value))
            return "+inf"u8;

        if (double.IsNegativeInfinity(value))
            return "-inf"u8;

        if (double.IsNaN(value))
            return "nan"u8;

        if (IsInteger(value))
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
