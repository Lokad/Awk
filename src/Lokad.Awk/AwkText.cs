using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Threading;

namespace Lokad.Awk;

internal sealed class AwkText : IEquatable<AwkText>
{
    private readonly byte[] _bytes;
    private string? _cachedString;

    private AwkText(byte[] bytes)
    {
        _bytes = bytes;
    }

    public static AwkText Empty { get; } = new([]);

    public int ByteLength => _bytes.Length;

    public int RuneLength => CountRunes(_bytes);

    public ReadOnlySpan<byte> AsSpan() => _bytes;

    public ReadOnlyMemory<byte> AsMemory() => _bytes;

    public static AwkText FromSourceLiteralString(string text) =>
        FromString(text, AwkRuntimeCounters.Disabled);

    public static AwkText FromString(string text) =>
        FromString(text, AwkRuntimeCounters.Disabled);

    public static AwkText FromString(string text, AwkRuntimeCounters counters)
    {
        if (string.IsNullOrEmpty(text))
            return Empty;

        var bytes = AwkUtf8Codec.EncodeOwned(text, counters);
        counters.RecordValueCreated();
        counters.RecordAwkTextAllocation(bytes.Length);
        return new AwkText(bytes);
    }

    public static AwkText FromString(
        string text,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (string.IsNullOrEmpty(text))
            return Empty;

        var bytes = AllocateOwnedBytes(AwkUtf8Codec.GetEncodedByteCount(text), governor);
        AwkUtf8Codec.Encode(text, bytes, counters);
        counters.RecordValueCreated();
        counters.RecordAwkTextAllocation(bytes.Length);
        return new AwkText(bytes);
    }

    public static AwkText FromHostBytes(ReadOnlyMemory<byte> bytes) =>
        FromHostBytes(bytes, AwkRuntimeCounters.Disabled);

    public static AwkText FromHostBytes(ReadOnlyMemory<byte> bytes, AwkRuntimeCounters counters)
    {
        if (bytes.IsEmpty)
            return Empty;

        ThrowIfInvalidUtf8(bytes.Span);
        return FromTrustedOwnedBytes(bytes.ToArray(), counters);
    }

    public static AwkText FromOwnedBytes(byte[] bytes) =>
        FromOwnedBytes(bytes, AwkRuntimeCounters.Disabled);

    public static AwkText FromOwnedBytes(byte[] bytes, AwkRuntimeCounters counters)
    {
        if (bytes.Length == 0)
            return Empty;

        ThrowIfInvalidUtf8(bytes);
        return FromTrustedOwnedBytes(bytes, counters);
    }

    public static AwkText FromOwnedBytes(
        byte[] bytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (bytes.Length == 0)
            return Empty;

        RecordOwnedBytesAllocation(bytes.Length, governor);
        ThrowIfInvalidUtf8(bytes);
        return CreateTrustedOwnedBytes(bytes, counters);
    }

    internal static AwkText FromTrustedOwnedBytes(byte[] bytes) =>
        FromTrustedOwnedBytes(bytes, AwkRuntimeCounters.Disabled);

    internal static AwkText FromTrustedOwnedBytes(byte[] bytes, AwkRuntimeCounters counters)
    {
        if (bytes.Length == 0)
            return Empty;

        return CreateTrustedOwnedBytes(bytes, counters);
    }

    internal static AwkText FromTrustedOwnedBytes(
        byte[] bytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (bytes.Length == 0)
            return Empty;

        RecordOwnedBytesAllocation(bytes.Length, governor);
        return CreateTrustedOwnedBytes(bytes, counters);
    }

    public string AsString() =>
        AsString(AwkRuntimeCounters.Disabled);

    public string AsString(AwkRuntimeCounters counters)
    {
        counters.RecordAwkTextAsStringCall();
        if (_bytes.Length == 0)
            return string.Empty;

        var cached = _cachedString;
        if (cached is not null)
            return cached;

        counters.RecordAwkTextAsStringDecodeMiss();
        var decoded = AwkUtf8Codec.DecodeRuntimeText(_bytes, counters);
        return Interlocked.CompareExchange(ref _cachedString, decoded, null) ?? decoded;
    }

    public int CompareBytes(AwkText other)
    {
        return AsSpan().SequenceCompareTo(other.AsSpan());
    }

    public bool EqualsAscii(string value)
    {
        var span = AsSpan();
        if (span.Length != value.Length)
            return false;

        for (var index = 0; index < span.Length; index++)
        {
            if (value[index] > 0x7F || span[index] != (byte)value[index])
                return false;
        }

        return true;
    }

    public AwkText Concat(AwkText other) =>
        Concat(other, AwkRuntimeCounters.Disabled);

    public AwkText Concat(AwkText other, AwkRuntimeCounters counters)
    {
        if (ByteLength == 0)
            return other;

        if (other.ByteLength == 0)
            return this;

        var length = CheckedLength(ByteLength, other.ByteLength);
        var bytes = new byte[length];
        AsSpan().CopyTo(bytes);
        other.AsSpan().CopyTo(bytes.AsSpan(ByteLength));
        return FromTrustedOwnedBytes(bytes, counters);
    }

    public AwkText Concat(
        AwkText other,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (ByteLength == 0)
            return other;

        if (other.ByteLength == 0)
            return this;

        var length = CheckedLength(ByteLength, other.ByteLength);
        var bytes = AllocateOwnedBytes(length, governor);
        AsSpan().CopyTo(bytes);
        other.AsSpan().CopyTo(bytes.AsSpan(ByteLength));
        return FromTrustedOwnedBytes(bytes, counters);
    }

    public AwkText Repeat(int count) =>
        Repeat(count, AwkRuntimeCounters.Disabled);

    public AwkText Repeat(int count, AwkRuntimeCounters counters)
    {
        if (count <= 0 || ByteLength == 0)
            return Empty;

        if (count == 1)
            return this;

        var length = CheckedProduct(ByteLength, count);
        var bytes = new byte[length];
        var destination = bytes.AsSpan();
        for (var index = 0; index < count; index++)
        {
            AsSpan().CopyTo(destination[(index * ByteLength)..]);
        }

        return FromTrustedOwnedBytes(bytes, counters);
    }

    public AwkText Repeat(
        int count,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (count <= 0 || ByteLength == 0)
            return Empty;

        if (count == 1)
            return this;

        var length = CheckedProduct(ByteLength, count);
        var bytes = AllocateOwnedBytes(length, governor);
        var destination = bytes.AsSpan();
        for (var index = 0; index < count; index++)
        {
            AsSpan().CopyTo(destination[(index * ByteLength)..]);
        }

        return FromTrustedOwnedBytes(bytes, counters);
    }

    public AwkText CopyByteSlice(int start, int length)
    {
        if (start < 0 || start > ByteLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length < 0 || start > ByteLength - length)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length == 0)
            return Empty;

        if (start == 0 && length == ByteLength)
            return this;

        return FromOwnedBytes(AsSpan().Slice(start, length).ToArray());
    }

    public AwkText CopyByteSlice(
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (start < 0 || start > ByteLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length < 0 || start > ByteLength - length)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length == 0)
            return Empty;

        if (start == 0 && length == ByteLength)
            return this;

        var bytes = AllocateOwnedBytes(length, governor);
        AsSpan().Slice(start, length).CopyTo(bytes);
        return FromOwnedBytes(bytes, counters);
    }

    public AwkText CopyRuneSlice(int start, int length)
    {
        ResolveRuneByteRange(start, length, out var byteStart, out var byteEnd);

        if (length == 0)
            return Empty;

        if (byteStart == 0 && byteEnd == ByteLength)
            return this;

        return FromTrustedOwnedBytes(AsSpan()[byteStart..byteEnd].ToArray());
    }

    public AwkText CopyRuneSlice(
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        ResolveRuneByteRange(start, length, out var byteStart, out var byteEnd);

        if (length == 0)
            return Empty;

        if (byteStart == 0 && byteEnd == ByteLength)
            return this;

        var byteLength = byteEnd - byteStart;
        var bytes = AllocateOwnedBytes(byteLength, governor);
        AsSpan()[byteStart..byteEnd].CopyTo(bytes);
        return FromTrustedOwnedBytes(bytes, counters);
    }

    public int IndexOf(AwkText value)
    {
        return AsSpan().IndexOf(value.AsSpan());
    }

    public bool Contains(AwkText value) => IndexOf(value) >= 0;

    public bool StartsWith(AwkText value)
    {
        return AsSpan().StartsWith(value.AsSpan());
    }

    public bool EndsWith(AwkText value)
    {
        return AsSpan().EndsWith(value.AsSpan());
    }

    public bool EqualsAsciiIgnoreCase(AwkText other)
    {
        return ByteLength == other.ByteLength &&
            BytesEqualAsciiIgnoreCase(AsSpan(), other.AsSpan());
    }

    public bool StartsWithAsciiIgnoreCase(AwkText value)
    {
        return ByteLength >= value.ByteLength &&
            BytesEqualAsciiIgnoreCase(AsSpan()[..value.ByteLength], value.AsSpan());
    }

    public bool ContainsAsciiIgnoreCase(AwkText value)
    {
        if (value.ByteLength == 0)
            return true;

        if (value.ByteLength > ByteLength)
            return false;

        var lastStart = ByteLength - value.ByteLength;
        for (var start = 0; start <= lastStart; start++)
        {
            if (BytesEqualAsciiIgnoreCase(AsSpan().Slice(start, value.ByteLength), value.AsSpan()))
                return true;
        }

        return false;
    }

    public bool TryParseNumber(
        AwkNumberParsingMode numberParsingMode,
        bool requireFullText,
        out double number)
    {
        var span = AsSpan();
        if (!TryParseNumberPrefix(span, numberParsingMode, out number, out var end))
            return false;

        if (requireFullText && SkipAsciiWhitespace(span, end) != span.Length)
        {
            number = 0;
            return false;
        }

        return true;
    }

    internal bool TryParseAwkNumber(
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out bool consumedAll)
    {
        var span = AsSpan();
        if (!TryParseNumberPrefix(span, numberParsingMode, out number, out var end))
        {
            number = 0;
            consumedAll = false;
            return false;
        }

        consumedAll = SkipAsciiWhitespace(span, end) == span.Length;
        return true;
    }

    public bool Equals(AwkText? other)
    {
        return other is not null && AsSpan().SequenceEqual(other.AsSpan());
    }

    public override bool Equals(object? obj) =>
        obj is AwkText other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)2166136261;
            for (var index = 0; index < _bytes.Length; index++)
            {
                hash ^= _bytes[index];
                hash *= 16777619;
            }

            return hash;
        }
    }

    public override string ToString() => AsString();

    private void ResolveRuneByteRange(int start, int length, out int byteStart, out int byteEnd)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var span = AsSpan();
        var offset = 0;
        var runeIndex = 0;
        var endRuneIndex = (long)start + length;
        byteStart = -1;
        byteEnd = -1;
        while (true)
        {
            if (runeIndex == start)
                byteStart = offset;

            if (runeIndex == endRuneIndex)
            {
                byteEnd = offset;
                return;
            }

            if (offset == span.Length)
                break;

            var status = Rune.DecodeFromUtf8(span[offset..], out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            offset += consumed;
            runeIndex++;
        }

        if (byteStart < 0)
            throw new ArgumentOutOfRangeException(nameof(start));

        throw new ArgumentOutOfRangeException(nameof(length));
    }

    private static AwkText CreateTrustedOwnedBytes(byte[] bytes, AwkRuntimeCounters counters)
    {
        counters.RecordValueCreated();
        counters.RecordAwkTextAllocation(bytes.Length);
        return new AwkText(bytes);
    }

    internal static byte[] AllocateOwnedBytes(int length, AwkMemoryGovernor governor)
    {
        if (length < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        if (length == 0)
            return [];

        governor.Reserve(length);
        byte[] bytes;
        try
        {
            bytes = new byte[length];
        }
        catch
        {
            governor.ReleaseReserved(length);
            throw;
        }

        try
        {
            governor.CommitReserved(length);
        }
        catch
        {
            governor.ReleaseReserved(length);
            throw;
        }

        governor.ReleaseCommitted(length);
        return bytes;
    }

    private static void RecordOwnedBytesAllocation(int length, AwkMemoryGovernor governor)
    {
        if (length <= 0)
            return;

        governor.Reserve(length);
        try
        {
            governor.CommitReserved(length);
        }
        catch
        {
            governor.ReleaseReserved(length);
            throw;
        }

        governor.ReleaseCommitted(length);
    }

    private static void ThrowIfInvalidUtf8(ReadOnlySpan<byte> bytes)
    {
        var remaining = bytes;
        while (!remaining.IsEmpty)
        {
            var status = Rune.DecodeFromUtf8(remaining, out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);

            remaining = remaining[consumed..];
        }
    }

    private static int CountRunes(ReadOnlySpan<byte> bytes)
    {
        var count = 0;
        var remaining = bytes;
        while (!remaining.IsEmpty)
        {
            var status = Rune.DecodeFromUtf8(remaining, out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            count++;
            remaining = remaining[consumed..];
        }

        return count;
    }

    private static bool BytesEqualAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        for (var index = 0; index < left.Length; index++)
        {
            if (FoldAscii(left[index]) != FoldAscii(right[index]))
                return false;
        }

        return true;
    }

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + 32)
            : value;

    private static int CheckedLength(int left, int right)
    {
        if (left > int.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }

    private static int CheckedProduct(int left, int right)
    {
        if (left > int.MaxValue / right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left * right;
    }

    private static int SkipAsciiWhitespace(ReadOnlySpan<byte> span, int index)
    {
        while (index < span.Length && span[index] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
        {
            index++;
        }

        return index;
    }

    private static bool TryParseNonDecimalNumber(
        ReadOnlySpan<byte> span,
        int start,
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out int end)
    {
        number = 0;
        end = start;
        var index = start;
        var sign = 1;
        var hasExplicitSign = false;
        if (index < span.Length && span[index] is (byte)'+' or (byte)'-')
        {
            hasExplicitSign = true;
            sign = span[index] == (byte)'-' ? -1 : 1;
            index++;
        }

        if (index + 2 < span.Length &&
            span[index] == (byte)'0' &&
            span[index + 1] is (byte)'x' or (byte)'X' &&
            IsHexDigit(span[index + 2]))
        {
            index += 2;
            var value = 0L;
            while (index < span.Length && IsHexDigit(span[index]))
            {
                var digit = HexValue(span[index]);
                if (value > (long.MaxValue - digit) / 16)
                    return false;

                value = (value * 16) + digit;
                index++;
            }

            number = sign * value;
            end = index;
            return true;
        }

        if (numberParsingMode != AwkNumberParsingMode.NonDecimalData ||
            hasExplicitSign ||
            index + 1 >= span.Length ||
            span[index] != (byte)'0' ||
            !IsDecimalDigit(span[index + 1]))
        {
            return false;
        }

        var digitStart = index;
        while (index < span.Length && IsDecimalDigit(span[index]))
            index++;

        if (index < span.Length && span[index] is (byte)'.' or (byte)'e' or (byte)'E')
            return false;

        var octalValue = 0L;
        for (var digitIndex = digitStart + 1; digitIndex < index; digitIndex++)
        {
            var digit = span[digitIndex] - (byte)'0';
            if (digit > 7 || octalValue > (long.MaxValue - digit) / 8)
                return false;

            octalValue = (octalValue * 8) + digit;
        }

        number = octalValue;
        end = index;
        return true;
    }

    private static bool IsDecimalDigit(byte value) =>
        value is >= (byte)'0' and <= (byte)'9';

    private static bool IsHexDigit(byte value) =>
        IsDecimalDigit(value) ||
        value is >= (byte)'a' and <= (byte)'f' ||
        value is >= (byte)'A' and <= (byte)'F';

    private static int HexValue(byte value)
    {
        if (value <= (byte)'9')
            return value - (byte)'0';

        return FoldAscii(value) - (byte)'a' + 10;
    }

    private static bool TryParseNumberPrefix(
        ReadOnlySpan<byte> span,
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out int end)
    {
        var start = SkipAsciiWhitespace(span, 0);

        if (numberParsingMode != AwkNumberParsingMode.Decimal &&
            TryParseNonDecimalNumber(span, start, numberParsingMode, out number, out end))
        {
            return true;
        }

        return TryParseDecimalNumber(span, start, out number, out end);
    }

    private static bool TryParseDecimalNumber(
        ReadOnlySpan<byte> span,
        int start,
        out double number,
        out int end)
    {
        number = 0;
        end = start;
        var index = start;
        if (index < span.Length && span[index] is (byte)'+' or (byte)'-')
            index++;

        var digitCount = 0;
        while (index < span.Length && IsDecimalDigit(span[index]))
        {
            digitCount++;
            index++;
        }

        if (index < span.Length && span[index] == (byte)'.')
        {
            index++;
            while (index < span.Length && IsDecimalDigit(span[index]))
            {
                digitCount++;
                index++;
            }
        }

        if (digitCount == 0)
            return false;

        if (index < span.Length && span[index] is (byte)'e' or (byte)'E')
        {
            var exponentStart = index;
            index++;
            if (index < span.Length && span[index] is (byte)'+' or (byte)'-')
                index++;

            var exponentDigitStart = index;
            while (index < span.Length && IsDecimalDigit(span[index]))
                index++;

            if (exponentDigitStart == index)
                index = exponentStart;
        }

        if (!Utf8Parser.TryParse(span[start..index], out number, out var consumed) || consumed != index - start)
        {
            number = 0;
            return false;
        }

        end = index;
        return true;
    }
}
