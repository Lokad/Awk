namespace Lokad.Awk;

// Immediate scalar consumers may use stable field bytes without owning the spelling.
// Effects and suspension require owned capture. Comparison classification stays local;
// text consumers need no classification before formatting at use.
internal readonly ref struct AwkScalarOperand
{
    private readonly ReadOnlySpan<byte> _bytes;
    private readonly int _field = -1;
    internal AwkValue Value { get; }
    internal bool IsBorrowed { get; }

    internal AwkScalarOperand(AwkValue value) => Value = value;

    internal AwkScalarOperand(AwkByteTextOperand text)
    {
        if (text.TryGetOwned(out var owned)) Value = AwkValue.ForStoredText(owned);
        else
        {
            _bytes = text.AsSpan(checkpoint: null);
            IsBorrowed = true;
        }
    }

    internal AwkScalarOperand(ReadOnlySpan<byte> bytes, int field)
    {
        _bytes = bytes;
        IsBorrowed = true;
        _field = field;
    }

    internal AwkScalarOperand(AwkValue classification, ReadOnlySpan<byte> bytes)
    {
        Value = classification;
        _bytes = bytes;
        IsBorrowed = true;
    }

    internal AwkByteTextOperand Text(AwkRuntimeState state, bool forceScalar = false) => IsBorrowed
        ? new(_bytes) : new(state.FormatStringByteText(forceScalar ? Value.ForceScalar() : Value));

    internal void PrepareRangeSource(AwkRuntimeState state, scoped ref AwkByteTextOperand source, int start, int length)
    {
        if (IsBorrowed && _field >= 0 && !source.IsOwned && state.StringUnitMode != AwkStringUnitMode.Byte &&
            _bytes.Length > AwkRuntimeMemory.TextRuneCheckpointStride &&
            (long)start + length > AwkRuntimeMemory.TextRuneCheckpointStride)
            source = new(state.CaptureFieldStringText(_field));
    }

    internal AwkText OwnSlice(AwkRuntimeState state, in AwkByteTextOperand slice, bool whole, AwkMemoryLease destination) =>
        state.OwnStringResult(slice, IsBorrowed ? _field : -1, whole, destination);
}
