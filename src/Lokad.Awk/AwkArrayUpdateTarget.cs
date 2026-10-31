namespace Lokad.Awk;

// Keep an owned entry cell, never a dictionary reference. Retired cells hold no payload.
internal readonly record struct AwkArrayUpdateTarget(AwkArrayKey Key, double Previous,
    AwkArrayIdentity? Array, AwkArrayCell? Cell = null)
{
    public long RetainedTargetBytes => AwkRuntimeMemory.CheckedAdd(Key.RetainedTextMemoryBytes,
        Cell is null ? 0 : AwkRuntimeMemory.ArrayCellBytes);
}
