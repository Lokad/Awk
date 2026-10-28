namespace Lokad.Awk;

// CSV framing belongs to the record cursor; this decoder also serves split(text,array).
internal static class AwkCsvFields
{
    public static long EstimateMemory(AwkText text, bool materialize, Action checkpoint)
    {
        var scanner = new Scanner(text.AsSpan(checkpoint), checkpoint);
        var count = 0;
        var bytes = 0L;
        while (scanner.MoveNext(out var field))
        {
            count++;
            if ((materialize || field.HasEscapes) && field.Length > 0)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateTextBytes(field.DecodedLength));
        }
        return AwkRuntimeMemory.CheckedAdd(bytes, materialize
            ? AwkRuntimeMemory.EstimateReferenceListBytes(count)
            : AwkRuntimeMemory.EstimateFieldTextListBytes(count));
    }

    public static List<AwkFieldText> SplitRecord(AwkText text, AwkMemoryAccounting memory,
        AwkRuntimeCounters counters, Action checkpoint)
    {
        var count = Count(text, checkpoint);
        var fields = new List<AwkFieldText>(count);
        var scanner = new Scanner(text.AsSpan(checkpoint), checkpoint);
        while (scanner.MoveNext(out var field))
            fields.Add(field.HasEscapes
                ? AwkFieldText.FromOwned(Decode(text, field, memory, counters, checkpoint))
                : AwkFieldText.FromRecordSlice(field.Start, field.Length));
        return fields;
    }

    public static AwkTextSplitResult SplitText(AwkText text, AwkMemoryAccounting memory,
        AwkRuntimeCounters counters, Action checkpoint)
    {
        var fields = new List<AwkText>(Count(text, checkpoint));
        var scanner = new Scanner(text.AsSpan(checkpoint), checkpoint);
        while (scanner.MoveNext(out var field))
            fields.Add(Decode(text, field, memory, counters, checkpoint));
        return new(fields, []);
    }

    private static int Count(AwkText text, Action checkpoint)
    {
        var scanner = new Scanner(text.AsSpan(checkpoint), checkpoint);
        var count = 0;
        while (scanner.MoveNext(out _)) count++;
        return count;
    }

    private static AwkText Decode(AwkText text, Field field, AwkMemoryAccounting memory,
        AwkRuntimeCounters counters, Action checkpoint)
    {
        if (!field.HasEscapes)
            return text.CopyByteSlice(field.Start, field.Length, counters, memory, checkpoint);
        using var builder = new AwkUtf8Builder(memory, field.DecodedLength);
        var bytes = text.AsSpan(checkpoint).Slice(field.Start, field.Length);
        var start = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            if ((i & (AwkRuntimeLimits.CancellationCheckBytes - 1)) == 0) checkpoint();
            if (bytes[i] != (byte)'"' || i + 1 == bytes.Length || bytes[i + 1] != (byte)'"') continue;
            builder.Append(bytes[start..(i + 1)], checkpoint);
            start = i + 2;
            i++;
        }
        builder.Append(bytes[start..], checkpoint);
        return builder.CopyToAwkText(counters, checkpoint);
    }

    private readonly record struct Field(int Start, int Length, int DecodedLength, bool HasEscapes);

    private ref struct Scanner(ReadOnlySpan<byte> bytes, Action checkpoint)
    {
        private readonly ReadOnlySpan<byte> _bytes = bytes;
        private readonly Action _checkpoint = checkpoint;
        private int _next;
        private bool _finished = bytes.IsEmpty;

        public bool MoveNext(out Field field)
        {
            field = default;
            if (_finished) return false;
            _checkpoint();
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
