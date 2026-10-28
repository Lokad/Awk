namespace Lokad.Awk;

// Bind this owner, not its replaceable dictionary. RHS evaluation may clear/split it.
internal sealed class AwkArrayIdentity(Dictionary<AwkArrayKey, AwkVariableSlot> slots)
{
    public Dictionary<AwkArrayKey, AwkVariableSlot> Slots { get; private set; } = slots;

    public void ReplaceSlots(Dictionary<AwkArrayKey, AwkVariableSlot> replacement) => Slots = replacement;
}
