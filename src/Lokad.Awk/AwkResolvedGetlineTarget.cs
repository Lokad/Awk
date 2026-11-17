namespace Lokad.Awk;

internal readonly struct AwkResolvedGetlineTarget : IDisposable
{
    // Default-initialized targets publish into $0; named targets own a reference.
    private readonly bool _hasReference;
    private readonly AwkResolvedReference _reference;

    private AwkResolvedGetlineTarget(AwkResolvedReference reference)
    {
        _hasReference = true;
        _reference = reference;
    }

    public static AwkResolvedGetlineTarget ReferenceTarget(AwkResolvedReference reference) => new(reference);
    public bool IsCurrentRecord => !_hasReference;
    public long RetainedKeyBytes => _hasReference ? _reference.RetainedKeyBytes : 0;

    public void Dispose() => _reference.Dispose();

    // ApplyResult publishes implicit-record targets before reaching this named-target path.
    public void AssignRecord(AwkRuntimeState state, AwkText record) =>
        _reference.AssignValue(state, state.CreateNumericStringValue(record));
}
