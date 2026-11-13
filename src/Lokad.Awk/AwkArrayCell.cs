namespace Lokad.Awk;

// Dictionary entries own these cells. Captured updates can keep a cell across the RHS;
// retirement clears its payload before releasing the containing array's charge.
internal sealed class AwkArrayCell(AwkValue slot)
{
    private AwkValue _slot = slot;
    public ref readonly AwkValue Slot => ref _slot;
    public bool IsAttached { get; private set; } = true;
    // The owner preflights storage changes before calling this method.
    public void SetSlot(AwkValue value) => _slot = value;

    public void Detach()
    {
        _slot = default;
        IsAttached = false;
    }
}
