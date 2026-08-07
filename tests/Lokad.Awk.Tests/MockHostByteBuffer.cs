using System;

namespace Lokad.Awk.Tests;

internal sealed class MockHostByteBuffer
{
    private const int DefaultMaxBufferBytes = 64 << 20;

    private byte[] _buffer;
    private int _length;
    private readonly int _maxBufferBytes;

    public MockHostByteBuffer()
        : this(256)
    {
    }

    public MockHostByteBuffer(int capacity)
        : this(capacity, DefaultMaxBufferBytes)
    {
    }

    internal MockHostByteBuffer(int capacity, int maxBufferBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBufferBytes);

        _maxBufferBytes = maxBufferBytes;
        _buffer = capacity > 0 ? new byte[capacity] : [];
    }

    public int Length => _length;

    public MockHostByteBuffer Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return this;

        EnsureCapacity(_length + bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_length));
        _length += bytes.Length;
        return this;
    }

    public MockHostByteBuffer Append(ReadOnlyMemory<byte> bytes) => Append(bytes.Span);

    public MockHostByteBuffer Append(byte value)
    {
        EnsureCapacity(_length + 1);
        _buffer[_length++] = value;
        return this;
    }

    public ReadOnlyMemory<byte> ToMemory() =>
        _length == 0 ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(_buffer, 0, _length);

    private void EnsureCapacity(int capacity)
    {
        if (capacity <= _buffer.Length)
            return;

        if (capacity > _maxBufferBytes)
            throw new InvalidOperationException("MockHostByteBuffer exceeded its configured byte limit.");

        var newSize = _buffer.Length == 0 ? 16 : _buffer.Length;
        while (newSize < capacity)
            newSize *= 2;

        if (newSize > _maxBufferBytes)
            newSize = _maxBufferBytes;

        Array.Resize(ref _buffer, newSize);
    }
}
