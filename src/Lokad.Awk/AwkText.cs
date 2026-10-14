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
        AwkMemoryAccounting governor)
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

        var runeLength = CountRunes(AsSpan(checkpoint), checkpoint);
        checkpoint?.Invoke();
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

    public ReadOnlySpan<byte> AsSpan(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return _concatenation is null
            ? _bytes.AsSpan(_byteOffset, _byteLength)
            : MaterializeConcatenation(_concatenation, checkpoint).AsSpan();
    }

    public ReadOnlyMemory<byte> AsMemory(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return _concatenation is null
            ? _bytes.AsMemory(_byteOffset, _byteLength)
            : MaterializeConcatenation(_concatenation, checkpoint).AsMemory();
    }

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

    public static AwkText FromString(string text, AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        if (text.Length == 0)
            return Empty;

        checkpoint?.Invoke();
        var bytes = AwkUtf8Codec.EncodeOwned(text, counters);
        checkpoint?.Invoke();
        counters.RecordValueCreated();
        counters.RecordAwkTextAllocation(bytes.Length);
        return new AwkText(bytes);
    }

    public static AwkText FromString(
        string text,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        if (text.Length == 0)
            return Empty;

        return FromUtf16(text.AsSpan(), counters, governor, checkpoint);
    }

    internal static AwkText FromUtf16(
        ReadOnlySpan<char> text,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        if (text.IsEmpty)
            return Empty;

        checkpoint?.Invoke();
        var byteCount = AwkUtf8Codec.GetEncodedByteCount(text);
        checkpoint?.Invoke();
        var bytes = AllocateOwnedBytes(byteCount, governor);
        AwkUtf8Codec.Encode(text, bytes, counters);
        checkpoint?.Invoke();
        counters.RecordValueCreated();
        counters.RecordAwkTextAllocation(bytes.Length);
        return new AwkText(bytes);
    }

    public static AwkText FromHostBytes(ReadOnlyMemory<byte> bytes) =>
        FromHostBytes(bytes, AwkRuntimeCounters.Disabled);

    public static AwkText FromHostBytes(ReadOnlyMemory<byte> bytes, AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        if (bytes.IsEmpty)
            return Empty;

        ThrowIfInvalidUtf8(bytes.Span, checkpoint);
        return CopyTrustedBytes(bytes.Span, counters, AwkMemoryAccounting.Disabled, checkpoint);
    }

    public static AwkText FromOwnedBytes(byte[] bytes, AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        if (bytes.Length == 0)
            return Empty;

        ThrowIfInvalidUtf8(bytes, checkpoint);
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
        AwkMemoryAccounting governor)
    {
        if (bytes.Length == 0)
            return Empty;

        RecordOwnedBytesAllocation(bytes.Length, governor);
        return CreateTrustedOwnedBytes(bytes, counters);

        static void RecordOwnedBytesAllocation(int length, AwkMemoryAccounting governor)
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
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        if (bytes.IsEmpty)
            return Empty;

        checkpoint?.Invoke();
        var ownedBytes = AllocateOwnedBytes(bytes.Length, governor);
        CopyBytes(bytes, ownedBytes, checkpoint);
        return FromTrustedOwnedBytes(ownedBytes, counters);
    }

    internal static AwkText CopyHostBytes(
        ReadOnlySpan<byte> bytes,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        ThrowIfInvalidUtf8(bytes, checkpoint);
        return CopyTrustedBytes(bytes, counters, governor, checkpoint);
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

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        counters.RecordAwkTextAsStringCall();
        if (_byteLength == 0)
            return string.Empty;

        var cached = Volatile.Read(ref _cachedString);
        if (cached is not null)
            return cached;

        counters.RecordAwkTextAsStringDecodeMiss();
        var decoded = AwkUtf8Codec.DecodeRuntimeText(AsSpan(checkpoint), counters);
        checkpoint?.Invoke();
        return Interlocked.CompareExchange(ref _cachedString, decoded, null) ?? decoded;
    }

    public int CompareBytes(AwkText other, Action? checkpoint = null) =>
        CompareBytes(AsSpan(checkpoint), other.AsSpan(checkpoint), checkpoint);

    internal static int CompareBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, Action? checkpoint)
    {
        if (checkpoint is null)
            return left.SequenceCompareTo(right);

        var commonLength = Math.Min(left.Length, right.Length);
        for (var offset = 0; offset < commonLength;)
        {
            checkpoint();
            var length = Math.Min(AwkExecutionGovernor.WorkChunkBytes, commonLength - offset);
            var comparison = left.Slice(offset, length).SequenceCompareTo(right.Slice(offset, length));
            if (comparison != 0)
                return comparison;
            offset += length;
        }
        checkpoint();
        return left.Length - right.Length;
    }

    public bool EqualsAscii(string value, Action? checkpoint = null)
    {
        var span = AsSpan(checkpoint);
        if (span.Length != value.Length)
            return false;

        for (var index = 0; index < span.Length; index++)
        {
            if ((index & (AwkExecutionGovernor.WorkChunkBytes - 1)) == 0)
                checkpoint?.Invoke();
            if (value[index] > 0x7F || span[index] != (byte)value[index])
                return false;
        }

        checkpoint?.Invoke();
        return true;
    }

    public AwkText Concat(
        AwkText other,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        if (ByteLength == 0)
            return other;

        if (other.ByteLength == 0)
            return this;

        var length = AwkRuntimeMemory.CheckedAdd(ByteLength, other.ByteLength);
        governor.PreflightAllocation(length);
        counters.RecordValueCreated();
        return new AwkText(
            SnapshotBorrowedText(this, counters, governor, checkpoint),
            SnapshotBorrowedText(other, counters, governor, checkpoint),
            length,
            counters,
            governor);
    }

    public static AwkText Concat(
        ReadOnlySpan<AwkText> parts,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null) =>
        Concat<AwkText, AwkTextPartSelector>(parts, counters, governor, checkpoint);

    public static AwkText Concat(
        ReadOnlySpan<AwkValue> parts,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null) =>
        Concat<AwkValue, AwkValueTextPartSelector>(parts, counters, governor, checkpoint);

    private static AwkText Concat<TPart, TSelector>(
        ReadOnlySpan<TPart> parts,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
        where TSelector : struct, IAwkTextPartSelector<TPart>
    {
        var totalLength = 0;
        var nonEmptyPartCount = 0;
        AwkText? firstNonEmptyPart = null;
        AwkText? secondNonEmptyPart = null;
        for (var index = 0; index < parts.Length; index++)
        {
            if ((index & 1023) == 0)
                checkpoint?.Invoke();
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
                SnapshotBorrowedText(firstNonEmptyPart, counters, governor, checkpoint),
                SnapshotBorrowedText(secondNonEmptyPart, counters, governor, checkpoint),
                totalLength,
                counters,
                governor);
        }

        var bytes = AllocateOwnedBytes(totalLength, governor);
        var destinationOffset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            if ((index & 1023) == 0)
                checkpoint?.Invoke();
            var part = TSelector.Select(parts, index);
            if (part.ByteLength == 0)
                continue;

            var source = part.AsSpan(checkpoint);
            CopyBytes(source, bytes.AsSpan(destinationOffset), checkpoint);
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
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
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
        CopyBytes(AsSpan(checkpoint).Slice(start, length), bytes, checkpoint);
        return FromOwnedBytes(bytes, counters, checkpoint);
    }

    public AwkText CopyRuneSlice(
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null)
    {
        ResolveRuneByteRange(out var byteStart, out var byteEnd);

        if (length == 0)
            return Empty;

        if (byteStart == 0 && byteEnd == ByteLength)
            return this;

        var byteLength = byteEnd - byteStart;
        var bytes = AllocateOwnedBytes(byteLength, governor);
        CopyBytes(AsSpan(checkpoint)[byteStart..byteEnd], bytes, checkpoint);
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

            var span = AsSpan(checkpoint);
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

            checkpoint?.Invoke();
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
            var span = AsSpan(checkpoint);
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

    public int IndexOf(AwkText value, Action? checkpoint = null)
    {
        var position = AsSpan(checkpoint).IndexOf(value.AsSpan(checkpoint));
        checkpoint?.Invoke();
        return position;
    }

    public bool Contains(AwkText value, Action? checkpoint = null) => IndexOf(value, checkpoint) >= 0;

    public bool StartsWith(AwkText value, Action? checkpoint = null) =>
        ByteLength >= value.ByteLength &&
        CompareBytes(AsSpan(checkpoint)[..value.ByteLength], value.AsSpan(checkpoint), checkpoint) == 0;

    public bool EndsWith(AwkText value, Action? checkpoint = null) =>
        ByteLength >= value.ByteLength &&
        CompareBytes(AsSpan(checkpoint)[(ByteLength - value.ByteLength)..], value.AsSpan(checkpoint), checkpoint) == 0;

    public bool TryParseNumber(
        AwkNumberParsingMode numberParsingMode,
        AwkNumberTextExtent textExtent,
        out double number,
        Action? checkpoint = null)
    {
        var span = AsSpan(checkpoint);
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
        var span = AsSpan(checkpoint);
        if (!TryParseNumberPrefix(span, numberParsingMode, out number, out var end, checkpoint))
        {
            number = 0;
            consumedAll = false;
            return false;
        }

        consumedAll = SkipAsciiWhitespace(span, end, checkpoint) == span.Length;
        return true;
    }

    public bool Equals(AwkText? other) => Equals(other, checkpoint: null);

    public bool Equals(AwkText? other, Action? checkpoint)
    {
        checkpoint?.Invoke();
        return other is not null && ByteLength == other.ByteLength && CompareBytes(other, checkpoint) == 0;
    }

    public override bool Equals(object? obj) =>
        obj is AwkText other && Equals(other);

    public override int GetHashCode() => GetHashCode(checkpoint: null);

    internal int GetHashCode(Action? checkpoint) =>
        AwkByteFacts.GetOrdinalHashCode(AsSpan(checkpoint), checkpoint);

    public override string ToString() => AsString();

    internal bool IsAscii(out int inspectedByteCount, Action? checkpoint = null)
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

        var span = AsSpan(checkpoint);
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

    private byte[] MaterializeConcatenation(AwkTextConcatenation concatenation, Action? checkpoint)
    {
        var cached = Volatile.Read(ref concatenation.MaterializedBytes);
        if (cached is not null)
            return cached;

        var lockTaken = false;
        try
        {
            if (checkpoint is null)
                Monitor.Enter(concatenation, ref lockTaken);
            else
            {
                while (!Monitor.TryEnter(concatenation, millisecondsTimeout: 50))
                    checkpoint();
                lockTaken = true;
                checkpoint();
            }
            cached = concatenation.MaterializedBytes;
            if (cached is not null)
                return cached;

            // Keep the allocation live while copying; cancellation releases the lease
            // and leaves the retained parts available for an independent retry.
            var governor = concatenation.Governor;
            governor.Reserve(ByteLength);
            byte[] bytes;
            try
            {
                bytes = new byte[ByteLength];
                governor.CommitReserved(ByteLength);
            }
            catch
            {
                governor.ReleaseReserved(ByteLength);
                throw;
            }
            try
            {
                CopyConcatenationTo(bytes);
                checkpoint?.Invoke();
                concatenation.Counters.RecordConcatenationBytesCopied(ByteLength);
                concatenation.Counters.RecordAwkTextAllocation(ByteLength);
                Volatile.Write(ref concatenation.MaterializedBytes, bytes);
                concatenation.LeftPart = null;
                concatenation.RightPart = null;
                return bytes;
            }
            finally
            {
                governor.ReleaseCommitted(ByteLength);
            }
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(concatenation);
        }

        void CopyConcatenationTo(Span<byte> destination)
        {
            var pending = ArrayPool<AwkText>.Shared.Rent(16);
            var pendingCount = 1;
            var destinationOffset = 0;
            pending[0] = this;
            try
            {
                var visited = 0;
                while (pendingCount > 0)
                {
                    if ((visited++ & 1023) == 0)
                        checkpoint?.Invoke();
                    var current = pending[--pendingCount];
                    var currentConcatenation = current._concatenation;
                    if (currentConcatenation is null)
                    {
                        var source = current._bytes.AsSpan(current._byteOffset, current._byteLength);
                        CopyBytes(source, destination[destinationOffset..], checkpoint);
                        destinationOffset += source.Length;
                        continue;
                    }

                    var materialized = Volatile.Read(ref currentConcatenation.MaterializedBytes);
                    if (materialized is not null)
                    {
                        CopyBytes(materialized, destination[destinationOffset..], checkpoint);
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

                        CopyBytes(materialized, destination[destinationOffset..], checkpoint);
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
                try
                {
                    checkpoint?.Invoke();
                    pending.AsSpan(0, pendingCount).CopyTo(expanded);
                    checkpoint?.Invoke();
                }
                catch
                {
                    ArrayPool<AwkText>.Shared.Return(expanded, clearArray: true);
                    throw;
                }
                ArrayPool<AwkText>.Shared.Return(pending, clearArray: true);
                pending = expanded;
            }
        }
    }

    private static AwkText SnapshotBorrowedText(
        AwkText text,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        Action? checkpoint = null) =>
        text._isBorrowed
            ? CopyTrustedBytes(text.AsSpan(checkpoint), counters, governor, checkpoint)
            : text;

    private sealed class AwkTextConcatenation
    {
        public AwkTextConcatenation(
            AwkText left,
            AwkText right,
            AwkRuntimeCounters counters,
            AwkMemoryAccounting governor)
        {
            LeftPart = left;
            RightPart = right;
            Counters = counters;
            Governor = governor;
        }

        public AwkRuntimeCounters Counters { get; }

        public AwkMemoryAccounting Governor { get; }

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

    internal static byte[] AllocateOwnedBytes(int length, AwkMemoryAccounting governor)
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

    private static void ThrowIfInvalidUtf8(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        var remaining = bytes;
        long nextCheckpoint = 0;
        while (!remaining.IsEmpty)
        {
            var offset = bytes.Length - remaining.Length;
            if (offset >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)offset + AwkExecutionGovernor.WorkChunkBytes;
            }
            var status = Rune.DecodeFromUtf8(remaining, out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);

            remaining = remaining[consumed..];
        }
        checkpoint?.Invoke();
    }

    internal static void CopyBytes(ReadOnlySpan<byte> source, Span<byte> destination, Action? checkpoint)
    {
        if (checkpoint is null)
        {
            source.CopyTo(destination);
            return;
        }
        checkpoint();
        // Preserve memmove behavior for callers passing overlapping builder views.
        if (source.Overlaps(destination))
            source.CopyTo(destination);
        else
        {
            for (var offset = 0; offset < source.Length;)
            {
                checkpoint();
                var length = Math.Min(AwkExecutionGovernor.WorkChunkBytes, source.Length - offset);
                source.Slice(offset, length).CopyTo(destination.Slice(offset, length));
                offset += length;
            }
        }
        checkpoint();
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
