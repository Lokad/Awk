namespace Lokad.Awk;

internal static class AwkRegionPrinting
{
    internal static void Printf(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output, AwkRegionPrintf print)
    {
        var formatBytes = 0L;
        try
        {
            if (print.Fields.Length != 0)
            {
                for (var index = 0; index < print.Fields.Length; index++)
                {
                    if ((index & 63) == 0) state.ThrowIfCancellationRequested();
                    state.PreflightPrintfField(print.Fields[index]);
                }
            }
            if (print.Output.IsRedirected) storage.PrepareString(print.Output.Value);
            var format = storage.PrepareFormattedString(storage.Values[print.Format], out formatBytes);
            if (print.Fields.Length != 0)
                AppendPrintf(state, storage, output, print.Output, format, new AwkPrintfFieldArguments(print.Fields, state));
            else
                AppendPrintf(state, storage, output, print.Output, format, new AwkValueArrayPrintfArguments(storage.Values, print.Count, state, print.Values));
        }
        finally
        {
            if (print.Fields.Length == 0) storage.ReleaseValues(print.Values, print.Count);
            storage.ReleaseFormattedString(formatBytes);
            storage.ReleaseValue(print.Format);
        }
    }

    private static void AppendPrintf<TArguments>(AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink output, AwkRegionOutput target, string format, TArguments arguments)
        where TArguments : IPrintfArguments
    {
        if (target.IsRedirected)
        {
            var redirected = storage.Builders[0].Buffer;
            redirected.Clear();
            AwkFormattingRuntime.FormatPrintfToBuilder(redirected, format, arguments, state);
            state.ThrowIfCancellationRequested();
            return;
        }
        if (output.TryFormatPrintf(format, arguments, state)) return;
        using var formatted = new AwkUtf8Builder(state.MemoryAccounting, capacity: 0);
        AwkFormattingRuntime.FormatPrintfToBuilder(formatted, format, arguments, state);
        state.ThrowIfCancellationRequested();
        output.Append(formatted.AsMemory());
    }

    internal static int Append(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output, AwkRegionPrint print)
    {
        var fields = storage.PrintSegments.AsSpan(print.Segments, print.Count);
        var borrowed = storage.BorrowedPrintFields.AsSpan(print.Segments, print.Count);
        var fieldViews = print.UsesFieldViews;
        AwkUtf8Builder? numbers = null;
        if (!fieldViews)
        {
            numbers = storage.Builders[0].Buffer;
            numbers.Clear();
        }
        ReadOnlySpan<byte> record = default;
        if (fieldViews)
        {
            // All addresses are fixed and cannot mutate or suspend. Views are
            // consumed in this synchronous primitive, before any host append.
            state.Counters.RecordFieldListPrint();
            for (var index = 0; index < print.Count; index++)
            {
                if ((index & 63) == 0) state.ThrowIfCancellationRequested();
                var argument = print.Arguments[index];
                borrowed[index] = state.TryPreparePrintField(argument.Index, out fields[index], out record);
                if (!borrowed[index]) storage.StoreValue(argument.FallbackValue,
                    state.GetFieldValue(argument.Index), preserveOrigin: true);
            }
        }
        else if (print.Count <= 4) state.Counters.RecordSmallValuePrint();

        if (print.Output.IsRedirected) storage.PrepareString(print.Output.Value);

        // Every argument has been evaluated. Preserve separator/terminator and
        // per-value formatting order, including untyped scalar effects and OFMT.
        var separator = print.Count > 1 ? state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator)
            : AwkValue.FromText(AwkText.Empty);
        var terminator = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        storage.StoreText(print.Texts + print.Count, state.FormatStringByteText(separator));
        storage.StoreText(print.Texts + print.Count + 1, state.FormatStringByteText(terminator));
        long payloadByteLength = 0;
        for (var index = 0; index < print.Count; index++)
        {
            if ((index & 63) == 0) state.ThrowIfCancellationRequested();
            if (!fieldViews || !borrowed[index])
            {
                var argument = print.Arguments[index];
                if (argument.Kind == AwkRegionPrintArgumentKind.Number)
                {
                    var buffer = numbers ?? throw new InvalidOperationException("A numeric print requires its funded buffer.");
                    var number = storage.Numbers[argument.Index];
                    state.Counters.RecordPrintNumberArgument();
                    var start = buffer.Length;
                    if (state.TryAppendPrintInteger(number, buffer))
                        fields[index] = AwkFieldText.FromRecordSlice(start, buffer.Length - start);
                    else
                    {
                        var text = state.FormatPrintByteText(AwkValue.FromNumber(number));
                        storage.StoreText(print.Texts + index, text);
                        fields[index] = AwkFieldText.FromOwned(text);
                    }
                }
                else
                {
                    var valueSlot = fieldViews ? argument.FallbackValue : argument.Index;
                    var start = numbers?.Length ?? 0;
                    if (numbers is not null && state.TryAppendPrintInteger(storage.Values[valueSlot], numbers))
                    {
                        fields[index] = AwkFieldText.FromRecordSlice(start, numbers.Length - start);
                        storage.MoveValueToText(valueSlot, print.Texts + index, AwkText.Empty);
                    }
                    else
                    {
                        var text = state.FormatPrintByteText(storage.Values[valueSlot]);
                        storage.MoveValueToText(valueSlot, print.Texts + index, text);
                        fields[index] = AwkFieldText.FromOwned(text);
                    }
                }
            }
            // The sum of int-sized argument lengths fits in long. Delay the
            // bounded-size check until every argument has been formatted.
            payloadByteLength += fields[index].ByteLength();
        }
        state.ThrowIfCancellationRequested();
        var recordByteLength = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(
            payloadByteLength, AwkRuntimeMemory.CheckedMultiply(
                storage.Texts[print.Texts + print.Count].ByteLength, Math.Max(0, print.Count - 1))));
        var payloadStart = 0;
        if (print.Output.IsRedirected)
        {
            var redirected = storage.Builders[0].Buffer;
            if (numbers is null) redirected.Clear();
            else
            {
                // Grow before borrowing this owner's numeric prefix. The complete
                // payload follows it; only that slice reaches the pending host user.
                payloadStart = numbers.Length;
                redirected.EnsureCapacity(AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(
                    payloadStart, AwkRuntimeMemory.CheckedAdd(recordByteLength,
                        storage.Texts[print.Texts + print.Count + 1].ByteLength))));
                record = numbers.AsSpan();
            }
            redirected.AppendRecord(new AwkRecordFields(fields), record, storage.Texts[print.Texts + print.Count],
                storage.Texts[print.Texts + print.Count + 1], recordByteLength, state.CancellationCheckpoint);
        }
        else
        {
            if (numbers is not null) record = numbers.AsSpan();
            output.AppendRecord(new AwkRecordFields(fields), record, storage.Texts[print.Texts + print.Count],
                storage.Texts[print.Texts + print.Count + 1], recordByteLength);
            numbers?.Clear();
        }
        storage.ReleasePrint(print);
        return payloadStart;
    }
}
