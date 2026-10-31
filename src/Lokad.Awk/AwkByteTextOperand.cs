namespace Lokad.Awk;

// Immediate consumers may borrow bytes; values that escape still own AwkText.
// Stack-only storage prevents a record view from crossing a suspension.
internal readonly ref struct AwkByteTextOperand
{
    private readonly AwkText? _owned;
    private readonly ReadOnlySpan<byte> _bytes;

    public AwkByteTextOperand(AwkText owned) => _owned = owned;
    public AwkByteTextOperand(ReadOnlySpan<byte> bytes) => _bytes = bytes;

    public ReadOnlySpan<byte> AsSpan(Action checkpoint)
    {
        // Keep owned flattening at the consumer's original point, after provider pinning.
        if (_owned is { } owned) return owned.AsSpan(checkpoint);
        checkpoint();
        return _bytes;
    }
}
