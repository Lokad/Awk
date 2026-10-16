using System;
using System.Buffers;

namespace Lokad.Awk;

internal static class AwkPrintRuntime
{
    internal static void AppendSingle(AwkOutputSink output, AwkValue value, AwkRuntimeState state)
    {
        state.CancellationCheckpoint();
        state.Counters.RecordSingleValuePrint();
        using var lease = state.MemoryAccounting.CreateTransientLease();
        // Preserve print's terminator-before-value formatting order.
        var terminator = state.FormatStringByteText(
            state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator), lease);
        var content = state.FormatPrintByteText(value, lease);
        state.CancellationCheckpoint();
        output.Append([content, terminator]);
    }

    public static AwkPrintSegments Stage(
        ReadOnlySpan<AwkValue> values,
        AwkValue outputFieldSeparator,
        AwkValue outputRecordSeparator,
        AwkRuntimeState state)
    {
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        var capacity = AwkRuntimeMemory.CheckedToInt32(
            AwkRuntimeMemory.CheckedMultiply(values.Length, 2));
        state.CancellationCheckpoint();
        var lease = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateAwkTextArrayBytes(capacity));
        var pooled = !state.MemoryAccounting.IsEnabled;
        AwkText[] segments;
        try
        {
            segments = pooled ? ArrayPool<AwkText>.Shared.Rent(capacity) : new AwkText[capacity];
        }
        catch
        {
            lease.Dispose();
            throw;
        }
        var count = 0;
        try
        {
            var separator = values.Length > 1 ? state.FormatStringByteText(outputFieldSeparator, lease) : AwkText.Empty;
            var terminator = state.FormatStringByteText(outputRecordSeparator, lease);
            for (var index = 0; index < values.Length; index++)
            {
                if ((index & 63) == 0)
                    state.CancellationCheckpoint();
                if (index > 0)
                    segments[count++] = separator;

                segments[count++] = state.FormatPrintByteText(values[index], lease);
            }

            segments[count++] = terminator;
            state.CancellationCheckpoint();
            return new AwkPrintSegments(segments, count, lease, pooled);
        }
        catch
        {
            if (pooled)
                ArrayPool<AwkText>.Shared.Return(segments, clearArray: true);
            lease.Dispose();
            throw;
        }
    }
}

internal readonly struct AwkPrintSegments(AwkText[] values, int count, AwkMemoryLease lease, bool pooled) : IDisposable
{
    public ReadOnlySpan<AwkText> Segments => values.AsSpan(0, count);

    public void Dispose()
    {
        if (pooled)
            ArrayPool<AwkText>.Shared.Return(values, clearArray: true);
        else
            Array.Clear(values);
        lease.Dispose();
    }
}
