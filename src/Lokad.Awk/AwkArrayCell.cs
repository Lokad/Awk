namespace Lokad.Awk;

// Dictionary entries own these cells. Captured updates can keep a cell across the RHS;
// retirement clears its payload before releasing the containing array's charge.
internal sealed class AwkArrayCell(AwkValue slot)
{
    private AwkValue _slot = slot;
    public AwkValue Slot => _slot;
    public bool IsAttached { get; private set; } = true;
    // Synchronous owner consumers need only the role/reference, not a value copy.
    internal AwkArrayIdentity? ArrayBinding => _slot.TryGetArrayBinding(out var array) ? array : null;
    // The scoped numeric kernel captures its value before conversion can poll.
    internal ref readonly AwkValue BorrowedSlot => ref _slot;
    // The owner preflights storage changes before calling this method.
    public void SetSlot(AwkValue value) => _slot = value;

    public void Detach()
    {
        _slot = default;
        IsAttached = false;
    }
}
