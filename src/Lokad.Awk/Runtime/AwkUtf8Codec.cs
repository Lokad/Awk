using System;
using System.Text;

namespace Lokad.Awk;

internal static class AwkUtf8Codec
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Decode(ReadOnlyMemory<byte> bytes) =>
        bytes.IsEmpty ? string.Empty : StrictUtf8.GetString(bytes.Span);

    public static string Decode(ReadOnlySpan<byte> bytes) =>
        bytes.IsEmpty ? string.Empty : StrictUtf8.GetString(bytes);

    public static string DecodeRuntimeText(ReadOnlyMemory<byte> bytes) =>
        DecodeRuntimeText(bytes, AwkRuntimeCounters.Disabled);

    public static string DecodeRuntimeText(ReadOnlyMemory<byte> bytes, AwkRuntimeCounters counters)
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
        text.Length == 0 ? ReadOnlyMemory<byte>.Empty : StrictUtf8.GetBytes(text);

    public static ReadOnlyMemory<byte> Encode(string text, AwkRuntimeCounters counters)
    {
        if (text.Length == 0)
            return ReadOnlyMemory<byte>.Empty;

        counters.RecordUtf8Encode();
        return EncodeOwned(text);
    }

    public static byte[] EncodeOwned(string text)
    {
        if (text.Length == 0)
            return [];

        return StrictUtf8.GetBytes(text);
    }

    public static byte[] EncodeOwned(string text, AwkRuntimeCounters counters)
    {
        if (text.Length == 0)
            return [];

        counters.RecordUtf8Encode();
        return EncodeOwned(text);
    }

    public static int GetEncodedByteCount(ReadOnlySpan<char> text) =>
        StrictUtf8.GetByteCount(text);

    public static int Encode(
        ReadOnlySpan<char> text,
        Span<byte> destination,
        AwkRuntimeCounters counters)
    {
        if (text.IsEmpty)
            return 0;

        counters.RecordUtf8Encode();
        return StrictUtf8.GetBytes(text, destination);
    }

    public static int EncodeWithoutCounting(
        ReadOnlySpan<char> text,
        Span<byte> destination) =>
        StrictUtf8.GetBytes(text, destination);

}
