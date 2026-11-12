namespace Lokad.Awk;

// Immediate consumers may borrow bytes; values that escape still own AwkText.
// Stack-only storage prevents a record view from crossing a suspension.
internal readonly ref struct AwkByteTextOperand
{
    private readonly AwkText _owned;
    private readonly ReadOnlySpan<byte> _bytes;

    public AwkByteTextOperand(AwkText owned) => _owned = owned;
    public AwkByteTextOperand(ReadOnlySpan<byte> bytes) => _bytes = bytes;

    internal bool IsOwned => _owned.Backing is not null;
    internal int ByteLength => IsOwned ? _owned.ByteLength : _bytes.Length;

    internal bool TryGetOwned(out AwkText text)
    {
        text = _owned;
        return IsOwned;
    }

    internal AwkText Own(AwkRuntimeCounters counters, AwkMemoryAccounting governor, Action checkpoint,
        AwkMemoryLease? destination = null) =>
        _owned.Backing is not null ? _owned : AwkText.CopyTrustedBytes(_bytes, counters, governor, checkpoint, destination);

    internal bool HasUnitAt(int index, bool bytes, Action? checkpoint)
    {
        if (_owned.Backing is not null)
            return bytes ? index < _owned.ByteLength : _owned.HasRuneAt(index, checkpoint);
        return bytes ? index < _bytes.Length : AwkText.HasRuneAt(_bytes, index, checkpoint);
    }

    internal AwkByteTextOperand SliceUnits(int start, int length, bool bytes, AwkRuntimeCounters counters,
        AwkMemoryAccounting governor, Action? checkpoint, AwkMemoryLease destination, bool borrowOwned = false)
    {
        if (_owned.Backing is not null && !borrowOwned)
        {
            if (!bytes) return new(_owned.CopyRuneSlice(start, length, counters, governor, checkpoint, destination));
            start = Math.Min(start, _owned.ByteLength);
            return new(_owned.CopyByteSlice(start, Math.Min(length, _owned.ByteLength - start), counters, governor, checkpoint, destination));
        }
        if (_owned.Backing is not null && !bytes)
        {
            var view = _owned.BorrowRuneSlice(start, length, counters, governor, checkpoint);
            return view.Length == _owned.ByteLength ? new(_owned) : new(view);
        }
        var content = _owned.Backing is not null ? _owned.AsSpan(checkpoint) : _bytes;
        int first, last;
        if (bytes)
        {
            first = Math.Min(start, content.Length);
            last = (int)Math.Min((long)first + length, content.Length);
        }
        else AwkText.ResolveRuneByteRange(content, start, length, counters, checkpoint, out first, out last);
        var slice = content[first..last];
        // Byte slicing can cut a scalar; validate before admitting the escaping result.
        if (bytes) AwkText.ThrowIfInvalidUtf8(slice, checkpoint);
        if (_owned.Backing is not null && slice.Length == _owned.ByteLength) return new(_owned);
        return new(slice);
    }

    public ReadOnlySpan<byte> AsSpan(Action? checkpoint)
    {
        // Keep owned flattening at the consumer's original point, after provider pinning.
        if (_owned.Backing is not null) return _owned.AsSpan(checkpoint);
        // The consumer owns cancellation for scanning these already-ready bytes.
        return _bytes;
    }
}
