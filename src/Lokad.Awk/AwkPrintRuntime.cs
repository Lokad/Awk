using System;
using System.Buffers;

namespace Lokad.Awk;

internal static class AwkPrintRuntime
{
    internal static void AppendFields(AwkOutputSink output, AwkExpression[] expressions,
        int[] fields, AwkEvaluationContext context)
    {
        var state = context.State;
        Span<AwkValue> values = [default, default, default, default];
        Span<AwkFieldText> segments = [AwkFieldText.Empty, AwkFieldText.Empty, AwkFieldText.Empty,
            AwkFieldText.Empty, AwkFieldText.Empty, AwkFieldText.Empty, AwkFieldText.Empty, AwkFieldText.Empty];
        var record = AwkText.Empty;
        var borrowed = 0;
        for (var index = 0; index < fields.Length; index++)
        {
            AwkExpression.EvaluateLiteralFieldIndex(expressions[index], context);
            if (state.TryPreparePrintField(fields[index], out segments[index * 2], out record))
                borrowed |= 1 << index;
            else values[index] = state.GetFieldValue(fields[index]);
        }
        // Literal indices cannot mutate or suspend: every slice still refers to this record.
        var ofs = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator);
        var ors = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        state.CancellationCheckpoint();
        state.Counters.RecordFieldListPrint();
        using var lease = state.MemoryAccounting.CreateTransientLease();
        var separator = AwkFieldText.FromOwned(state.FormatStringByteText(ofs, lease));
        var terminator = AwkFieldText.FromOwned(state.FormatStringByteText(ors, lease));
        state.CancellationCheckpoint();
        for (var index = 0; index < fields.Length; index++)
        {
            if (index > 0) segments[index * 2 - 1] = separator;
            if ((borrowed & (1 << index)) == 0)
                segments[index * 2] = AwkFieldText.FromOwned(state.FormatPrintByteText(values[index], lease));
        }
        segments[fields.Length * 2 - 1] = terminator;
        state.CancellationCheckpoint();
        output.Append(segments[..(fields.Length * 2)], record);
    }

    internal static void AppendSmall(AwkOutputSink output, ReadOnlySpan<AwkValue> values, int count, AwkRuntimeState state)
    {
        var outputFieldSeparator = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator);
        var outputRecordSeparator = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        state.CancellationCheckpoint();
        state.Counters.RecordSmallValuePrint();
        using var lease = state.MemoryAccounting.CreateTransientLease();
        var separator = state.FormatStringByteText(outputFieldSeparator, lease);
        var terminator = state.FormatStringByteText(outputRecordSeparator, lease);
        Span<AwkText> segments = [AwkText.Empty, AwkText.Empty, AwkText.Empty, AwkText.Empty,
            AwkText.Empty, AwkText.Empty, AwkText.Empty, AwkText.Empty];
        state.CancellationCheckpoint();
        for (var index = 0; index < count; index++)
        {
            if (index > 0) segments[index * 2 - 1] = separator;
            segments[index * 2] = state.FormatPrintByteText(values[index], lease);
        }
        segments[count * 2 - 1] = terminator;
        state.CancellationCheckpoint();
        output.Append(segments[..(count * 2)]);
    }

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
