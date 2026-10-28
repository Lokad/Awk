namespace Lokad.Awk;

// A builtin's array argument can name a root or carry a child/origin already
// evaluated in the caller. Promotion waits until the builtin consumes its arrays.
internal readonly record struct AwkArrayTarget(AwkArrayName Name, AwkValue Reference)
{
    public AwkArrayTarget(AwkArrayName name) : this(name, default) { }
    public AwkArrayTarget(AwkValue reference) : this(default, reference) { }
    public bool IsNamed => Name.Text.Length != 0;
    public long RetainedBytes => TryGetArray(out _) ? AwkRuntimeMemory.ArrayIdentityBytes : Reference.RetainedTextMemoryBytes;

    public bool TryGetArray([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array) =>
        Reference.TryGetArrayBinding(out array);
}
