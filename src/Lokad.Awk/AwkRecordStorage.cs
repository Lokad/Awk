using System.Numerics;

namespace Lokad.Awk;

// Two frame-owned slots separate staged input from the published record. Every
// borrowed slot has an admitted fallback before publication or borrow retirement.
internal sealed class AwkRecordStorage(AwkMemoryAccounting memory,
    AwkRuntimeCounters counters, Action checkpoint, AwkFieldFrame? frame = null) : IDisposable
{
    private const int MaxReusableRecordBytes = 4096;
    private RecordBuffer _first;
    private RecordBuffer _second;
    private int _currentSlot;

    internal int CurrentSlot => _currentSlot;
    internal ReadOnlyMemory<byte> Content => ContentAt(_currentSlot);
    internal ReadOnlyMemory<byte> ContentAt(int slot) => BufferAt(slot).Content;

    internal static bool CanStage(int length) => length is > 0 and <= MaxReusableRecordBytes;

    private ref RecordBuffer BufferAt(int slot)
    {
        if (slot == 0) return ref _first;
        return ref _second;
    }

    private AwkUtf8Builder Prepare(ref RecordBuffer slot, int length, out bool reused)
    {
        var buffer = slot.Buffer;
        reused = buffer is not null;
        if (buffer is null)
        {
            buffer = new(memory, (int)BitOperations.RoundUpToPowerOf2((uint)length));
            slot.Buffer = buffer;
        }
        else buffer.EnsureCapacity(length);
        return buffer;
    }

    // The cursor proves UTF-8 validity before entering either storage writer.
    internal void StageValidated(ReadOnlySpan<byte> bytes)
    {
        var nextSlot = _currentSlot ^ 1;
        ref var next = ref BufferAt(nextSlot);
        var buffer = Prepare(ref next, bytes.Length, out var reused);
        buffer.Clear();
        buffer.Append(bytes, checkpoint);
        checkpoint();
        next.SetOwned();
        _currentSlot = nextSlot;
        counters.RecordBufferedInputRecord(reused);
        counters.RecordInputRecordCopy(bytes.Length);
    }

    internal void StageBorrowed(AwkByteReader reader, ReadOnlyMemory<byte> bytes)
    {
        var nextSlot = _currentSlot ^ 1;
        ref var next = ref BufferAt(nextSlot);
        _ = Prepare(ref next, bytes.Length, out var reused);
        checkpoint();
        reader.AttachRecordStorage(this);
        next.Borrower = reader;
        next.BorrowedBytes = bytes;
        _currentSlot = nextSlot;
        counters.RecordBufferedInputRecord(reused, borrowed: true);
    }

    // Borrow retirement is not cancellable. All capacity was funded at staging;
    // these bounded copies cannot allocate or call admission/cancellation callbacks.
    internal void PreserveBorrowedRecords(AwkByteReader reader)
    {
        Preserve(ref _first, 0);
        Preserve(ref _second, 1);

        void Preserve(ref RecordBuffer slot, int index)
        {
            if (!ReferenceEquals(slot.Borrower, reader)) return;
            var buffer = slot.Buffer ?? throw new InvalidOperationException("A borrowed record needs funded storage.");
            var bytes = slot.BorrowedBytes;
            buffer.Clear();
            buffer.Append(bytes.Span);
            slot.SetOwned();
            frame?.RebindBufferedRecord(this, index, buffer.AsMemory());
            counters.RecordInputRecordCopy(bytes.Length, promotion: true);
        }
    }

    public void Dispose()
    {
        _first.Dispose();
        _second.Dispose();
    }

    private struct RecordBuffer
    {
        internal AwkUtf8Builder? Buffer;
        internal AwkByteReader? Borrower;
        internal ReadOnlyMemory<byte> BorrowedBytes;
        internal readonly ReadOnlyMemory<byte> Content => Borrower is not null
            ? BorrowedBytes : Buffer?.AsMemory() ?? ReadOnlyMemory<byte>.Empty;

        internal void SetOwned()
        {
            Borrower = null;
            BorrowedBytes = default;
        }

        internal void Dispose()
        {
            SetOwned();
            Buffer?.Dispose();
            Buffer = null;
        }
    }
}
