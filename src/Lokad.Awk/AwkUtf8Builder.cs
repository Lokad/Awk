using System;

namespace Lokad.Awk;

internal sealed class AwkUtf8Builder : IDisposable
{
    private readonly AwkMemoryGovernor _governor;
    private readonly int _maxBuilderBytes;
    private readonly int _contentOffset;
    private byte[] _buffer;
    private int _committedCapacity;
    private bool _disposed;

    public AwkUtf8Builder(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor)
        : this(limits, governor, AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity)
    {
    }

    public AwkUtf8Builder(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor,
        int capacity)
        : this(limits, governor, capacity, reservedPrefix: null)
    {
    }

    private AwkUtf8Builder(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor,
        int capacity,
        byte? reservedPrefix)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _governor = governor;
        _maxBuilderBytes = limits.MaxBuilderBytes;
        _contentOffset = reservedPrefix.HasValue ? 1 : 0;
        var initialCapacity = Math.Min(capacity, _maxBuilderBytes);
        var allocatedCapacity = AwkRuntimeMemory.CheckedAdd(initialCapacity, _contentOffset);
        if (allocatedCapacity == 0)
        {
            _buffer = [];
            return;
        }

        _governor.Reserve(allocatedCapacity);
        try
        {
            _buffer = new byte[allocatedCapacity];
        }
        catch
        {
            _governor.ReleaseReserved(allocatedCapacity);
            throw;
        }

        if (reservedPrefix.HasValue)
            _buffer[0] = reservedPrefix.Value;

        _governor.CommitReserved(allocatedCapacity);
        _committedCapacity = allocatedCapacity;
    }

    public static AwkUtf8Builder CreateWithReservedPrefix(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor,
        int capacity,
        byte prefix) =>
        new(limits, governor, capacity, prefix);

    public int Length { get; private set; }

    public ReadOnlySpan<byte> AsSpan()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(_contentOffset, Length);
    }

    public ReadOnlyMemory<byte> AsMemory()
    {
        ThrowIfDisposed();
        return _buffer.AsMemory(_contentOffset, Length);
    }

    public AwkUtf8Builder Append(AwkText text)
    {
        return Append(text.AsSpan());
    }

    public AwkUtf8Builder Append(ReadOnlyMemory<byte> bytes) => Append(bytes.Span);

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes)
    {
        ThrowIfDisposed();
        if (bytes.IsEmpty)
            return this;

        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, bytes.Length);
        EnsureCapacity(nextLength);
        bytes.CopyTo(_buffer.AsSpan(_contentOffset + Length));
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(byte value)
    {
        ThrowIfDisposed();
        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, 1);
        EnsureCapacity(nextLength);
        _buffer[_contentOffset + Length] = value;
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(byte value, int count)
    {
        ThrowIfDisposed();
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return this;

        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, count);
        EnsureCapacity(nextLength);
        _buffer.AsSpan(_contentOffset + Length, count).Fill(value);
        Length = nextLength;
        return this;
    }

    public AwkText CopyToAwkText() =>
        CopyToAwkText(AwkRuntimeCounters.Disabled);

    public void Clear()
    {
        ThrowIfDisposed();
        Length = 0;
    }

    public void RemovePrefix(int length)
    {
        ThrowIfDisposed();
        if (length < 0 || length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length == 0)
            return;

        if (length == Length)
        {
            Clear();
            return;
        }

        _buffer.AsSpan(_contentOffset + length, Length - length)
            .CopyTo(_buffer.AsSpan(_contentOffset));
        Length -= length;
    }

    public AwkText BorrowAsAwkText(bool includeReservedPrefix)
    {
        // This view is valid only until the builder is mutated.
        ThrowIfDisposed();
        if (includeReservedPrefix && _contentOffset == 0)
            throw new InvalidOperationException("This UTF-8 builder has no reserved prefix.");

        var prefixLength = includeReservedPrefix ? 1 : 0;
        return AwkText.FromTrustedBorrowedBytes(
            _buffer,
            _contentOffset - prefixLength,
            Length + prefixLength);
    }

    public AwkText CopyToAwkText(AwkRuntimeCounters counters)
    {
        ThrowIfDisposed();
        if (Length == 0)
            return AwkText.Empty;

        var bytes = AwkText.AllocateOwnedBytes(Length, _governor);
        AsSpan().CopyTo(bytes);
        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return Length == 0 ? [] : AsSpan().ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_committedCapacity > 0)
            _governor.ReleaseCommitted(_committedCapacity);

        _committedCapacity = 0;
        Length = 0;
        _buffer = [];
        _disposed = true;
    }

    private void EnsureCapacity(int capacity)
    {
        void ResizeAndCommit(int newSize)
        {
            var allocatedSize = AwkRuntimeMemory.CheckedAdd(newSize, _contentOffset);
            _governor.Reserve(allocatedSize);
            try
            {
                Array.Resize(ref _buffer, allocatedSize);
            }
            catch
            {
                _governor.ReleaseReserved(allocatedSize);
                throw;
            }

            if (_committedCapacity > 0)
                _governor.ReleaseCommitted(_committedCapacity);

            _governor.CommitReserved(allocatedSize);
            _committedCapacity = allocatedSize;
        }

        if (capacity <= _buffer.Length - _contentOffset)
            return;

        if (capacity > _maxBuilderBytes)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        var currentCapacity = _buffer.Length - _contentOffset;
        var newSize = currentCapacity == 0 ? 16 : currentCapacity;
        while (newSize < capacity)
        {
            if (newSize > _maxBuilderBytes / 2)
            {
                newSize = _maxBuilderBytes;
                break;
            }

            newSize *= 2;
        }

        if (newSize > _maxBuilderBytes)
            newSize = _maxBuilderBytes;

        ResizeAndCommit(newSize);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AwkUtf8Builder));
    }

}
