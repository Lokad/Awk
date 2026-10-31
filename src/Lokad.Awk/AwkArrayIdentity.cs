namespace Lokad.Awk;

// Bind this owner, not its replaceable dictionary. RHS evaluation may clear/split it.
internal sealed class AwkArrayIdentity
{
    private static readonly Dictionary<AwkArrayKey, AwkArrayCell> DetachedSlots = new(AwkArrayKeyComparer.Instance);
    public AwkArrayIdentity(Dictionary<AwkArrayKey, AwkArrayCell> slots, AwkArrayIdentity? parent = null)
    {
        Slots = slots;
        Parent = parent;
        AdoptChildren();
    }

    public Dictionary<AwkArrayKey, AwkArrayCell> Slots { get; private set; }

    public AwkArrayIdentity? Parent { get; private set; }

    public bool IsDetached { get; private set; }

    public void Detach()
    {
        // Removed owners can still be in argument/target staging. They retain no old
        // backing or payload; mutation must resolve a live target before using storage.
        Slots = DetachedSlots;
        Parent = null;
        IsDetached = true;
    }

    public bool IsWithin(AwkArrayIdentity ancestor, Action? checkpoint = null)
    {
        var visited = 0;
        for (AwkArrayIdentity? current = this; current is not null; current = current.Parent)
        {
            if ((visited++ & 63) == 0) checkpoint?.Invoke();
            if (ReferenceEquals(current, ancestor)) return true;
        }
        return false;
    }

    public void ReplaceSlots(Dictionary<AwkArrayKey, AwkArrayCell> replacement)
    {
        Slots = replacement;
        AdoptChildren();
    }

    private void AdoptChildren()
    {
        foreach (var pair in Slots)
            if (pair.Value.TryGetArrayBinding(out var child)) child.Parent = this;
    }
}
