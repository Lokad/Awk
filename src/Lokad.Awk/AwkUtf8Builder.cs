using System;

namespace Lokad.Awk;

internal sealed class AwkUtf8Builder : IDisposable
{
    private readonly AwkMemoryAccounting _governor;
    private byte[] _buffer;
    private int _committedCapacity;
    private bool _disposed;

    public AwkUtf8Builder(AwkMemoryAccounting governor,
        int capacity = AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _governor = governor;
        if (capacity > Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        if (capacity == 0)
        {
            _buffer = [];
            return;
        }

        _governor.Reserve(capacity);
        try
        {
            _buffer = new byte[capacity];
        }
        catch
        {
            _governor.ReleaseReserved(capacity);
            throw;
        }

        _governor.CommitReserved(capacity);
        _committedCapacity = capacity;
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

    public AwkUtf8Builder Append(AwkText text, Action? checkpoint = null)
    {
        return Append(text.AsSpan(checkpoint), checkpoint);
    }

    public AwkUtf8Builder Append(ReadOnlySpan<AwkText> texts, Action? checkpoint = null)
    {
        ThrowIfDisposed();

        var nextLength = Length;
        for (var index = 0; index < texts.Length; index++)
        {
            if ((index & 1023) == 0)
                checkpoint?.Invoke();
            nextLength = AwkRuntimeMemory.CheckedAdd(nextLength, texts[index].ByteLength);
        }

        EnsureCapacity(nextLength);
        var destinationOffset = Length;
        for (var index = 0; index < texts.Length; index++)
        {
            var bytes = texts[index].AsSpan(checkpoint);
            AwkText.CopyBytes(bytes, _buffer.AsSpan(destinationOffset), checkpoint);
            destinationOffset += bytes.Length;
        }

        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(ReadOnlySpan<AwkFieldText> segments, AwkText record, Action? checkpoint = null) =>
        Append(segments, record.AsSpan(checkpoint), checkpoint);

    public AwkUtf8Builder Append(ReadOnlySpan<AwkFieldText> segments, ReadOnlySpan<byte> record, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        var nextLength = Length;
        foreach (var segment in segments)
        {
            checkpoint?.Invoke();
            nextLength = AwkRuntimeMemory.CheckedAdd(nextLength, segment.ByteLength());
        }
        EnsureCapacity(nextLength);
        var offset = Length;
        foreach (var segment in segments)
        {
            var bytes = segment.AsSpan(record, checkpoint);
            AwkText.CopyBytes(bytes, _buffer.AsSpan(offset), checkpoint);
            offset += bytes.Length;
        }
        Length = nextLength;
        return this;
    }

    public void AppendRecord(ReadOnlySpan<AwkFieldText> fields, ReadOnlySpan<byte> record,
        AwkText separator, AwkText terminator, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        var between = separator.AsSpan(checkpoint);
        var ending = terminator.AsSpan(checkpoint);
        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, ending.Length);
        for (var index = 0; index < fields.Length; index++)
        {
            if ((index & 1023) == 0) checkpoint?.Invoke();
            if (index > 0) nextLength = AwkRuntimeMemory.CheckedAdd(nextLength, between.Length);
            nextLength = AwkRuntimeMemory.CheckedAdd(nextLength, fields[index].ByteLength());
        }
        // Admit the complete record and terminator before copying. Failed/canceled
        // copies cannot publish a prefix: Length changes only after all bytes are owned.
        EnsureCapacity(nextLength);
        var offset = Length;
        for (var index = 0; index < fields.Length; index++)
        {
            if ((index & 1023) == 0) checkpoint?.Invoke();
            if (index > 0)
            {
                AwkText.CopyBytes(between, _buffer.AsSpan(offset), checkpoint);
                offset += between.Length;
            }
            var bytes = fields[index].AsSpan(record, checkpoint);
            AwkText.CopyBytes(bytes, _buffer.AsSpan(offset), checkpoint);
            offset += bytes.Length;
        }
        AwkText.CopyBytes(ending, _buffer.AsSpan(offset), checkpoint);
        Length = nextLength;
    }

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes, AwkText terminator, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        var ending = terminator.AsSpan(checkpoint);
        var nextLength = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(Length, bytes.Length), ending.Length);
        EnsureCapacity(nextLength);
        AwkText.CopyBytes(bytes, _buffer.AsSpan(Length), checkpoint);
        AwkText.CopyBytes(ending, _buffer.AsSpan(Length + bytes.Length), checkpoint);
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        if (bytes.IsEmpty)
            return this;

        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, bytes.Length);
        EnsureCapacity(nextLength);
        AwkText.CopyBytes(bytes, _buffer.AsSpan(Length), checkpoint);
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder AppendUtf8(ReadOnlySpan<char> text)
    {
        ThrowIfDisposed();
        if (text.IsEmpty)
            return this;

        var byteCount = AwkUtf8Codec.GetEncodedByteCount(text);
        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, byteCount);
        EnsureCapacity(nextLength);
        AwkUtf8Codec.EncodeWithoutCounting(
            text,
            _buffer.AsSpan(Length, byteCount));
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(byte value)
    {
        ThrowIfDisposed();
        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, 1);
        EnsureCapacity(nextLength);
        _buffer[Length] = value;
        Length = nextLength;
        return this;
    }

    public AwkUtf8Builder Append(byte value, int count, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return this;

        checkpoint?.Invoke();
        var nextLength = AwkRuntimeMemory.CheckedAdd(Length, count);
        EnsureCapacity(nextLength);
        if (checkpoint is null)
            _buffer.AsSpan(Length, count).Fill(value);
        else
        {
            for (var offset = 0; offset < count;)
            {
                checkpoint();
                var length = Math.Min(AwkRuntimeLimits.CancellationCheckBytes, count - offset);
                _buffer.AsSpan(Length + offset, length).Fill(value);
                offset += length;
            }
            checkpoint();
        }
        Length = nextLength;
        return this;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        Length = 0;
    }

    public void RemovePrefix(int length)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Length);

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

    public AwkText BorrowAsAwkText(int byteLength)
    {
        // This view is valid only until the builder is mutated.
        ThrowIfDisposed();
        if (byteLength < 0 || byteLength > Length)
            throw new ArgumentOutOfRangeException(nameof(byteLength));
        return AwkText.FromTrustedBorrowedBytes(_buffer, 0, byteLength);
    }

    public AwkText CopyToAwkText(AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        if (Length == 0)
            return AwkText.Empty;

        var bytes = AwkText.AllocateOwnedBytes(Length, _governor);
        AwkText.CopyBytes(AsSpan(), bytes, checkpoint);
        return AwkText.FromTrustedOwnedBytes(bytes, counters);
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

    public void EnsureCapacity(int capacity)
    {
        void ResizeAndCommit(int newSize)
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

        if (capacity <= _buffer.Length)
            return;

        if (capacity > Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        var currentCapacity = _buffer.Length;
        var newSize = currentCapacity == 0 ? 16 : currentCapacity;
        while (newSize < capacity)
        {
            if (newSize > Array.MaxLength / 2)
            {
                newSize = Array.MaxLength;
                break;
            }

            newSize *= 2;
        }

        ResizeAndCommit(newSize);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

}
