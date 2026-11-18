using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal readonly struct AwkText : IEquatable<AwkText>
{
    private const int UnknownTextFacts = 0;
    private const int KnownNonAsciiWithoutRuneCount = -1;
    private const int RuneCheckpointStride = AwkRuntimeMemory.TextRuneCheckpointStride;
    // Owned short ASCII needs neither scalar indexing nor a mandatory cache object.
    // Literal/string ingress retains caches; managed boundaries prepare them explicitly.
    private const int CompactAsciiByteLength = 32;
    // Scalar/slot payloads retain this backing directly, without boxing the view.
    // Default is empty text; storage owns bytes and lazily computed text facts.
    private readonly object? _backing;
    private static readonly TextStorage EmptyStorage = new([]);

    private TextStorage Storage => _backing switch
    {
        TextStorage storage => storage,
        null => EmptyStorage,
        _ => throw new InvalidOperationException("Compact text has no cache storage.")
    };
    private byte[] OwnedBytes => _backing is TextStorage storage ? storage.Bytes : (byte[]?)_backing ?? [];
    private AwkTextConcatenation? _concatenation => (_backing as TextStorage)?.Concatenation;
    private ref int _textFacts => ref Storage.TextFacts;
    private ref int[]? _runeByteCheckpoints => ref Storage.RuneByteCheckpoints;

    internal object? Backing => _backing;
    internal bool RequiresStringCache => _backing is not (null or TextStorage);
    // Restore only backing produced by this type. Value/cell roles exclude their
    // non-text payloads before reaching this boundary; default remains empty.
    internal static AwkText FromBacking(object? backing) => new(backing);
    internal bool HasSameStorage(AwkText other) => ReferenceEquals(_backing, other._backing);

    private AwkText(object? backing)
    {
        System.Diagnostics.Debug.Assert(backing is null or TextStorage or byte[]);
        _backing = backing;
    }

    private AwkText(byte[] bytes)
    {
        _backing = new TextStorage(bytes);
    }

    public static AwkText Empty => default;

    // Private construction admits only cached storage, owned bytes or default empty.
    // Select metadata first so its access does not invoke the CLR's array-type test.
    public int ByteLength => _backing is TextStorage storage ? storage.ByteLength :
        ((byte[]?)_backing)?.Length ?? 0;

    internal bool HasAvailableBytes => _concatenation is null ||
        _concatenation.MaterializedBytes is not null;

    // Keep the conservative cache allowance with the actual owner. Promotion at
    // a managed boundary consumes that admitted capacity, without a new lease.
    public long EstimatedRetainedMemoryBytes => _backing is TextStorage storage
        ? storage.EstimatedRetainedMemoryBytes :
        AwkRuntimeMemory.EstimateTextBytes(((byte[]?)_backing)?.Length ?? 0);

    public int RuneLength => GetRuneLength();

    internal int GetRuneLength(Action? checkpoint = null)
    {
        var storage = _backing as TextStorage;
        if (storage is null && _backing is byte[] bytes)
            return bytes.Length;
        storage ??= EmptyStorage;
        var cached = storage.TextFacts;
        if (cached > UnknownTextFacts)
            return cached - 1;

        var runeLength = CountRunes(AsSpan(checkpoint), checkpoint);
        checkpoint?.Invoke();
        storage.TextFacts = runeLength + 1;
        return runeLength;
    }

    internal static int CountRunes(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        var count = 0;
        var offset = 0;
        while (offset < bytes.Length)
        {
            checkpoint?.Invoke();
            var chunkLength = Math.Min(bytes.Length - offset, AwkRuntimeLimits.CancellationCheckBytes);
            if (Ascii.IsValid(bytes.Slice(offset, chunkLength)))
            {
                count += chunkLength;
                offset += chunkLength;
                continue;
            }
            var end = offset + chunkLength;
            do
            {
                if (Rune.DecodeFromUtf8(bytes[offset..], out _, out var consumed) != OperationStatus.Done)
                    throw new InvalidOperationException("AwkText contains invalid UTF-8.");
                count++;
                offset += consumed;
            } while (offset < end);
        }
        return count;
    }

    public ReadOnlySpan<byte> AsSpan(Action? checkpoint = null)
    {
        if (_backing is TextStorage storage)
        {
            if (storage.Concatenation is not { } concatenation) return storage.Bytes;
            // Ready bytes need no callback. Flattening can allocate and
            // copy, so retain its entry check as well as its bounded work checks.
            checkpoint?.Invoke();
            return MaterializeConcatenation(concatenation, checkpoint);
        }
        return ((byte[]?)_backing).AsSpan();
    }

    public ReadOnlyMemory<byte> AsMemory(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (_backing is TextStorage storage)
            return storage.Concatenation is { } concatenation
                ? MaterializeConcatenation(concatenation, checkpoint) : storage.Bytes;
        return ((byte[]?)_backing).AsMemory();
    }

    public static AwkText FromSourceLiteralString(string text) => FromString(text);

    public static AwkText FromString(string text, Action? checkpoint = null)
    {
        if (text.Length == 0)
            return Empty;

        checkpoint?.Invoke();
        var bytes = AwkUtf8Codec.EncodeOwned(text);
        checkpoint?.Invoke();
        return FromEncodedStringBytes(bytes);
    }

    // Encoded managed strings retain lazy cache storage even for short ASCII.
    internal static AwkText FromEncodedStringBytes(byte[] bytes) =>
        bytes.Length == 0 ? Empty : new AwkText(bytes);

    // The caller owns valid UTF-8 and can supply an existing ASCII proof.
    internal static AwkText FromTrustedOwnedBytes(byte[] bytes, bool knownAscii = false)
    {
        if (bytes.Length == 0)
            return Empty;
        if (bytes.Length <= CompactAsciiByteLength && (knownAscii || Ascii.IsValid(bytes)))
            return new AwkText((object)bytes);
        var text = new AwkText(bytes);
        if (knownAscii && text._backing is TextStorage storage)
            storage.TextFacts = bytes.Length + 1;
        return text;
    }

    // The caller has already admitted the destination.
    internal static byte[] CopyFundedBytes(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        if (bytes.IsEmpty)
            return [];

        checkpoint?.Invoke();
        var ownedBytes = new byte[bytes.Length];
        CopyBytes(bytes, ownedBytes, checkpoint);
        return ownedBytes;
    }

    public string AsString(Action? checkpoint = null)
    {
        var text = PrepareDecodedString(out var decoded, checkpoint);
        if (decoded) PublishDecodedString(text, checkpoint);
        return text;
    }

    // A host-boundary preparation has already decoded and funded this cache.
    // Reading it during the pending operation must not repeat conversion work.
    internal string PreparedString => ByteLength == 0 ? string.Empty :
        _backing is TextStorage storage && storage.CachedString is { } prepared
            ? prepared : throw new InvalidOperationException("Text must be decoded before its prepared string is used.");

    // The caller retains the returned cache owner. Its ordinary text estimate
    // already funds metadata and decoding, even when the current backing is compact.
    internal AwkText WithStringCache()
    {
        var backing = _backing;
        return backing is null or TextStorage ? this : new AwkText((byte[])backing);
    }

    internal string PrepareDecodedString(out bool decoded, Action? checkpoint)
    {
        decoded = false;
        checkpoint?.Invoke();

        if (ByteLength == 0)
            return string.Empty;

        if (_backing is TextStorage { CachedString: { } cached })
            return cached;

        decoded = true;
        return AwkUtf8Codec.DecodeRuntimeText(_backing is TextStorage
            ? AsSpan(checkpoint) : ((byte[]?)_backing).AsSpan());
    }

    internal void PublishDecodedString(string text, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (_backing is TextStorage storage) storage.CachedString = text;
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
            var length = Math.Min(AwkRuntimeLimits.CancellationCheckBytes, commonLength - offset);
            var comparison = left.Slice(offset, length).SequenceCompareTo(right.Slice(offset, length));
            if (comparison != 0)
                return comparison;
            offset += length;
        }
        checkpoint();
        return left.Length - right.Length;
    }

    internal static bool EqualBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, Action? checkpoint)
    {
        // Long, cancellable work shares the bounded comparison loop. Ordinary
        // equality needs neither ordering nor another copy of that machinery.
        return checkpoint is null ? left.SequenceEqual(right) : CompareBytes(left, right, checkpoint) == 0;
    }

    public bool EqualsAscii(string value, Action? checkpoint = null)
    {
        var span = AsSpan(checkpoint);
        if (span.Length != value.Length)
            return false;

        for (var index = 0; index < span.Length; index++)
        {
            if ((index & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
                checkpoint?.Invoke();
            if (value[index] > 0x7F || span[index] != (byte)value[index])
                return false;
        }

        checkpoint?.Invoke();
        return true;
    }

    internal static AwkText FromConcatenation(AwkTextConcatenation concatenation, int byteLength) =>
        new(new TextStorage(concatenation, byteLength));

    internal bool HasRuneAt(int index, Action? checkpoint)
    {
        if (index >= ByteLength) return false;
        if (index == 0) return true;
        if (RequiresStringCache) return true;
        var facts = _textFacts;
        if (facts > UnknownTextFacts) return index < facts - 1;
        if (index >= RuneCheckpointStride) return index < GetRuneLength(checkpoint);

        return HasRuneAt(AsSpan(checkpoint), index, checkpoint);
    }

    internal static bool HasRuneAt(ReadOnlySpan<byte> span, int index, Action? checkpoint)
    {
        if (index >= span.Length) return false;
        if (index == 0) return true;
        if (index <= RuneCheckpointStride && Ascii.IsValid(span[..index])) return true;
        var offset = 0;
        long nextCheckpoint = 0;
        for (var rune = 0; rune < index && offset < span.Length; rune++)
        {
            if (offset >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)offset + AwkRuntimeLimits.CancellationCheckBytes;
            }
            if (Rune.DecodeFromUtf8(span[offset..], out _, out var consumed) != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");
            offset += consumed;
        }
        checkpoint?.Invoke();
        return offset < span.Length;
    }

    // Shared one-use range scan. Owned text keeps its cached long-range index;
    // borrowed input needs neither whole-text materialization nor an index owner.
    internal static void ResolveRuneByteRange(ReadOnlySpan<byte> span, int start, int length, Action? checkpoint, out int byteStart, out int byteEnd)
    {
        var endRuneIndex = (long)start + length;
        var prefix = span[..(int)Math.Min(endRuneIndex, span.Length)];
        if (prefix.Length <= AwkRuntimeLimits.CancellationCheckBytes && Ascii.IsValid(prefix))
        {
            byteStart = Math.Min(start, prefix.Length);
            byteEnd = prefix.Length;

            checkpoint?.Invoke();
            return;
        }
        var runeIndex = 0;
        var offset = 0;
        byteStart = 0;
        long nextCheckpoint = 0;
        while (runeIndex < endRuneIndex && offset < span.Length)
        {
            if (offset >= nextCheckpoint)
            {
                checkpoint?.Invoke();
                nextCheckpoint = (long)offset + AwkRuntimeLimits.CancellationCheckBytes;
            }
            if (runeIndex == start) byteStart = offset;
            if (Rune.DecodeFromUtf8(span[offset..], out _, out var consumed) != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");
            offset += consumed;
            runeIndex++;
        }
        if (start >= runeIndex) byteStart = offset;
        byteEnd = offset;

        checkpoint?.Invoke();
    }

    internal ReadOnlySpan<byte> BorrowRuneSlice(int start, int length, AwkMemoryAccounting governor, Action? checkpoint)
    {
        ResolveRuneSliceRange(start, length, governor, checkpoint, out var byteStart, out var byteEnd);
        return AsSpan(checkpoint)[byteStart..byteEnd];
    }

    internal bool ResolveRuneSliceRange(int start, int length, AwkMemoryAccounting governor, Action? checkpoint, out int byteStart, out int byteEnd)
    {
        var text = this;
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        var knownAscii = text.RequiresStringCache || text._textFacts - 1 == text.ByteLength;
        if (knownAscii)
        {
            byteStart = Math.Min(start, text.ByteLength);
            byteEnd = (int)Math.Min((long)start + length, text.ByteLength);
        }
        else ResolveRuneByteRange(out byteStart, out byteEnd);
        return knownAscii;

        void ResolveRuneByteRange(out int resolvedByteStart, out int resolvedByteEnd)
        {
            var endRuneIndex = (long)start + length;
            // A short text or bounded prefix needs neither total rune count nor a whole-text index.
            // Keep the index for nonlocal/repeated access once it is available.
            if ((text.ByteLength <= RuneCheckpointStride || endRuneIndex <= RuneCheckpointStride) &&
                text._runeByteCheckpoints is null)
            {
                AwkText.ResolveRuneByteRange(text.AsSpan(checkpoint), start, length, checkpoint, out resolvedByteStart, out resolvedByteEnd);
                return;
            }
            if (text.IsAscii(out var inspectedByteCount, checkpoint))
            {

                resolvedByteStart = Math.Min(start, text.ByteLength);
                resolvedByteEnd = (int)Math.Min(endRuneIndex, text.ByteLength);
                return;
            }

            var checkpoints = GetRuneByteCheckpoints();
            var runeLength = text.GetRuneLength(checkpoint);
            start = Math.Min(start, runeLength);
            endRuneIndex = Math.Min(endRuneIndex, runeLength);
            resolvedByteStart = ResolveRuneByteOffset(start, checkpoints);
            resolvedByteEnd = ResolveRuneByteOffset((int)endRuneIndex, checkpoints);
        }

        int[] GetRuneByteCheckpoints()
        {
            var cached = text._runeByteCheckpoints;
            if (cached is not null)
                return cached;

            var checkpointCount = checked((text.ByteLength / RuneCheckpointStride) + 2);
            var checkpointBytes = checked(checkpointCount * sizeof(int));
            using var checkpointLease = governor.ReserveTransient(checkpointBytes);
            var created = new int[checkpointCount];

            var span = text.AsSpan(checkpoint);
            var byteOffset = 0;
            var runeIndex = 0;
            long nextCheckpoint = 0;
            while (byteOffset < span.Length)
            {
                if (byteOffset >= nextCheckpoint)
                {
                    checkpoint?.Invoke();
                    nextCheckpoint = (long)byteOffset + AwkRuntimeLimits.CancellationCheckBytes;
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

            text._textFacts = runeIndex + 1;
            return text._runeByteCheckpoints = created;
        }

        int ResolveRuneByteOffset(int runeIndex, int[] checkpoints)
        {
            var checkpointIndex = runeIndex / RuneCheckpointStride;
            var currentRuneIndex = checkpointIndex * RuneCheckpointStride;
            var byteOffset = checkpoints[checkpointIndex];
            var span = text.AsSpan(checkpoint);
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

            return byteOffset;
        }
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

    public bool Equals(AwkText other) => Equals(other, checkpoint: null);

    public bool Equals(AwkText other, Action? checkpoint)
    {
        checkpoint?.Invoke();
        return ByteLength == other.ByteLength && EqualBytes(AsSpan(checkpoint), other.AsSpan(checkpoint), checkpoint);
    }

    public override bool Equals(object? obj) =>
        obj is AwkText other && Equals(other);

    public override int GetHashCode() => GetHashCode(checkpoint: null);

    internal int GetHashCode(Action? checkpoint) =>
        AwkByteFacts.GetOrdinalHashCode(AsSpan(checkpoint), checkpoint);

    public override string ToString() => AsString();

    internal bool IsAscii(out int inspectedByteCount, Action? checkpoint = null)
    {
        if (RequiresStringCache)
        {
            inspectedByteCount = 0;
            return true;
        }
        var cached = _textFacts;
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
            if ((index & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0)
                checkpoint?.Invoke();
            if (span[index] <= 0x7F)
                continue;

            inspectedByteCount = index + 1;
            if (_textFacts == UnknownTextFacts)
                _textFacts = KnownNonAsciiWithoutRuneCount;
            return false;
        }

        inspectedByteCount = span.Length;
        _textFacts = ByteLength + 1;
        return true;
    }

    private byte[] MaterializeConcatenation(AwkTextConcatenation concatenation, Action? checkpoint)
    {
        var text = this;
        var cached = concatenation.MaterializedBytes;
        if (cached is not null)
            return cached;

        checkpoint?.Invoke();
        // Keep the allocation live while copying; cancellation releases the lease
        // and leaves the retained parts available for an independent retry.
        var governor = concatenation.Governor;
        governor.Reserve(text.ByteLength);
        byte[] bytes;
        try
        {
            bytes = new byte[text.ByteLength];
            governor.CommitReserved(text.ByteLength);
        }
        catch
        {
            governor.ReleaseReserved(text.ByteLength);
            throw;
        }
        try
        {
            CopyConcatenationTo(bytes);
            checkpoint?.Invoke();
            concatenation.Counters.Add(AwkCounter.ConcatenationBytesCopied, text.ByteLength);
            concatenation.Counters.ObserveTextAllocation(text.ByteLength);
            concatenation.MaterializedBytes = bytes;
            concatenation.LeftPart = Empty;
            concatenation.RightPart = Empty;
            return bytes;
        }
        finally
        {
            governor.ReleaseCommitted(text.ByteLength);
        }

        void CopyConcatenationTo(Span<byte> destination)
        {
            var pending = ArrayPool<AwkText>.Shared.Rent(16);
            var pendingCount = 1;
            var destinationOffset = 0;
            pending[0] = text;
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
                        var source = current.OwnedBytes.AsSpan();
                        CopyBytes(source, destination[destinationOffset..], checkpoint);
                        destinationOffset += source.Length;
                        continue;
                    }

                    var materialized = currentConcatenation.MaterializedBytes;
                    if (materialized is not null)
                    {
                        CopyBytes(materialized, destination[destinationOffset..], checkpoint);
                        destinationOffset += materialized.Length;
                        continue;
                    }

                    var left = currentConcatenation.LeftPart;
                    var right = currentConcatenation.RightPart;
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

    internal sealed class AwkTextConcatenation
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

        public AwkText LeftPart { get; set; }

        public AwkText RightPart { get; set; }

        public byte[]? MaterializedBytes;
    }

    private sealed class TextStorage
    {
        internal TextStorage(byte[] bytes)
        {
            Bytes = bytes;
            ByteLength = bytes.Length;
            EstimatedRetainedMemoryBytes = AwkRuntimeMemory.EstimateTextBytes(bytes.Length);
        }

        internal TextStorage(AwkTextConcatenation concatenation, int byteLength)
        {
            Bytes = [];
            ByteLength = byteLength;
            Concatenation = concatenation;
            EstimatedRetainedMemoryBytes = AwkRuntimeMemory.EstimateConcatenatedTextBytes(
                concatenation.LeftPart, concatenation.RightPart, byteLength);
        }

        internal readonly byte[] Bytes;
        internal readonly int ByteLength;
        internal readonly AwkTextConcatenation? Concatenation;
        internal readonly long EstimatedRetainedMemoryBytes;
        internal string? CachedString;
        // Zero is unknown, -1 is non-ASCII with unknown rune count; positive is count + 1.
        internal int TextFacts;
        internal int[]? RuneByteCheckpoints;
    }

    // The destination retains construction funding through publication or cleanup.
    // Admission includes text/cache capacity; the byte allocation is not probed again.
    internal static byte[] AllocateOwnedBytes(int length, AwkMemoryAccounting governor,
        AwkMemoryLease destination, Action? checkpoint)
    {
        if (length < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        if (length == 0) return [];
        var bytes = governor.IsEnabled ? AwkRuntimeMemory.EstimateTextBytes(length) : 0;
        destination.ReserveAdditional(bytes);
        try
        {
            // Only admission can add a callback here. Ordinary copies retain
            // their own entry/completion polls, including when no budget exists.
            if (bytes > 0) checkpoint?.Invoke();
            return new byte[length];
        }
        catch
        {
            destination.Release(bytes);
            throw;
        }
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

    internal static void ThrowIfInvalidUtf8(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        var remaining = bytes;
        while (!remaining.IsEmpty)
        {
            checkpoint?.Invoke();
            var length = Math.Min(remaining.Length, AwkRuntimeLimits.CancellationCheckBytes);
            if (length < remaining.Length && remaining[length - 1] >= 0x80)
            {
                // Validate a crossing scalar before the next cancellation check,
                // as ordinary decoding does. Inspect at most its four bytes;
                // malformed prefixes still fail in this window before publication.
                var start = length - 1;
                while (start > length - 4 && (remaining[start] & 0xc0) == 0x80) start--;
                if (Rune.DecodeFromUtf8(remaining[start..], out _, out var consumed) == OperationStatus.Done)
                    length = Math.Max(length, start + consumed);
            }
            if (!System.Text.Unicode.Utf8.IsValid(remaining[..length]))
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidUtf8Input);
            remaining = remaining[length..];
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
                var length = Math.Min(AwkRuntimeLimits.CancellationCheckBytes, source.Length - offset);
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
        AwkNumberParser.Parse(span, mode, out var parsed, checkpoint);
        parsed.ThrowIfError();
        number = parsed.Number;
        end = parsed.End;
        return parsed.Status == AwkNumberParseStatus.Number;
    }
}
