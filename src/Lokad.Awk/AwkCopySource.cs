namespace Lokad.Awk;

// A synchronous copy carries a stable span or coordinates in a promotable record slot.
internal readonly ref struct AwkCopySource
{
    private readonly ReadOnlySpan<byte> _bytes;
    private readonly AwkRecordStorage? _storage;
    private readonly int _slot;
    private readonly int _offset;

    internal int Length { get; }

    internal AwkCopySource(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes;
        Length = bytes.Length;
    }

    internal AwkCopySource(AwkRecordStorage storage, int slot)
        : this(storage, slot, 0, storage.LengthAt(slot)) { }

    internal AwkCopySource(AwkRecordStorage storage, int slot, int offset, int length)
    {
        _storage = storage;
        _slot = slot;
        _offset = offset;
        Length = length;
    }

    internal AwkCopySource Slice(int offset, int length) =>
        _storage is { } storage ? new(storage, _slot, _offset + offset, length) : new(_bytes.Slice(offset, length));

    internal ReadOnlySpan<byte> AsSpan() =>
        _storage is null ? _bytes : _storage.ContentAt(_slot).Span.Slice(_offset, Length);
}
