namespace Lokad.Awk;

// Program metadata only. Array identities and repaired aliases remain execution-owned.
internal readonly struct AwkArrayBinding
{
    private readonly int _index;

    private AwkArrayBinding(int index) => _index = index;

    public static AwkArrayBinding Global(int index) => new(checked(index + 1));

    public static AwkArrayBinding Local(AwkFunctionLocalSlotId slot) => new(~slot.Index);

    public bool TryGetGlobalIndex(out int index)
    {
        index = _index - 1;
        return _index > 0;
    }

    public bool TryGetLocalSlot(out AwkFunctionLocalSlotId slot)
    {
        slot = new(~_index);
        return _index < 0;
    }
}
