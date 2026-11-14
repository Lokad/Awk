namespace Lokad.Awk;

// Dictionary entries own these cells. Captured updates can keep a cell across the RHS;
// retirement clears its payload before releasing the containing array's charge.
internal sealed class AwkArrayCell(AwkValue slot)
{
    public AwkValue Slot { get; private set; } = slot;
    public bool IsAttached { get; private set; } = true;
    // Synchronous owner consumers need only the role/reference, not a value copy.
    internal AwkArrayIdentity? ArrayBinding => Slot.TryGetArrayBinding(out var array) ? array : null;
    // The owner preflights storage changes before calling this method.
    public void SetSlot(AwkValue value) => Slot = value;

    public void Detach()
    {
        Slot = default;
        IsAttached = false;
    }
}
