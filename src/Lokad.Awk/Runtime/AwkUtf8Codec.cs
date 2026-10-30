using System;
using System.Text;

namespace Lokad.Awk;

internal static class AwkUtf8Codec
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static Decoder CreateDecoder() => StrictUtf8.GetDecoder();

    public static string Decode(ReadOnlyMemory<byte> bytes) =>
        bytes.IsEmpty ? string.Empty : StrictUtf8.GetString(bytes.Span);

    public static string Decode(ReadOnlySpan<byte> bytes) =>
        bytes.IsEmpty ? string.Empty : StrictUtf8.GetString(bytes);

    public static string DecodeRuntimeText(ReadOnlySpan<byte> bytes, AwkRuntimeCounters counters)
    {
        try
        {
            var decoded = Decode(bytes);
            counters.RecordStringDecode();
            return decoded;
        }
        catch (DecoderFallbackException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
        }
    }

    public static ReadOnlyMemory<byte> Encode(string text) =>
        EncodeOwned(text);

    public static byte[] EncodeOwned(string text)
    {
        if (text.Length == 0)
            return [];

        try { return StrictUtf8.GetBytes(text); }
        catch (EncoderFallbackException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
        }
    }

    public static byte[] EncodeOwned(string text, AwkRuntimeCounters counters)
    {
        if (text.Length == 0)
            return [];

        counters.RecordUtf8Encode();
        return EncodeOwned(text);
    }

    public static int GetEncodedByteCount(ReadOnlySpan<char> text)
    {
        try { return StrictUtf8.GetByteCount(text); }
        catch (EncoderFallbackException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
        }
    }

    public static int Encode(
        ReadOnlySpan<char> text,
        Span<byte> destination,
        AwkRuntimeCounters counters)
    {
        if (text.IsEmpty)
            return 0;

        counters.RecordUtf8Encode();
        return EncodeWithoutCounting(text, destination);
    }

    public static int EncodeWithoutCounting(
        ReadOnlySpan<char> text,
        Span<byte> destination)
    {
        try { return StrictUtf8.GetBytes(text, destination); }
        catch (EncoderFallbackException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
        }
    }

}
