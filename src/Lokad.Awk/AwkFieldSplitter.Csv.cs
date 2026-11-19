namespace Lokad.Awk;

// CSV framing belongs to the record cursor; this decoder also serves split(text,array).
internal sealed partial class AwkFieldSplitter
{
    public (int Count, long Bytes) MeasureCsvSplitText(AwkText text)
    {
        var scanner = new CsvScanner(text.AsSpan(state.CancellationCheckpoint), state.CancellationCheckpoint);
        var count = 0;
        if (!state.MemoryAccounting.IsEnabled)
        {
            while (scanner.MoveNext(out _)) count++;
            return (count, 0);
        }
        var bytes = 0L;
        while (scanner.MoveNext(out var field))
        {
            count++;
            if (field.Length > 0)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateTextBytes(field.DecodedLength));
        }
        return (count, AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateReferenceListBytes(count)));
    }

    private void FillCsvRecord(ReadOnlySpan<byte> text, ref AwkRecordFieldWriter writer)
    {
        var scanner = new CsvScanner(text, state.CancellationCheckpoint);
        while (scanner.MoveNext(out var field))
        {
            if (field.HasEscapes)
            {
                // Keep the returned text funded until the writer takes retained ownership.
                using var admission = state.MemoryAccounting.IsEnabled
                    ? state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.EstimateTextBytes(field.DecodedLength)) : default;
                state.CancellationCheckpoint();
                writer.AddOwned(DecodeCsvField(text, field));
            }
            else writer.Add(new(field.Start, field.Length));
        }
    }

    public AwkTextSplitResult SplitCsvText(AwkText text, int count)
    {
        var bytes = text.AsSpan(state.CancellationCheckpoint);
        state.CancellationCheckpoint();
        var fields = new List<AwkText>(count);
        var scanner = new CsvScanner(bytes, state.CancellationCheckpoint);
        while (scanner.MoveNext(out var field))
            fields.Add(DecodeCsvField(bytes, field));
        return new(fields, []);
    }

    private AwkText DecodeCsvField(ReadOnlySpan<byte> text, CsvField field)
    {
        if (!field.HasEscapes)
            return state.CopyText(text.Slice(field.Start, field.Length));
        // Both callers have admitted the decoded text using the scanner's exact length.
        var decoded = new byte[field.DecodedLength];
        var bytes = text.Slice(field.Start, field.Length);
        var start = 0;
        var written = 0;
        var bytesUntilCheck = AwkRuntimeLimits.CancellationCheckBytes;
        long nextCheckpoint = 0;
        var checkpoint = state.CancellationCheckpoint;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i >= nextCheckpoint)
            {
                checkpoint();
                nextCheckpoint = (long)i + AwkRuntimeLimits.CancellationCheckBytes;
            }
            if (bytes[i] != (byte)'"' || i + 1 == bytes.Length || bytes[i + 1] != (byte)'"') continue;
            AwkText.CopyFragment(bytes[start..(i + 1)], decoded, ref written, ref bytesUntilCheck, checkpoint);
            start = i + 2;
            i++;
        }
        AwkText.CopyFragment(bytes[start..], decoded, ref written, ref bytesUntilCheck, checkpoint);
        state.ThrowIfCancellationRequested();
        return state.AdoptText(decoded);
    }

    private readonly record struct CsvField(int Start, int Length, int DecodedLength, bool HasEscapes);

    private ref struct CsvScanner(ReadOnlySpan<byte> bytes, Action checkpoint)
    {
        private readonly ReadOnlySpan<byte> _bytes = bytes;
        private readonly Action _checkpoint = checkpoint;
        private int _next;
        private bool _finished = bytes.IsEmpty;

        public bool MoveNext(out CsvField field)
        {
            field = default;
            if (_finished) return false;
            var start = _next;
            var position = start;
            var decodedLength = 0;
            var escaped = false;
            var quoted = position < _bytes.Length && _bytes[position] == (byte)'"';
            if (quoted) start = ++position;
            var end = start;
            long nextCheckpoint = position;
            while (position < _bytes.Length)
            {
                if (position >= nextCheckpoint)
                {
                    _checkpoint();
                    nextCheckpoint = (long)position + AwkRuntimeLimits.CancellationCheckBytes;
                }
                var value = _bytes[position];
                if (!quoted && value == (byte)',') break;
                if (quoted && value == (byte)'"')
                {
                    if (position + 1 < _bytes.Length && _bytes[position + 1] == (byte)'"')
                    {
                        escaped = true;
                        position += 2;
                        decodedLength++;
                        end = position;
                        continue;
                    }
                    if (position + 1 == _bytes.Length || _bytes[position + 1] == (byte)',')
                    {
                        end = position++;
                        break;
                    }
                }
                position++;
                decodedLength++;
                end = position;
            }
            field = new(start, end - start, decodedLength, escaped);
            if (position < _bytes.Length)
                _next = position + 1;
            else
                _finished = true;
            return true;
        }
    }
}
