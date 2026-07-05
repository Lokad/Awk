using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkStringRuntime
{
    private readonly Func<bool> _charactersAsBytes;

    public AwkStringRuntime(Func<bool> charactersAsBytes)
    {
        _charactersAsBytes = charactersAsBytes;
    }

    public int CountUnits(string value) =>
        _charactersAsBytes() ? Encoding.UTF8.GetByteCount(value) : CountTextElements(value);

    public int CountUnits(AwkText value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _charactersAsBytes() ? value.ByteLength : value.RuneLength;
    }

    public int IndexOfUnits(string source, string value)
    {
        if (!_charactersAsBytes())
        {
            var position = source.IndexOf(value, StringComparison.Ordinal);
            return position < 0 ? 0 : CountTextElements(source.AsSpan(0, position)) + 1;
        }

        var byteModePosition = source.IndexOf(value, StringComparison.Ordinal);
        return byteModePosition < 0
            ? 0
            : Encoding.UTF8.GetByteCount(source.AsSpan(0, byteModePosition)) + 1;
    }

    public int IndexOfUnits(AwkText source, AwkText value)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(value);
        var position = source.IndexOf(value);
        if (position < 0)
            return 0;

        return _charactersAsBytes()
            ? position + 1
            : CountRunes(source.AsSpan()[..position]) + 1;
    }

    public string SubstringByUnits(string source, int start, int length)
    {
        if (!_charactersAsBytes())
        {
            var startIndex = GetTextElementStringIndex(source, start);
            var endIndex = GetTextElementStringIndex(source, start + length);
            return source[startIndex..endIndex];
        }

        return TryGetStringRangeForByteRange(source, start, length, out var byteStartIndex, out var byteEndIndex)
            ? source[byteStartIndex..byteEndIndex]
            : DecodeByteSlice(source, start, length);
    }

    public AwkText SubstringByUnits(AwkText source, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _charactersAsBytes()
            ? source.CopyByteSlice(start, length)
            : source.CopyRuneSlice(start, length);
    }

    public AwkText SubstringByUnits(
        AwkText source,
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        ArgumentNullException.ThrowIfNull(source);
        return _charactersAsBytes()
            ? source.CopyByteSlice(start, length, counters, governor)
            : source.CopyRuneSlice(start, length, counters, governor);
    }

    private static int CountTextElements(ReadOnlySpan<char> value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
            count++;

        return count;
    }

    private static int CountRunes(ReadOnlySpan<byte> value)
    {
        var count = 0;
        while (!value.IsEmpty)
        {
            var status = Rune.DecodeFromUtf8(value, out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            count++;
            value = value[consumed..];
        }

        return count;
    }

    private static int GetTextElementStringIndex(string value, int textElementOffset)
    {
        if (textElementOffset <= 0)
            return 0;

        var count = 0;
        var index = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (count == textElementOffset)
                return index;

            index += rune.Utf16SequenceLength;
            count++;
        }

        return value.Length;
    }

    private static bool TryGetStringRangeForByteRange(
        string value,
        int byteStart,
        int byteLength,
        out int startIndex,
        out int endIndex)
    {
        startIndex = 0;
        endIndex = 0;
        var byteEnd = byteStart + byteLength;
        var byteOffset = 0;
        var charIndex = 0;
        var foundStart = byteStart == 0;

        if (foundStart)
            startIndex = 0;

        foreach (var rune in value.EnumerateRunes())
        {
            var nextCharIndex = charIndex + rune.Utf16SequenceLength;
            var nextByteOffset = byteOffset + Encoding.UTF8.GetByteCount(value.AsSpan(charIndex, rune.Utf16SequenceLength));
            if (!foundStart)
            {
                if (byteStart == byteOffset)
                {
                    startIndex = charIndex;
                    foundStart = true;
                }
                else if (byteStart > byteOffset && byteStart < nextByteOffset)
                {
                    return false;
                }
            }

            if (foundStart)
            {
                if (byteEnd == byteOffset)
                {
                    endIndex = charIndex;
                    return true;
                }

                if (byteEnd == nextByteOffset)
                {
                    endIndex = nextCharIndex;
                    return true;
                }

                if (byteEnd > byteOffset && byteEnd < nextByteOffset)
                    return false;
            }

            byteOffset = nextByteOffset;
            charIndex = nextCharIndex;
        }

        if (!foundStart && byteStart == byteOffset)
        {
            startIndex = value.Length;
            foundStart = true;
        }

        if (foundStart && byteEnd == byteOffset)
        {
            endIndex = value.Length;
            return true;
        }

        return false;
    }

    private static string DecodeByteSlice(string value, int byteStart, int byteLength)
    {
        if (byteLength <= 0)
            return string.Empty;

        var bytes = new byte[byteLength];
        var byteOffset = 0;
        var written = 0;
        var charIndex = 0;
        Span<byte> encoded = stackalloc byte[4];
        foreach (var rune in value.EnumerateRunes())
        {
            var runeCharLength = rune.Utf16SequenceLength;
            var encodedLength = Encoding.UTF8.GetBytes(value.AsSpan(charIndex, runeCharLength), encoded);
            for (var index = 0; index < encodedLength; index++)
            {
                if (byteOffset >= byteStart && written < byteLength)
                    bytes[written++] = encoded[index];

                byteOffset++;
                if (written == byteLength)
                    return Encoding.UTF8.GetString(bytes);
            }

            charIndex += runeCharLength;
        }

        return Encoding.UTF8.GetString(bytes, 0, written);
    }
}
