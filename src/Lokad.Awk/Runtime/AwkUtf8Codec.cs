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
        string.IsNullOrEmpty(text) ? ReadOnlyMemory<byte>.Empty : StrictUtf8.GetBytes(text);

    public static ReadOnlyMemory<byte> Encode(string text, AwkRuntimeCounters counters)
    {
        if (string.IsNullOrEmpty(text))
            return ReadOnlyMemory<byte>.Empty;

        counters.RecordUtf8Encode();
        return EncodeOwned(text);
    }

    public static byte[] EncodeOwned(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        return StrictUtf8.GetBytes(text);
    }

    public static byte[] EncodeOwned(string text, AwkRuntimeCounters counters)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        counters.RecordUtf8Encode();
        return EncodeOwned(text);
    }

    public static ReadOnlyMemory<byte> EncodeLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new[] { (byte)'\n' };
        }

        if (text[^1] == '\n')
        {
            return StrictUtf8.GetBytes(text);
        }

        var byteCount = StrictUtf8.GetByteCount(text);
        var buffer = new byte[byteCount + 1];
        StrictUtf8.GetBytes(text.AsSpan(), buffer.AsSpan());
        buffer[^1] = (byte)'\n';
        return buffer;
    }
}
