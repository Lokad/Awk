using System.Runtime.InteropServices;

namespace Lokad.Awk;

// Capture scalar words or an owner and range. Record coordinates survive promotion
// within this computation. The snapshot cannot cross a host suspension.
internal readonly ref struct AwkTextInput
{
    private const int OwnedText = -1, RecordSlot0 = -2, RecordSlot1 = -3, TextRange = -4;
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

    internal static AwkTextInput FromRecord(AwkRecordStorage storage, int slot, int offset, int length, int field)
    {
        var memory = storage.ContentAt(slot);
        // Preserve custom memory access before later argument evaluation.
        if (!MemoryMarshal.TryGetArray(memory, out _))
            _ = memory.Span;
        return new(storage, offset, length, slot == 0 ? RecordSlot0 : RecordSlot1, field);
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
                RecordSlot0 or RecordSlot1 when _reference is AwkRecordStorage storage =>
                    new(storage, _kind == RecordSlot0 ? 0 : 1, offset, length),
                TextRange => new(AwkText.FromBacking(_reference).AsSpan().Slice(offset, length)),
                _ => throw new InvalidOperationException("A scalar text input requires formatting at use.")
            };
        }
    }

    internal AwkTextInput Prepare(AwkRuntimeState state, bool forceScalar = false) =>
        _kind < 0 ? this : new(FormatScalar(state, forceScalar));

    internal AwkByteTextOperand Text(AwkRuntimeState state) =>
        _kind < 0 ? ReadyText : new(FormatScalar(state, forceScalar: false));

    private AwkText FormatScalar(AwkRuntimeState state, bool forceScalar)
    {
        var value = AwkValue.FromStorage(_reference, BitConverter.Int64BitsToDouble(_word), (AwkValueKind)_kind);
        return state.FormatStringByteText(forceScalar ? value.ForceScalar() : value);
    }
}
