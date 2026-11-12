using System.Buffers;
using System.Runtime.InteropServices;

namespace Lokad.Awk;

// A synchronous text consumer captures either canonical scalar words or the
// actual immutable byte owner/range. Existing cursor/frame owners keep funding;
// this stack-only snapshot cannot cross an actual host suspension.
internal readonly ref struct AwkTextInput
{
    private const int OwnedText = -1, ArrayBytes = -2, ManagerBytes = -3, TextRange = -4, ReaderBytes = -5;
    private readonly object? _reference;
    private readonly long _word;
    private readonly int _kind;
    private readonly int _field;

    internal AwkTextInput(scoped in AwkValue value)
    {
        _reference = value.StoredReference;
        _word = BitConverter.DoubleToInt64Bits(value.StoredNumber);
        _kind = (int)value.StoredKind;
        _field = -1;
    }

    internal AwkTextInput(AwkText text)
    {
        _reference = text.Backing;
        _word = 0;
        _kind = OwnedText;
        _field = -1;
    }

    private AwkTextInput(object? owner, int offset, int length, int kind, int field)
    {
        _reference = owner;
        _word = ((long)offset << 32) | (uint)length;
        _kind = kind;
        _field = field;
    }

    internal static AwkTextInput FromMemory(ReadOnlyMemory<byte> memory, int field)
    {
        if (MemoryMarshal.TryGetArray(memory, out var array))
            return new(array.Array, array.Offset, array.Count, ArrayBytes, field);
        // Array ranges are already validated by ReadOnlyMemory. Preserve a
        // custom manager's access at capture, before later argument reads.
        _ = memory.Span;
        if (MemoryMarshal.TryGetMemoryManager<byte, MemoryManager<byte>>(memory, out var manager, out var offset, out var length))
            return new(manager, offset, length, ManagerBytes, field);
        throw new InvalidOperationException("Byte memory requires an array or memory-manager owner.");
    }

    internal static AwkTextInput FromReader(AwkReadBuffer reader, int offset, int length)
    {
        _ = reader.Content.Slice(offset, length).Span;
        return new(reader, offset, length, ReaderBytes, -1);
    }

    internal static AwkTextInput FromTextRange(AwkText text, int offset, int length, int field) =>
        new(text.Backing, offset, length, TextRange, field);

    internal int TextField => _field;

    internal AwkByteTextOperand ReadyText
    {
        get
        {
            var offset = (int)(_word >> 32);
            var length = (int)_word;
            return _kind switch
            {
                OwnedText => new(AwkText.FromBacking(_reference)),
                ArrayBytes => new((_reference as byte[]).AsSpan(offset, length)),
                ManagerBytes when _reference is MemoryManager<byte> manager => new(manager.GetSpan().Slice(offset, length)),
                TextRange => new(AwkText.FromBacking(_reference).AsSpan().Slice(offset, length)),
                ReaderBytes when _reference is AwkReadBuffer reader => new(reader.Content.Span.Slice(offset, length)),
                _ => throw new InvalidOperationException("A scalar text input requires formatting at use.")
            };
        }
    }

    internal AwkByteTextOperand Text(AwkRuntimeState state, bool forceScalar = false)
    {
        if (_kind < 0) return ReadyText;
        var value = AwkValue.FromStorage(_reference, BitConverter.Int64BitsToDouble(_word), (AwkValueKind)_kind);
        return new(state.FormatStringByteText(forceScalar ? value.ForceScalar() : value));
    }
}
