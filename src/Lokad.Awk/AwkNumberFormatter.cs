using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal static class AwkNumberFormatter
{
    private const int MaxFormattedDoubleBytes = 32;

    public static string ToString(double value)
    {
        if (double.IsPositiveInfinity(value))
            return "+inf";

        if (double.IsNegativeInfinity(value))
            return "-inf";

        if (double.IsNaN(value))
            return "nan";

        if (IsInteger(value))
            return ((long)value).ToString(CultureInfo.InvariantCulture);

        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    public static AwkText ToAwkText(double value) =>
        ToAwkText(value, AwkRuntimeCounters.Disabled);

    public static AwkText ToAwkText(double value, AwkRuntimeCounters counters)
    {
        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        return AwkText.FromTrustedOwnedBytes(Format(value, buffer).ToArray(), counters);
    }

    public static AwkText ToAwkText(
        double value,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        var bytes = Format(value, buffer);
        var owned = AwkText.AllocateOwnedBytes(bytes.Length, governor);
        bytes.CopyTo(owned);
        return AwkText.FromTrustedOwnedBytes(owned, counters);
    }

    public static AwkText ToDefaultFormatAwkText(
        double value,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        static AwkText CopyAscii(
            ReadOnlySpan<byte> bytes,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor governor)
        {
            var owned = AwkText.AllocateOwnedBytes(bytes.Length, governor);
            bytes.CopyTo(owned);
            return AwkText.FromTrustedOwnedBytes(owned, counters);
        }

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
            return CopyAscii("+inf"u8, counters, governor);

        if (double.IsNegativeInfinity(value))
            return CopyAscii("-inf"u8, counters, governor);

        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        if (!Utf8Formatter.TryFormat(value, buffer, out var formattedBytes, new StandardFormat('G', 6)))
            throw new InvalidOperationException("Unable to format an awk numeric value.");

        var bytes = buffer[..formattedBytes];
        Span<byte> normalized = stackalloc byte[MaxFormattedDoubleBytes];
        var output = NormalizeLowercaseExponent(bytes, normalized);
        var owned = AwkText.AllocateOwnedBytes(output.Length, governor);
        output.CopyTo(owned);
        return AwkText.FromTrustedOwnedBytes(owned, counters);
    }

    public static void AppendTo(AwkUtf8Builder builder, double value)
    {
        Span<byte> buffer = stackalloc byte[MaxFormattedDoubleBytes];
        builder.Append(Format(value, buffer));
    }

    public static bool IsInteger(double value) =>
        Math.Abs(value % 1) < double.Epsilon;

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
            if (!Utf8Formatter.TryFormat((long)value, buffer, out var bytesWritten))
                throw new InvalidOperationException("Unable to format an awk integer value.");

            return buffer[..bytesWritten];
        }

        if (!Utf8Formatter.TryFormat(value, buffer, out var formattedBytes, new StandardFormat('G')))
            throw new InvalidOperationException("Unable to format an awk numeric value.");

        return buffer[..formattedBytes];
    }

}
