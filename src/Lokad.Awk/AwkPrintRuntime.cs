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
}
