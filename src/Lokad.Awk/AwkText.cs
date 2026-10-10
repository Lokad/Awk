using System;
using System.Buffers;
using System.Text;
using System.Threading;

namespace Lokad.Awk;

internal sealed class AwkText : IEquatable<AwkText>
{
    private const int UnknownTextFacts = 0;
    private const int KnownNonAsciiWithoutRuneCount = -1;
    private const int RuneCheckpointStride = AwkRuntimeMemory.TextRuneCheckpointStride;
    private readonly byte[] _bytes;
    private readonly int _byteOffset;
    private readonly int _byteLength;
    private readonly bool _isBorrowed;
    private readonly AwkTextConcatenation? _concatenation;
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
        EstimatedRetainedMemoryBytes = AwkRuntimeMemory.EstimateTextBytes(byteLength);
    }

    private AwkText(
        AwkText left,
        AwkText right,
        int byteLength,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        _bytes = [];
        _byteLength = byteLength;
        _concatenation = new AwkTextConcatenation(left, right, counters, governor);
        EstimatedRetainedMemoryBytes = AwkRuntimeMemory.EstimateConcatenatedTextBytes(left, right, byteLength);
    }

    public static AwkText Empty { get; } = new([]);

    public int ByteLength => _byteLength;

    public long EstimatedRetainedMemoryBytes { get; }

    public int RuneLength => GetRuneLength();

    internal int GetRuneLength(Action? checkpoint = null)
    {
        var cached = Volatile.Read(ref _textFacts);
        if (cached > UnknownTextFacts)
            return cached - 1;

        var runeLength = CountRunes(AsSpan(), checkpoint);
        Volatile.Write(ref _textFacts, runeLength + 1);
        return runeLength;
    }

    internal static int CountRunes(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        var count = 0;
        var offset = 0;
        long nextCheckpoint = 0;
        while (offset < bytes.Length)
        {
            if (offset >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)offset + AwkExecutionGovernor.WorkChunkBytes;
            }
            if (Rune.DecodeFromUtf8(bytes[offset..], out _, out var consumed) != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            count++;
            offset += consumed;
        }
        return count;
    }

    public ReadOnlySpan<byte> AsSpan() =>
        _concatenation is null
            ? _bytes.AsSpan(_byteOffset, _byteLength)
            : MaterializeConcatenation(_concatenation).AsSpan();

    public ReadOnlyMemory<byte> AsMemory() =>
        _concatenation is null
            ? _bytes.AsMemory(_byteOffset, _byteLength)
            : MaterializeConcatenation(_concatenation).AsMemory();

    public bool ContainsNonAscii(AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        var isAscii = IsAscii(out var inspectedByteCount, checkpoint);
        counters.RecordRegexPolicyInputBytesInspected(inspectedByteCount);
        return !isAscii;
    }

    public static AwkText FromSourceLiteralString(string text) =>
        FromString(text, AwkRuntimeCounters.Disabled);

    public static AwkText FromString(string text) =>
        FromString(text, AwkRuntimeCounters.Disabled);

    public static AwkText FromString(string text, AwkRuntimeCounters counters)
    {
        if (text.Length == 0)
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
        if (text.Length == 0)
            return Empty;

        return FromUtf16(text.AsSpan(), counters, governor);
    }

    internal static AwkText FromUtf16(
        ReadOnlySpan<char> text,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
    {
        if (text.IsEmpty)
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

        static void RecordOwnedBytesAllocation(int length, AwkMemoryGovernor governor)
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
        governor.PreflightAllocation(length);
        counters.RecordValueCreated();
        return new AwkText(
            SnapshotBorrowedText(this, counters, governor),
            SnapshotBorrowedText(other, counters, governor),
            length,
            counters,
            governor);
    }

    public static AwkText Concat(
        ReadOnlySpan<AwkText> parts,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        Concat<AwkText, AwkTextPartSelector>(parts, counters, governor);

    public static AwkText Concat(
        ReadOnlySpan<AwkValue> parts,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        Concat<AwkValue, AwkValueTextPartSelector>(parts, counters, governor);

    private static AwkText Concat<TPart, TSelector>(
        ReadOnlySpan<TPart> parts,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
        where TSelector : struct, IAwkTextPartSelector<TPart>
    {
        var totalLength = 0;
        var nonEmptyPartCount = 0;
        AwkText? firstNonEmptyPart = null;
        AwkText? secondNonEmptyPart = null;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = TSelector.Select(parts, index);
            if (part.ByteLength == 0)
                continue;

            totalLength = AwkRuntimeMemory.CheckedAdd(totalLength, part.ByteLength);
            nonEmptyPartCount++;
            if (nonEmptyPartCount == 1)
                firstNonEmptyPart = part;
            else if (nonEmptyPartCount == 2)
                secondNonEmptyPart = part;
        }

        if (nonEmptyPartCount == 0)
            return Empty;

        if (nonEmptyPartCount == 1)
        {
            return firstNonEmptyPart ??
                throw new InvalidOperationException("A non-empty concatenation must retain its only part.");
        }

        governor.PreflightAllocation(totalLength);
        if (nonEmptyPartCount == 2)
        {
            if (firstNonEmptyPart is null || secondNonEmptyPart is null)
                throw new InvalidOperationException("A two-part concatenation must retain both parts.");

            counters.RecordValueCreated();
            return new AwkText(
                SnapshotBorrowedText(firstNonEmptyPart, counters, governor),
                SnapshotBorrowedText(secondNonEmptyPart, counters, governor),
                totalLength,
                counters,
                governor);
        }

        var bytes = AllocateOwnedBytes(totalLength, governor);
        var destinationOffset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = TSelector.Select(parts, index);
            if (part.ByteLength == 0)
                continue;

            var source = part.AsSpan();
            source.CopyTo(bytes.AsSpan(destinationOffset));
            destinationOffset += source.Length;
        }

        counters.RecordConcatenationBytesCopied(totalLength);
        return FromTrustedOwnedBytes(bytes, counters);
    }

    private interface IAwkTextPartSelector<TPart>
    {
        /// <summary>Returns the byte-native text represented by one concatenation input.</summary>
        static abstract AwkText Select(ReadOnlySpan<TPart> parts, int index);
    }

    private readonly struct AwkTextPartSelector : IAwkTextPartSelector<AwkText>
    {
        public static AwkText Select(ReadOnlySpan<AwkText> parts, int index) =>
            parts[index];
    }

    private readonly struct AwkValueTextPartSelector : IAwkTextPartSelector<AwkValue>
    {
        public static AwkText Select(ReadOnlySpan<AwkValue> parts, int index) =>
            parts[index].ByteText;
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
        AwkMemoryGovernor governor,
        Action? checkpoint = null)
    {
        ResolveRuneByteRange(out var byteStart, out var byteEnd);

        if (length == 0)
            return Empty;

        if (byteStart == 0 && byteEnd == ByteLength)
            return this;

        var byteLength = byteEnd - byteStart;
        var bytes = AllocateOwnedBytes(byteLength, governor);
        AsSpan()[byteStart..byteEnd].CopyTo(bytes);
        return FromTrustedOwnedBytes(bytes, counters);

        void ResolveRuneByteRange(out int resolvedByteStart, out int resolvedByteEnd)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            var endRuneIndex = (long)start + length;
            if (IsAscii(out var inspectedByteCount, checkpoint))
            {
                counters.RecordRuneSliceBytesScanned(inspectedByteCount);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(start, ByteLength);

                if (endRuneIndex > ByteLength)
                    throw new ArgumentOutOfRangeException(nameof(length));

                resolvedByteStart = start;
                resolvedByteEnd = (int)endRuneIndex;
                return;
            }

            counters.RecordRuneSliceBytesScanned(inspectedByteCount);
            var checkpoints = GetRuneByteCheckpoints();
            var runeLength = GetRuneLength(checkpoint);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start, runeLength);

            if (endRuneIndex > runeLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            resolvedByteStart = ResolveRuneByteOffset(start, checkpoints);
            resolvedByteEnd = ResolveRuneByteOffset((int)endRuneIndex, checkpoints);
        }

        int[] GetRuneByteCheckpoints()
        {
            var cached = Volatile.Read(ref _runeByteCheckpoints);
            if (cached is not null)
                return cached;

            var checkpointCount = checked((ByteLength / RuneCheckpointStride) + 2);
            var checkpointBytes = checked(checkpointCount * sizeof(int));
            using var checkpointLease = governor.ReserveTransient(checkpointBytes);
            var created = new int[checkpointCount];

            var span = AsSpan();
            var byteOffset = 0;
            var runeIndex = 0;
            long nextCheckpoint = 0;
            while (byteOffset < span.Length)
            {
                if (byteOffset >= nextCheckpoint)
                {
                    checkpoint?.Invoke();
                    nextCheckpoint = (long)byteOffset + AwkExecutionGovernor.WorkChunkBytes;
                }
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

        int ResolveRuneByteOffset(int runeIndex, int[] checkpoints)
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
        AwkNumberTextExtent textExtent,
        out double number,
        Action? checkpoint = null)
    {
        var span = AsSpan();
        if (!TryParseNumberPrefix(span, numberParsingMode, out number, out var end, checkpoint))
            return false;

        if (textExtent is AwkNumberTextExtent.EntireText && SkipAsciiWhitespace(span, end, checkpoint) != span.Length)
        {
            number = 0;
            return false;
        }

        return true;
    }

    internal bool TryParseAwkNumber(
        AwkNumberParsingMode numberParsingMode,
        out double number,
        out bool consumedAll,
        Action? checkpoint = null)
    {
        var span = AsSpan();
        if (!TryParseNumberPrefix(span, numberParsingMode, out number, out var end, checkpoint))
        {
            number = 0;
            consumedAll = false;
            return false;
        }

        consumedAll = SkipAsciiWhitespace(span, end, checkpoint) == span.Length;
        return true;
    }

    public bool Equals(AwkText? other)
    {
        return other is not null && AsSpan().SequenceEqual(other.AsSpan());
    }

    public override bool Equals(object? obj) =>
        obj is AwkText other && Equals(other);

    public override int GetHashCode() => AwkByteFacts.GetOrdinalHashCode(AsSpan());

    public override string ToString() => AsString();

    private bool IsAscii(out int inspectedByteCount, Action? checkpoint = null)
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
            if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                checkpoint?.Invoke();
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

    private byte[] MaterializeConcatenation(AwkTextConcatenation concatenation)
    {
        var cached = Volatile.Read(ref concatenation.MaterializedBytes);
        if (cached is not null)
            return cached;

        lock (concatenation)
        {
            cached = concatenation.MaterializedBytes;
            if (cached is not null)
                return cached;

            var bytes = AllocateOwnedBytes(ByteLength, concatenation.Governor);
            CopyConcatenationTo(bytes);
            concatenation.Counters.RecordConcatenationBytesCopied(ByteLength);
            concatenation.Counters.RecordAwkTextAllocation(ByteLength);
            Volatile.Write(ref concatenation.MaterializedBytes, bytes);
            concatenation.LeftPart = null;
            concatenation.RightPart = null;
            return bytes;
        }

        void CopyConcatenationTo(Span<byte> destination)
        {
            var pending = ArrayPool<AwkText>.Shared.Rent(16);
            var pendingCount = 1;
            var destinationOffset = 0;
            pending[0] = this;
            try
            {
                while (pendingCount > 0)
                {
                    var current = pending[--pendingCount];
                    var currentConcatenation = current._concatenation;
                    if (currentConcatenation is null)
                    {
                        var source = current._bytes.AsSpan(current._byteOffset, current._byteLength);
                        source.CopyTo(destination[destinationOffset..]);
                        destinationOffset += source.Length;
                        continue;
                    }

                    var materialized = Volatile.Read(ref currentConcatenation.MaterializedBytes);
                    if (materialized is not null)
                    {
                        materialized.CopyTo(destination[destinationOffset..]);
                        destinationOffset += materialized.Length;
                        continue;
                    }

                    var left = currentConcatenation.LeftPart;
                    var right = currentConcatenation.RightPart;
                    if (left is null || right is null)
                    {
                        materialized = Volatile.Read(ref currentConcatenation.MaterializedBytes);
                        if (materialized is null)
                            throw new InvalidOperationException("Concatenated text lost both its parts and materialized bytes.");

                        materialized.CopyTo(destination[destinationOffset..]);
                        destinationOffset += materialized.Length;
                        continue;
                    }

                    EnsurePendingCapacity(2);
                    pending[pendingCount++] = right;
                    pending[pendingCount++] = left;
                }

                if (destinationOffset != destination.Length)
                    throw new InvalidOperationException("Concatenated text length does not match its retained parts.");
            }
            finally
            {
                ArrayPool<AwkText>.Shared.Return(pending, clearArray: true);
            }

            void EnsurePendingCapacity(int additionalCount)
            {
                if (pendingCount <= pending.Length - additionalCount)
                    return;

                var expanded = ArrayPool<AwkText>.Shared.Rent(
                    AwkRuntimeMemory.CheckedAdd(pendingCount, additionalCount));
                pending.AsSpan(0, pendingCount).CopyTo(expanded);
                ArrayPool<AwkText>.Shared.Return(pending, clearArray: true);
                pending = expanded;
            }
        }
    }

    private static AwkText SnapshotBorrowedText(
        AwkText text,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        text._isBorrowed
            ? CopyTrustedBytes(text.AsSpan(), counters, governor)
            : text;

    private sealed class AwkTextConcatenation
    {
        public AwkTextConcatenation(
            AwkText left,
            AwkText right,
            AwkRuntimeCounters counters,
            AwkMemoryGovernor governor)
        {
            LeftPart = left;
            RightPart = right;
            Counters = counters;
            Governor = governor;
        }

        public AwkRuntimeCounters Counters { get; }

        public AwkMemoryGovernor Governor { get; }

        public AwkText? LeftPart { get; set; }

        public AwkText? RightPart { get; set; }

        public byte[]? MaterializedBytes;
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

    private static int SkipAsciiWhitespace(ReadOnlySpan<byte> span, int index, Action? checkpoint) =>
        AwkNumberParser.SkipWhitespace(span, index, checkpoint);

    private static bool TryParseNumberPrefix(ReadOnlySpan<byte> span, AwkNumberParsingMode mode,
        out double number, out int end, Action? checkpoint)
    {
        var parsed = AwkNumberParser.Parse(span, mode, checkpoint);
        parsed.ThrowIfError();
        number = parsed.Number;
        end = parsed.End;
        return parsed.Status == AwkNumberParseStatus.Number;
    }
}
