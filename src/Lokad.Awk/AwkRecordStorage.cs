using System.Numerics;
using System.Runtime.InteropServices;

namespace Lokad.Awk;

// Two frame-owned slots separate staged input from the published record. Every
// borrowed slot has an admitted fallback before publication or borrow retirement.
internal sealed class AwkRecordStorage(AwkRuntimeState state) : IDisposable
{
    private const int MaxBorrowedRecordBytes = 4096;
    private const int MaxReusableRecordBytes = 65536;
    private RecordBuffer _first;
    private RecordBuffer _second;
    private int _currentSlot;

    internal int CurrentSlot => _currentSlot;
    internal ReadOnlyMemory<byte> Content => ContentAt(_currentSlot);
    internal ReadOnlyMemory<byte> ContentAt(int slot) => BufferAt(slot).Content;
    internal AwkByteReader? BorrowerAt(int slot) => BufferAt(slot).Borrower;

    internal void ObserveMemoryAt(int slot)
    {
        var memory = ContentAt(slot);
        if (!MemoryMarshal.TryGetArray(memory, out _))
            _ = memory.Span;
    }

    internal ReadOnlySpan<byte> BytesAt(int slot)
    {
        ref var buffer = ref BufferAt(slot);
        var borrower = buffer.Borrower;
        var bytes = buffer.Content.Span;
        // A custom memory accessor can retire its own borrow while obtaining the span.
        return borrower is not null && buffer.Borrower is null ? buffer.Content.Span : bytes;
    }

    internal int LengthAt(int slot) => BufferAt(slot).Length;
    internal bool IsKnownAsciiAt(int slot) => BufferAt(slot).IsKnownAscii;

    internal static bool CanStage(int length) => length is > 0 and <= MaxReusableRecordBytes;
    internal static bool CanBorrow(int length) => length is > 0 and <= MaxBorrowedRecordBytes;

    private ref RecordBuffer BufferAt(int slot)
    {
        if (slot == 0) return ref _first;
        return ref _second;
    }

    private AwkUtf8Builder Prepare(ref RecordBuffer slot, int length)
    {
        var buffer = slot.Buffer;
        if (buffer is null)
        {
            buffer = new(state.MemoryAccounting, (int)BitOperations.RoundUpToPowerOf2((uint)length));
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
        var buffer = Prepare(ref next, bytes.Length);
        buffer.Clear();
        buffer.Append(bytes, state.CancellationCheckpoint);
        state.ThrowIfCancellationRequested();
        next.SetOwned();
        next.IsKnownAscii = false;
        _currentSlot = nextSlot;

        state.Counters.Add(AwkCounter.InputRecordBytesCopied, bytes.Length);
    }

    internal void StageBorrowed(AwkByteReader reader, ReadOnlyMemory<byte> bytes)
    {
        // The cursor supplies a slice of this immutable reader borrow. Capture its
        // proof with the bytes, before admission; promotion preserves both together.
        var knownAscii = reader.ContentIsAscii;
        var nextSlot = _currentSlot ^ 1;
        ref var next = ref BufferAt(nextSlot);
        _ = Prepare(ref next, bytes.Length);
        state.ThrowIfCancellationRequested();
        reader.AttachRecordStorage(this);
        next.Borrower = reader;
        next.BorrowedBytes = bytes;
        next.IsKnownAscii = knownAscii;
        _currentSlot = nextSlot;

    }

    // Borrow retirement is not cancellable. All capacity was funded at staging;
    // these bounded copies cannot allocate or call admission/cancellation callbacks.
    internal void PreserveBorrowedRecords(AwkByteReader reader)
    {
        Preserve(ref _first);
        Preserve(ref _second);

        void Preserve(ref RecordBuffer slot)
        {
            if (!ReferenceEquals(slot.Borrower, reader)) return;
            var buffer = slot.Buffer ?? throw new InvalidOperationException("A borrowed record needs funded storage.");
            var bytes = slot.BorrowedBytes;
            buffer.Clear();
            buffer.Append(bytes.Span);
            slot.SetOwned();
            state.Counters.Add(AwkCounter.InputRecordBytesCopied, bytes.Length);
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
        internal bool IsKnownAscii;
        internal readonly ReadOnlyMemory<byte> Content => Borrower is not null
            ? BorrowedBytes : Buffer?.AsMemory() ?? ReadOnlyMemory<byte>.Empty;
        internal readonly int Length => Borrower is not null ? BorrowedBytes.Length : Buffer?.Length ?? 0;

        internal void SetOwned()
        {
            Borrower = null;
            BorrowedBytes = default;
        }

        internal void Dispose()
        {
            SetOwned();
            IsKnownAscii = false;
            Buffer?.Dispose();
            Buffer = null;
        }
    }
}
