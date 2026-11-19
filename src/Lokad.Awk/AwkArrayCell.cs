namespace Lokad.Awk;

// Dictionary entries own these cells. Captured updates can keep a cell across the RHS;
// retirement clears its payload before releasing the containing array's charge.
internal sealed class AwkArrayCell(AwkValue slot)
{
    private AwkValue _slot = slot;

    public bool IsAttached => _slot.StoredKind != AwkValueKind.DetachedArrayCell;
    public AwkValue Slot => IsAttached ? _slot : default;
    // Synchronous owner consumers need only the role/reference, not a value copy.
    internal AwkArrayIdentity? ArrayBinding => _slot.TryGetArrayBinding(out var array) ? array : null;
    // The scoped numeric kernel captures its value before conversion can poll.
    internal ref readonly AwkValue BorrowedSlot => ref _slot;
    // The owner preflights storage changes before calling this method.
    public void SetSlot(AwkValue value)
    {
        if (IsAttached) _slot = value;
    }

    // The synchronous caller has read this attached number and reached no
    // operation that can mutate the array before replacing it.
    internal void ReplaceNumber(double value)
    {
        System.Diagnostics.Debug.Assert(_slot.IsNumberValue);
        _slot = AwkValue.FromNumber(value);
    }

    public void Detach() => _slot = AwkValue.FromStorage(null, 0, AwkValueKind.DetachedArrayCell);
}
