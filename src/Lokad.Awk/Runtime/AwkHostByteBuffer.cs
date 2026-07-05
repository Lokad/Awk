using System;

namespace Lokad.Awk;

/// <summary>
/// Legacy byte buffer helper for host/test adapters. Runtime value construction should use
/// <see cref="AwkUtf8Builder"/> so allocation failures flow through <see cref="AwkRuntimeLimits"/>.
/// </summary>
internal sealed class AwkHostByteBuffer
{
    public const int MaxBufferBytes = AwkRuntimeLimits.DefaultMaxBuilderBytes;

    private byte[] _buffer;
    private int _length;
    private readonly int _maxBuilderBytes;

    public AwkHostByteBuffer(int capacity = 256)
        : this(capacity, MaxBufferBytes)
    {
    }

    internal AwkHostByteBuffer(int capacity, int maxBuilderBytes)
    {
        if (maxBuilderBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBuilderBytes));

        _maxBuilderBytes = maxBuilderBytes;
        _buffer = capacity > 0 ? new byte[capacity] : [];
    }

    public int Length => _length;

    public AwkHostByteBuffer Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return this;
        }

        EnsureCapacity(_length + bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_length));
        _length += bytes.Length;
        return this;
    }

    public AwkHostByteBuffer Append(ReadOnlyMemory<byte> bytes) => Append(bytes.Span);

    public AwkHostByteBuffer Append(byte value)
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
        {
            return;
        }

        if (capacity > _maxBuilderBytes)
        {
            throw new InvalidOperationException("AwkHostByteBuffer exceeded its configured byte limit.");
        }

        var newSize = _buffer.Length == 0 ? 16 : _buffer.Length;
        while (newSize < capacity)
        {
            newSize *= 2;
        }

        if (newSize > _maxBuilderBytes)
        {
            newSize = _maxBuilderBytes;
        }

        Array.Resize(ref _buffer, newSize);
    }
}
