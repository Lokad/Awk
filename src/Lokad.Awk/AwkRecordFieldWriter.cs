namespace Lokad.Awk;

internal readonly record struct AwkFieldRange(int Start, int Length);

internal interface IAwkFieldSink<out TFields>
{
    void Add(AwkFieldRange field);
    TFields Complete();
}

// The frame owns destinations. Its scoped writer carries the current production
// lease through publication or interrupted-scan cleanup.
internal ref struct AwkRecordFieldWriter : IAwkFieldSink<int>
{
    private readonly List<AwkRecordField> _fields;
    private List<AwkFieldFrame.CachedFieldValue> _values;
    private readonly AwkMemoryAccounting _accounting;
    private readonly AwkRuntimeCounters _counters;
    private readonly Action _checkpoint;
    private AwkMemoryLease _staging;

    public AwkRecordFieldWriter(List<AwkRecordField> fields, List<AwkFieldFrame.CachedFieldValue> values, AwkMemoryAccounting accounting,
        AwkRuntimeCounters counters, Action checkpoint, AwkMemoryLease staging)
    {
        _fields = fields;
        _values = values;
        _accounting = accounting;
        _counters = counters;
        _checkpoint = checkpoint;
        _staging = staging;
    }

    public int PayloadBytes { get; private set; }
    internal readonly List<AwkFieldFrame.CachedFieldValue> Values => _values;
    internal readonly AwkMemoryLease Staging => _staging;

    public void Dispose()
    {
        _staging.Dispose();
        _staging = AwkMemoryLease.Disabled;
    }

    public void Add(AwkFieldRange field)
    {
        EnsureCapacity();
        _fields.Add(AwkRecordField.FromRecordSlice(field.Start, field.Length));
        // Producer ranges are disjoint and bounded by the admitted record.
        PayloadBytes += field.Length;
    }

    public void AddOwned(AwkText text)
    {
        if (text.ByteLength == 0)
        {
            Add(default);
            return;
        }
        EnsureCapacity();
        if (_accounting.IsEnabled)
        {
            var retained = text.EstimatedRetainedMemoryBytes;
            Fund(retained);
        }
        if (_values.Count == _values.Capacity)
        {
            var capacity = _values.Count == 0 ? 4 : AwkRuntimeMemory.CheckedToInt32(
                AwkRuntimeMemory.CheckedMultiply(_values.Capacity, 2));
            Fund(AwkRuntimeMemory.EstimateFieldValueListBytes(capacity));
            var values = new List<AwkFieldFrame.CachedFieldValue>(capacity);
            _counters.RecordFieldValueCacheBufferAllocation();
            for (var index = 0; index < _values.Count; index++)
            {
                if ((index & 1023) == 0) _checkpoint();
                values.Add(_values[index]);
            }
            _values = values;
        }
        _values.Add(new(_fields.Count, AwkFieldCacheKind.TextOnly, default, text));
        _fields.Add(AwkRecordField.FromOwned(_values.Count, text.ByteLength));
        PayloadBytes += text.ByteLength;
    }

    public readonly int Complete() => PayloadBytes;

    private void Fund(long bytes)
    {
        if (ReferenceEquals(_staging, AwkMemoryLease.Disabled))
            _staging = _accounting.CreateTransientLease();
        _staging.ReserveAdditional(bytes);
        _checkpoint();
    }

    private void EnsureCapacity()
    {
        if (_fields.Count != _fields.Capacity) return;
        var capacity = _fields.Capacity == 0 ? 4 : AwkRuntimeMemory.CheckedToInt32(
            AwkRuntimeMemory.CheckedMultiply(_fields.Capacity, 2));
        Fund(AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity));
        _fields.EnsureCapacity(capacity);
    }
}
