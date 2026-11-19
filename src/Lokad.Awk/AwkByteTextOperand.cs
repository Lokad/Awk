using System.Runtime.InteropServices;

namespace Lokad.Awk;

// Immediate consumers may borrow bytes; values that escape still own AwkText.
// Stack-only storage prevents a record view from crossing a suspension.
internal readonly ref struct AwkByteTextOperand
{
    // A stable span keeps its reference and length. An owner replaces that
    // reference with either an owning spelling or a record slot and offset.
    private readonly object? _owner;
    private readonly ref readonly byte _firstByte;
    private readonly int _recordPosition;
    private readonly int _byteLength;

    public AwkByteTextOperand(AwkText owned)
    {
        _owner = owned.Backing;
        _recordPosition = -1;
        _byteLength = owned.ByteLength;
    }

    public AwkByteTextOperand(ReadOnlySpan<byte> bytes)
    {
        _firstByte = ref MemoryMarshal.GetReference(bytes);
        _byteLength = bytes.Length;
    }

    internal AwkByteTextOperand(AwkRecordStorage record, int slot, int offset, int length)
    {
        System.Diagnostics.Debug.Assert(slot is 0 or 1);
        _owner = record;
        _recordPosition = checked(offset * 2 + slot);
        _byteLength = length;
    }

    internal bool IsOwned => _recordPosition < 0 && _owner is not null;
    internal int ByteLength => _byteLength;

    internal bool TryGetOwned(out AwkText text)
    {
        text = IsOwned ? AwkText.FromBacking(_owner) : default;
        return IsOwned;
    }

    internal AwkText Own(AwkRuntimeState state, AwkMemoryLease? destination = null)
    {
        if (TryGetOwned(out var text)) return text;
        return TryGetRecordSource(out var source) ? state.CopyText(source, destination) :
            state.CopyText(AsSpan(state.CancellationCheckpoint), destination);
    }

    internal bool HasUnitAt(int index, bool bytes, Action? checkpoint)
    {
        if (bytes || index == 0) return index < _byteLength;
        return TryGetOwned(out var owned) ? owned.HasRuneAt(index, checkpoint) :
            index < _byteLength && AwkText.HasRuneAt(AsSpan(checkpoint), index, checkpoint);
    }

    internal AwkByteTextOperand SliceUnits(int start, int length, bool bytes, AwkRuntimeState state,
        AwkMemoryLease destination, bool borrowOwned = false)
    {
        var checkpoint = state.CancellationCheckpoint;
        var isOwned = TryGetOwned(out var owned);
        if (isOwned && !borrowOwned)
        {
            if (!bytes) return new(state.CopyRuneSlice(owned, start, length, destination));
            start = Math.Min(start, _byteLength);
            return new(state.CopyByteSlice(owned, start, Math.Min(length, _byteLength - start), destination));
        }
        if (isOwned && !bytes)
        {
            var view = owned.BorrowRuneSlice(start, length, state.MemoryAccounting, checkpoint);
            return view.Length == _byteLength ? new(owned) : new(view);
        }
        var content = AsSpan(checkpoint);
        int first, last;
        if (bytes)
        {
            first = Math.Min(start, content.Length);
            last = (int)Math.Min((long)first + length, content.Length);
        }
        else AwkText.ResolveRuneByteRange(content, start, length, checkpoint, out first, out last);
        var slice = content[first..last];
        // Byte slicing can cut a scalar; validate before admitting the escaping result.
        if (bytes) AwkText.ThrowIfInvalidUtf8(slice, checkpoint);
        if (isOwned && slice.Length == _byteLength) return new(owned);
        return _recordPosition >= 0 && _owner is AwkRecordStorage record
            ? new(record, _recordPosition & 1, (_recordPosition >> 1) + first, slice.Length)
            : new(slice);
    }

    public ReadOnlySpan<byte> AsSpan(Action? checkpoint)
    {
        if (_owner is null) return MemoryMarshal.CreateReadOnlySpan(in _firstByte, _byteLength);
        if (_recordPosition < 0) return AwkText.FromBacking(_owner).AsSpan(checkpoint);
        return ((AwkRecordStorage)_owner).BytesAt(_recordPosition & 1).Slice(_recordPosition >> 1, _byteLength);
    }

    internal bool TryGetRecordSource(out AwkCopySource source)
    {
        if (_recordPosition >= 0 && _owner is AwkRecordStorage record)
        {
            source = new(record, _recordPosition & 1, _recordPosition >> 1, _byteLength);
            return true;
        }
        source = default;
        return false;
    }

    internal AwkByteReader? RecordBorrower => _recordPosition >= 0 && _owner is AwkRecordStorage record
        ? record.BorrowerAt(_recordPosition & 1) : null;
}
