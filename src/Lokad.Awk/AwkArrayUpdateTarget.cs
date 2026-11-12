namespace Lokad.Awk;

// Capture the owning key and old number. Dictionary cells survive rehash; ordinal
// locations are resolved again at commit. Never retain an entry/backing reference.
internal readonly record struct AwkArrayUpdateTarget(AwkArrayKey Key, double Previous,
    AwkArrayIdentity? Array, AwkArrayCell? Cell = null)
{
    // Inline/ordinal keys own their spelling in their words, without text payload.
    internal bool CanRetainLocation => Key.RequiresTextMaterialization &&
        Array is { IsDetached: false } && Cell is { IsAttached: true };

    public long RetainedTargetBytes => AwkRuntimeMemory.CheckedAdd(Key.RetainedTextMemoryBytes,
        Cell is null ? 0 : AwkRuntimeMemory.ArrayCellBytes);
}
