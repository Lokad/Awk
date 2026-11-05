namespace Lokad.Awk;

internal readonly record struct AwkFieldRange(int Start, int Length);

internal interface IAwkFieldSink<out TFields>
{
    void Add(AwkFieldRange field);
    TFields Complete();
}

// The frame owns the destination and funding handle, including interrupted scans.
internal ref struct AwkRecordFieldWriter : IAwkFieldSink<int>
{
    private readonly List<AwkFieldText> _fields;
    private readonly AwkMemoryAccounting _accounting;
    private readonly Action _checkpoint;
    private readonly ref AwkMemoryLease _staging;

    public AwkRecordFieldWriter(List<AwkFieldText> fields, AwkMemoryAccounting accounting,
        Action checkpoint, ref AwkMemoryLease staging)
    {
        _fields = fields;
        _accounting = accounting;
        _checkpoint = checkpoint;
        _staging = ref staging;
    }

    public int PayloadBytes { get; private set; }
    public long OwnedTextBytes { get; private set; }
    public readonly int Count => _fields.Count;

    public void Add(AwkFieldRange field)
    {
        EnsureCapacity();
        _fields.Add(AwkFieldText.FromRecordSlice(field.Start, field.Length));
        // Producer ranges are disjoint and bounded by the admitted record.
        PayloadBytes += field.Length;
    }

    public void AddOwned(AwkText text)
    {
        var field = AwkFieldText.FromOwned(text);
        if (_accounting.IsEnabled)
        {
            var retained = field.RetainedTextByteLength;
            Fund(retained);
            OwnedTextBytes = AwkRuntimeMemory.CheckedAdd(OwnedTextBytes, retained);
        }
        EnsureCapacity();
        _fields.Add(field);
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
        Fund(AwkRuntimeMemory.EstimateFieldTextListBytes(capacity));
        _fields.EnsureCapacity(capacity);
    }
}
