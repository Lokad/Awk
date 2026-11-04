namespace Lokad.Awk;

internal readonly struct AwkResolvedGetlineTarget : IDisposable
{
    private readonly bool _hasReference;
    private readonly AwkResolvedReference _reference;

    private AwkResolvedGetlineTarget(AwkResolvedReference reference)
    {
        _hasReference = true;
        _reference = reference;
    }

    public static AwkResolvedGetlineTarget CurrentRecord => default;
    public static AwkResolvedGetlineTarget ReferenceTarget(AwkResolvedReference reference) => new(reference);
    public bool IsCurrentRecord => !_hasReference;
    public long RetainedKeyBytes => _hasReference ? _reference.RetainedKeyBytes : 0;

    public void Dispose() => _reference.Dispose();

    public void AssignRecord(AwkRuntimeState state, AwkText record, AwkRecordFieldSplittingMode fieldSplittingMode)
    {
        if (_hasReference)
        {
            _reference.AssignValue(state, state.CreateNumericStringValue(record));
        }
        else
        {
            state.SetRecordFieldSplittingMode(fieldSplittingMode);
            state.SetCurrentRecord(record);
        }
    }
}
