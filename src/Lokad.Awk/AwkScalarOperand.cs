namespace Lokad.Awk;

// Retain classification separately from the field's spelling. A record range
// survives admission while preparing the other operand.
internal readonly ref struct AwkScalarOperand
{
    private readonly AwkByteTextOperand _text;
    internal AwkValue Value { get; }
    internal bool IsBorrowed => _text.HasValue;

    internal AwkScalarOperand(AwkValue value) => Value = value;

    internal AwkScalarOperand(AwkValue classification, AwkByteTextOperand text)
    {
        Value = classification;
        _text = text;
    }

    internal AwkByteTextOperand Text(AwkRuntimeState state) => IsBorrowed
        ? _text : new(state.FormatStringByteText(Value));
}
