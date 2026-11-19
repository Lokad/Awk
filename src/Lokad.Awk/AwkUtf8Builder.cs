namespace Lokad.Awk;

internal sealed class AwkUtf8Builder : IDisposable
{
    private readonly AwkMemoryAccounting _accounting;
    private byte[] _buffer;
    private bool _disposed;

    public AwkUtf8Builder(AwkMemoryAccounting accounting,
        int capacity = AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _accounting = accounting;
        if (capacity > Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        if (capacity == 0)
        {
            _buffer = [];
            return;
        }

        _accounting.Charge(capacity);
        try
        {
            _buffer = new byte[capacity];
        }
        catch
        {
            _accounting.Refund(capacity);
            throw;
        }
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

    internal void AppendRecord(AwkRecordFields fields, AwkCopySource source,
        AwkText separator, AwkText terminator, int byteLength, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        var between = separator.AsSpan(checkpoint);
        var ending = terminator.AsSpan(checkpoint);
        // The record owner or argument preparation already knows the complete
        // content length. Consume it without traversing the field descriptors again.
        var nextLength = AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(Length, byteLength), ending.Length);
        // Admit the complete record and terminator before copying. Failed/canceled
        // copies cannot publish a prefix: Length changes only after all bytes are owned.
        EnsureCapacity(nextLength);
        checkpoint?.Invoke();
        var record = source.AsSpan();
        var offset = Length;
        var bytesUntilCheck = AwkRuntimeLimits.CancellationCheckBytes;
        for (var index = 0; index < fields.Length; index++)
        {
            if (index > 0 && (index & 1023) == 0)
            {
                checkpoint?.Invoke();
                bytesUntilCheck = AwkRuntimeLimits.CancellationCheckBytes;
            }
            if (index > 0)
                AwkText.CopyFragment(between, _buffer, ref offset, ref bytesUntilCheck, checkpoint);
            var bytes = fields.BytesAt(index, record, checkpoint);
            AwkText.CopyFragment(bytes, _buffer, ref offset, ref bytesUntilCheck, checkpoint);
        }
        AwkText.CopyFragment(ending, _buffer, ref offset, ref bytesUntilCheck, checkpoint);
        checkpoint?.Invoke();
        Length = nextLength;
    }

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes, AwkText terminator, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        var ending = terminator.AsSpan(checkpoint);
        var nextLength = AdmitAppend(bytes.Length, ending.Length);
        CopyAdmittedAppend(bytes, ending, nextLength, checkpoint);
        return this;
    }

    internal void Append(AwkCopySource source, AwkText terminator, Action? checkpoint)
    {
        ThrowIfDisposed();
        var ending = terminator.AsSpan(checkpoint);
        var nextLength = AdmitAppend(source.Length, ending.Length);
        checkpoint?.Invoke();
        CopyAdmittedAppend(source.AsSpan(), ending, nextLength, checkpoint);
    }

    internal void Append(AwkCopySource source, Action? checkpoint)
    {
        ThrowIfDisposed();
        if (source.Length == 0) return;
        var length = Length;
        if (source.Length > _buffer.Length - length)
        {
            EnsureCapacity(AwkRuntimeMemory.CheckedAdd(length, source.Length));
            checkpoint?.Invoke();
        }
        AwkText.CopyBytes(source.AsSpan(), _buffer.AsSpan(length), checkpoint);
        Length = length + source.Length;
    }

    private int AdmitAppend(int contentLength, int endingLength)
    {
        var nextLength = AwkRuntimeMemory.CheckedAdd(AwkRuntimeMemory.CheckedAdd(Length, contentLength), endingLength);
        EnsureCapacity(nextLength);
        return nextLength;
    }

    private void CopyAdmittedAppend(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> ending, int nextLength, Action? checkpoint)
    {
        AwkText.CopyBytes(bytes, _buffer.AsSpan(Length), checkpoint);
        AwkText.CopyBytes(ending, _buffer.AsSpan(Length + bytes.Length), checkpoint);
        Length = nextLength;
    }

    public AwkUtf8Builder Append(ReadOnlySpan<byte> bytes, Action? checkpoint = null)
    {
        ThrowIfDisposed();
        if (bytes.IsEmpty)
            return this;

        var length = Length;
        // Available capacity proves the nonnegative sum fits. Actual growth
        // retains its overflow check and admission before any copy/publication.
        if (bytes.Length > _buffer.Length - length)
            EnsureCapacity(AwkRuntimeMemory.CheckedAdd(length, bytes.Length));
        AwkText.CopyBytes(bytes, _buffer.AsSpan(Length), checkpoint);
        Length = length + bytes.Length;
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
        AwkUtf8Codec.Encode(
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

    public byte[] CopyToOwnedBytes(Action? checkpoint = null)
    {
        ThrowIfDisposed();
        if (Length == 0)
            return [];

        var bytes = AwkText.AllocateOwnedBytes(Length, _accounting);
        AwkText.CopyBytes(AsSpan(), bytes, checkpoint);
        return bytes;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _accounting.Refund(_buffer.Length);
        Length = 0;
        _buffer = [];
        _disposed = true;
    }

    public void EnsureCapacity(int capacity)
    {
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

        _accounting.Charge(newSize);
        try
        {
            Array.Resize(ref _buffer, newSize);
        }
        catch
        {
            _accounting.Refund(newSize);
            throw;
        }

        _accounting.Refund(currentCapacity);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

}
