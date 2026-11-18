using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Capture the owning key and old number. Dictionary cells survive rehash; ordinal
// locations are resolved again at commit. Never retain an entry/backing reference.
internal readonly record struct AwkArrayUpdateTarget(AwkArrayKey Key, double Previous,
    AwkArrayIdentity? Array, AwkArrayCell? Cell = null)
{
    // Inline/ordinal keys own their spelling in their words, without text payload.
    internal bool CanRetainLocation => Key.RequiresTextMaterialization &&
        Array is { IsDetached: false } && Cell is { IsAttached: true };

    internal bool TryGetCurrentCell(AwkArrayIdentity array, AwkArrayKey key,
        [NotNullWhen(true)] out AwkArrayCell? cell) =>
        TryGetReusableCell(array, out cell) && Key.Equals(key, checkpoint: null);

    internal bool TryGetCurrentCell(AwkArrayIdentity array, ReadOnlySpan<byte> key,
        [NotNullWhen(true)] out AwkArrayCell? cell) =>
        TryGetReusableCell(array, out cell) && Key.Equals(key, checkpoint: null);

    private bool TryGetReusableCell(AwkArrayIdentity array, [NotNullWhen(true)] out AwkArrayCell? cell)
    {
        cell = Cell;
        return ReferenceEquals(Array, array) && !array.IsDetached && cell is { IsAttached: true } &&
            Key.RequiresTextMaterialization;
    }

    public long RetainedTargetBytes => AwkRuntimeMemory.CheckedAdd(Key.RetainedTextMemoryBytes,
        Cell is null ? 0 : AwkRuntimeMemory.ArrayCellBytes);
}
