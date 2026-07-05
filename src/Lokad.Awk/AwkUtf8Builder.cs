using System;

namespace Lokad.Awk;

internal sealed class AwkUtf8Builder : IDisposable
{
    private readonly AwkMemoryGovernor _governor;
    private readonly int _maxBuilderBytes;
    private byte[] _buffer;
    private int _committedCapacity;
    private bool _disposed;

    public AwkUtf8Builder(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor,
        int capacity = AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(governor);
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _governor = governor;
        _maxBuilderBytes = limits.MaxBuilderBytes;
        var initialCapacity = Math.Min(capacity, _maxBuilderBytes);
        if (initialCapacity == 0)
        {
            _buffer = [];
            return;
        }

        _governor.Reserve(initialCapacity);
        try
        {
            _buffer = new byte[initialCapacity];
        }
        catch
        {
            _governor.ReleaseReserved(initialCapacity);
            throw;
        }

        _governor.CommitReserved(initialCapacity);
        _committedCapacity = initialCapacity;
    }

    public int Length { get; private set; }

    public ReadOnlySpan<byte> AsSpan()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, Length);
    }

    public ReadOnlyMemory<byte> AsMemory()
    {
        ThrowIfDisposed();
        return _buffer.AsMemory(0, Length);
    }

    public AwkUtf8Builder Append(AwkText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Append(text.AsSpan());
    }

    public AwkUtf8Builder Append(ReadOnlyMemory<byte> bytes) => Append(bytes.Span);

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes)
    {
        ThrowIfDisposed();
        if (bytes.IsEmpty)
            return this;

        var nextLength = CheckedLength(Length, bytes.Length);
        EnsureCapacity(nextLength);
        bytes.CopyTo(_buffer.AsSpan(Length));
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(byte value)
    {
        ThrowIfDisposed();
        var nextLength = CheckedLength(Length, 1);
        EnsureCapacity(nextLength);
        _buffer[Length] = value;
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

        _buffer.AsSpan(length, Length - length).CopyTo(_buffer);
        Length -= length;
    }

    public AwkText CopyToAwkText(AwkRuntimeCounters counters)
    {
        ThrowIfDisposed();
        if (Length == 0)
            return AwkText.Empty;

        return AwkText.FromOwnedBytes(_buffer.AsSpan(0, Length).ToArray(), counters, _governor);
    }

    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return Length == 0 ? [] : _buffer.AsSpan(0, Length).ToArray();
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
        if (capacity <= _buffer.Length)
            return;

        if (capacity > _maxBuilderBytes)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        var newSize = _buffer.Length == 0 ? 16 : _buffer.Length;
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

        ReserveResizeCommit(newSize);
    }

    private void ReserveResizeCommit(int newSize)
    {
        _governor.Reserve(newSize);
        try
        {
            Array.Resize(ref _buffer, newSize);
        }
        catch
        {
            _governor.ReleaseReserved(newSize);
            throw;
        }

        if (_committedCapacity > 0)
            _governor.ReleaseCommitted(_committedCapacity);

        _governor.CommitReserved(newSize);
        _committedCapacity = newSize;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AwkUtf8Builder));
    }

    private static int CheckedLength(int left, int right)
    {
        if (left > int.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }
}
