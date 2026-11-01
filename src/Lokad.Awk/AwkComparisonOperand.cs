namespace Lokad.Awk;

// Only prepared computation can carry this stack-local field view. Record-invalidating
// or suspending operands keep owned capture. Numeric classification never
// escapes into scalar storage; its original spelling remains in the record frame.
internal readonly ref struct AwkComparisonOperand
{
    private readonly ReadOnlySpan<byte> _bytes;
    internal AwkValue Value { get; }
    internal bool IsBorrowed { get; }

    internal AwkComparisonOperand(AwkValue value) => Value = value;

    internal AwkComparisonOperand(AwkValue classification, ReadOnlySpan<byte> bytes)
    {
        Value = classification;
        _bytes = bytes;
        IsBorrowed = true;
    }

    internal AwkMemoryLease Retain(AwkRuntimeState state) => IsBorrowed
        ? AwkMemoryLease.Disabled : state.MemoryAccounting.RetainValue(Value);

    internal AwkByteTextOperand Text(AwkRuntimeState state) => IsBorrowed
        ? new(_bytes) : new(state.FormatStringByteText(Value));
}
