using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Threading;

namespace Lokad.Awk;

internal sealed class AwkText : IEquatable<AwkText>
{
    private const int UnknownTextFacts = 0;
    private const int KnownNonAsciiWithoutRuneCount = -1;
    private const int RuneCheckpointStride = 64;
    private readonly byte[] _bytes;
    private readonly int _byteOffset;
    private readonly int _byteLength;
    private readonly bool _isBorrowed;
    private string? _cachedString;
    // Zero means unclassified, -1 means non-ASCII with an unknown rune count,
    // and positive values store the rune count plus one.
    private int _textFacts;
    private int[]? _runeByteCheckpoints;

    private AwkText(byte[] bytes)
        : this(bytes, 0, bytes.Length, isBorrowed: false)
    {
    }

    private AwkText(byte[] bytes, int byteOffset, int byteLength, bool isBorrowed)
    {
        _bytes = bytes;
        _byteOffset = byteOffset;
        _byteLength = byteLength;
        _isBorrowed = isBorrowed;
    }

    public static AwkText Empty { get; } = new([]);

    public int ByteLength => _byteLength;

    public int RuneLength
    {
        get
        {
            var cached = Volatile.Read(ref _textFacts);
            if (cached > UnknownTextFacts)
                return cached - 1;

            var runeLength = CountRunes(AsSpan());
            Volatile.Write(ref _textFacts, runeLength + 1);
            return runeLength;
        }
    }

    public ReadOnlySpan<byte> AsSpan() => _bytes.AsSpan(_byteOffset, _byteLength);

    public ReadOnlyMemory<byte> AsMemory() => _bytes.AsMemory(_byteOffset, _byteLength);

    public bool ContainsNonAscii(AwkRuntimeCounters counters)
    {
        var isAscii = IsAscii(out var inspectedByteCount);
        counters.RecordRegexPolicyInputBytesInspected(inspectedByteCount);
        return !isAscii;
    }

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

    public static AwkText FromOwnedBytes(byte[] bytes, AwkRuntimeCounters counters)
    {
        if (bytes.Length == 0)
            return Empty;

        ThrowIfInvalidUtf8(bytes);
        return FromTrustedOwnedBytes(bytes, counters);
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

    internal static AwkText CopyTrustedBytes(
        ReadOnlySpan<byte> bytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (bytes.IsEmpty)
            return Empty;

        var ownedBytes = AllocateOwnedBytes(bytes.Length, governor);
        bytes.CopyTo(ownedBytes);
        return FromTrustedOwnedBytes(ownedBytes, counters);
    }

    internal static AwkText CopyHostBytes(
        ReadOnlySpan<byte> bytes,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        ThrowIfInvalidUtf8(bytes);
        return CopyTrustedBytes(bytes, counters, governor);
    }

    internal static AwkText FromTrustedBorrowedBytes(
        byte[] bytes,
        int byteOffset,
        int byteLength)
    {
        // The caller owns the backing buffer and must discard this view before mutating it.
        if (byteOffset < 0 || byteOffset > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(byteOffset));

        if (byteLength < 0 || byteOffset > bytes.Length - byteLength)
            throw new ArgumentOutOfRangeException(nameof(byteLength));

        return byteLength == 0
            ? Empty
            : new AwkText(bytes, byteOffset, byteLength, isBorrowed: true);
    }

    public string AsString() =>
        AsString(AwkRuntimeCounters.Disabled);

    public string AsString(AwkRuntimeCounters counters)
    {
        counters.RecordAwkTextAsStringCall();
        if (_byteLength == 0)
            return string.Empty;

        var cached = _cachedString;
        if (cached is not null)
            return cached;

        counters.RecordAwkTextAsStringDecodeMiss();
        var decoded = AwkUtf8Codec.DecodeRuntimeText(AsSpan(), counters);
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

    public AwkText Concat(
        AwkText other,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (ByteLength == 0)
            return other;

        if (other.ByteLength == 0)
            return this;

        var length = AwkRuntimeMemory.CheckedAdd(ByteLength, other.ByteLength);
        var bytes = AllocateOwnedBytes(length, governor);
        AsSpan().CopyTo(bytes);
        other.AsSpan().CopyTo(bytes.AsSpan(ByteLength));
        counters.RecordConcatenationBytesCopied(length);
        return FromTrustedOwnedBytes(bytes, counters);
    }

    public static AwkText Concat(
        ReadOnlySpan<AwkText> parts,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        var totalLength = 0;
        var nonEmptyPartCount = 0;
        AwkText? singleNonEmptyPart = null;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part.ByteLength == 0)
                continue;

            totalLength = AwkRuntimeMemory.CheckedAdd(totalLength, part.ByteLength);
            nonEmptyPartCount++;
            if (nonEmptyPartCount == 1)
                singleNonEmptyPart = part;
        }

        if (nonEmptyPartCount == 0)
            return Empty;

        if (nonEmptyPartCount == 1)
        {
            return singleNonEmptyPart ??
                throw new InvalidOperationException("A non-empty concatenation must retain its only part.");
        }

        var bytes = AllocateOwnedBytes(totalLength, governor);
        var destination = bytes.AsSpan();
        var position = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            part.AsSpan().CopyTo(destination[position..]);
            position += part.ByteLength;
        }

        counters.RecordConcatenationBytesCopied(totalLength);
        return FromTrustedOwnedBytes(bytes, counters);
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

        if (!_isBorrowed && start == 0 && length == ByteLength)
            return this;

        var bytes = AllocateOwnedBytes(length, governor);
        AsSpan().Slice(start, length).CopyTo(bytes);
        return FromOwnedBytes(bytes, counters);
    }

    public AwkText CopyRuneSlice(
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        ResolveRuneByteRange(start, length, counters, governor, out var byteStart, out var byteEnd);

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
            var span = AsSpan();
            for (var index = 0; index < span.Length; index++)
            {
                hash ^= span[index];
                hash *= 16777619;
            }

            return hash;
        }
    }

    public override string ToString() => AsString();

    private void ResolveRuneByteRange(
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        out int byteStart,
        out int byteEnd)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var endRuneIndex = (long)start + length;
        if (IsAscii(out var inspectedByteCount))
        {
            counters.RecordRuneSliceBytesScanned(inspectedByteCount);
            if (start > ByteLength)
                throw new ArgumentOutOfRangeException(nameof(start));

            if (endRuneIndex > ByteLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            byteStart = start;
            byteEnd = (int)endRuneIndex;
            return;
        }

        counters.RecordRuneSliceBytesScanned(inspectedByteCount);
        var checkpoints = GetRuneByteCheckpoints(counters, governor);
        var runeLength = RuneLength;
        if (start > runeLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (endRuneIndex > runeLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        byteStart = ResolveRuneByteOffset(start, checkpoints, counters);
        byteEnd = ResolveRuneByteOffset((int)endRuneIndex, checkpoints, counters);
    }

    private bool IsAscii(out int inspectedByteCount)
    {
        var cached = Volatile.Read(ref _textFacts);
        if (cached > UnknownTextFacts)
        {
            inspectedByteCount = 0;
            return cached - 1 == ByteLength;
        }

        if (cached == KnownNonAsciiWithoutRuneCount)
        {
            inspectedByteCount = 0;
            return false;
        }

        var span = AsSpan();
        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] <= 0x7F)
                continue;

            inspectedByteCount = index + 1;
            _ = Interlocked.CompareExchange(
                ref _textFacts,
                KnownNonAsciiWithoutRuneCount,
                comparand: UnknownTextFacts);
            return false;
        }

        inspectedByteCount = span.Length;
        Volatile.Write(ref _textFacts, ByteLength + 1);
        return true;
    }

    private int[] GetRuneByteCheckpoints(
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        var cached = Volatile.Read(ref _runeByteCheckpoints);
        if (cached is not null)
            return cached;

        var checkpointCount = checked((ByteLength / RuneCheckpointStride) + 2);
        var checkpointBytes = checked(checkpointCount * sizeof(int));
        int[] created;
        using (governor.ReserveTransient(checkpointBytes))
            created = new int[checkpointCount];

        var span = AsSpan();
        var byteOffset = 0;
        var runeIndex = 0;
        while (byteOffset < span.Length)
        {
            var status = Rune.DecodeFromUtf8(span[byteOffset..], out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            byteOffset += consumed;
            runeIndex++;
            if (runeIndex % RuneCheckpointStride == 0)
                created[runeIndex / RuneCheckpointStride] = byteOffset;
        }

        counters.RecordRuneSliceBytesScanned(span.Length);
        counters.RecordRuneSliceIndexAllocation(checkpointBytes);
        Volatile.Write(ref _textFacts, runeIndex + 1);
        return Interlocked.CompareExchange(ref _runeByteCheckpoints, created, null) ?? created;
    }

    private int ResolveRuneByteOffset(
        int runeIndex,
        int[] checkpoints,
        AwkRuntimeCounters counters)
    {
        var checkpointIndex = runeIndex / RuneCheckpointStride;
        var currentRuneIndex = checkpointIndex * RuneCheckpointStride;
        var byteOffset = checkpoints[checkpointIndex];
        var span = AsSpan();
        var scannedByteCount = 0;
        while (currentRuneIndex < runeIndex)
        {
            var status = Rune.DecodeFromUtf8(span[byteOffset..], out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            byteOffset += consumed;
            scannedByteCount += consumed;
            currentRuneIndex++;
        }

        counters.RecordRuneSliceBytesScanned(scannedByteCount);
        return byteOffset;
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

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + 32)
            : value;

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
