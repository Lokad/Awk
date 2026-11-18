namespace Lokad.Awk;

internal enum AwkCounter
{
    RecordsRead, BytesRead, OutputBytes, HostCalls,
    FieldsPrepared, FieldSplits, StringDecodes, Utf8Encodes,
    TextAllocations, TextBytes, InputRecordBytesCopied, ConcatenationBytesCopied,
    Count
}

// Observations only: no work units, admission decisions or execution limits.
// A supplied instance may collect independent concurrent commands.
internal sealed class AwkRuntimeCounters
{
    private readonly long[]? _values;

    private AwkRuntimeCounters(bool enabled)
    {
        if (enabled) _values = new long[(int)AwkCounter.Count];
    }

    public static AwkRuntimeCounters Disabled { get; } = new(false);
    public static AwkRuntimeCounters CreateEnabled() => new(true);
    public bool IsEnabled => _values is not null;

    public long this[AwkCounter counter] =>
        _values is { } values ? Volatile.Read(ref values[(int)counter]) : 0;

    internal void ObserveTextAllocation(int byteLength)
    {
        if (byteLength == 0) return;
        Add(AwkCounter.TextAllocations);
        Add(AwkCounter.TextBytes, byteLength);
    }

    public void Add(AwkCounter counter, long amount = 1)
    {
        if (_values is { } values && amount > 0)
            Interlocked.Add(ref values[(int)counter], amount);
        }
    }
