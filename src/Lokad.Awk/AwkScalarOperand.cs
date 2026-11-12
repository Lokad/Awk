namespace Lokad.Awk;

// Comparisons retain their scalar classification and may borrow stable field
// spelling. Effects and suspension require owned capture.
internal readonly ref struct AwkScalarOperand
{
    private readonly ReadOnlySpan<byte> _bytes;
    internal AwkValue Value { get; }
    internal bool IsBorrowed { get; }

    internal AwkScalarOperand(AwkValue value) => Value = value;

    internal AwkScalarOperand(AwkValue classification, ReadOnlySpan<byte> bytes)
    {
        Value = classification;
        _bytes = bytes;
        IsBorrowed = true;
    }

    internal AwkByteTextOperand Text(AwkRuntimeState state) => IsBorrowed
        ? new(_bytes) : new(state.FormatStringByteText(Value));
}
