using System.Numerics;

namespace Lokad.Awk;

// The frame owns both buffers. Main-input staging only writes the inactive one;
// plain getline keeps owned input and cannot invalidate the current record.
internal sealed class AwkRecordStorage(AwkMemoryAccounting memory,
    AwkRuntimeCounters counters, Action checkpoint) : IDisposable
{
    private const int MaxReusableRecordBytes = 4096;
    private AwkUtf8Builder? _current;
    private AwkUtf8Builder? _spare;

    internal ReadOnlyMemory<byte> Content => _current?.AsMemory() ?? ReadOnlyMemory<byte>.Empty;

    internal static bool CanStage(int length) => length is > 0 and <= MaxReusableRecordBytes;

    // The cursor proves UTF-8 validity before entering the funded storage writer.
    internal void StageValidated(ReadOnlySpan<byte> bytes)
    {
        var reused = _spare is not null;
        _spare ??= new(memory, (int)BitOperations.RoundUpToPowerOf2((uint)bytes.Length));
        var next = _spare;
        next.Clear();
        next.Append(bytes, checkpoint);
        checkpoint();
        _spare = _current;
        _current = next;
        counters.RecordBufferedInputRecord(reused);
    }

    public void Dispose()
    {
        _current?.Dispose();
        _spare?.Dispose();
        _current = null;
        _spare = null;
    }
}
