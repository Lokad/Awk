namespace Lokad.Awk;

internal static class AwkRegionPrinting
{
    internal static void Append(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output, AwkRegionPrint print)
    {
        var fields = storage.PrintSegments.AsSpan(print.Segments, print.Count);
        var borrowed = storage.BorrowedPrintFields.AsSpan(print.Segments, print.Count);
        ReadOnlySpan<byte> record = default;
        if (print.Fields.Length != 0)
        {
            // All addresses are fixed and cannot mutate or suspend. Views are
            // consumed in this synchronous primitive, before any host append.
            state.Counters.RecordFieldListPrint();
            for (var index = 0; index < print.Count; index++)
            {
                if ((index & 63) == 0) state.ThrowIfCancellationRequested();
                var field = print.Fields[index];
                borrowed[index] = state.TryPreparePrintField(field, out fields[index], out record);
                if (!borrowed[index]) storage.StoreValue(print.Values + index,
                    state.GetFieldValue(field), preserveOrigin: true);
            }
        }
        else if (print.Count <= 4) state.Counters.RecordSmallValuePrint();

        // Every argument has been evaluated. Preserve separator/terminator and
        // per-value formatting order, including untyped scalar effects and OFMT.
        var separator = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator);
        var terminator = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        storage.StoreText(print.Texts + print.Count, state.FormatStringByteText(separator));
        storage.StoreText(print.Texts + print.Count + 1, state.FormatStringByteText(terminator));
        for (var index = 0; index < print.Count; index++)
        {
            if ((index & 63) == 0) state.ThrowIfCancellationRequested();
            if (print.Fields.Length != 0 && borrowed[index]) continue;
            var text = state.FormatPrintByteText(storage.Values[print.Values + index]);
            storage.MoveValueToText(print.Values + index, print.Texts + index, text);
            fields[index] = AwkFieldText.FromOwned(text);
        }
        state.ThrowIfCancellationRequested();
        output.AppendRecord(fields, record, storage.Texts[print.Texts + print.Count], storage.Texts[print.Texts + print.Count + 1]);
        storage.ReleasePrint(print);
    }
}
