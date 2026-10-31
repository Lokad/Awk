using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Array entries own these cells. Captured updates can keep a cell across the RHS;
// retirement clears its payload before releasing the containing array's charge.
internal sealed class AwkArrayCell(AwkVariableSlot slot)
{
    public AwkVariableSlot Slot { get; private set; } = slot;
    public bool IsAttached { get; private set; } = true;
    public AwkVariableSlotKind Kind => Slot.Kind;
    public bool IsUntyped => Slot.IsUntyped;
    public long RetainedTextMemoryBytes => Slot.RetainedTextMemoryBytes;

    public bool TryGetArrayBinding([NotNullWhen(true)] out AwkArrayIdentity? array) =>
        Slot.TryGetArrayBinding(out array);

    public bool TryGetUntypedScalar([NotNullWhen(true)] out AwkUntypedScalar? origin) =>
        Slot.TryGetUntypedScalar(out origin);

    // The owner preflights storage changes before calling this method.
    public void SetSlot(AwkVariableSlot value) => Slot = value;

    public void Detach()
    {
        Slot = default;
        IsAttached = false;
    }
}
