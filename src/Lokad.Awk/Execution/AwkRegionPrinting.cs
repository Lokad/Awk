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

    internal static int Append(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output,
        AwkRegionPrint print, in AwkValue current, ReadOnlySpan<byte> finalText = default)
    {
        if (print.Count == 1 && !print.Output.IsRedirected && print.Arguments[0].Kind != AwkRegionPrintArgumentKind.Field)
        {
            AppendSingle(state, storage, output, print, in current, finalText);
            return 0;
        }
        return AppendList(state, storage, output, print, in current, finalText);
    }

    private static int AppendList(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output,
        AwkRegionPrint print, in AwkValue current, ReadOnlySpan<byte> finalText)
    {
        // The instruction captures its final source before entering formatting.
        // Its actual owner remains live through this synchronous copy.
        state.ThrowIfCancellationRequested();
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
        var separatorValue = print.Count > 1 ? state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator)
            : AwkValue.FromText(AwkText.Empty);
        var terminatorValue = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        var separator = storage.FormatBorrowedText(print.Texts + print.Count, separatorValue, print: false);
        var terminator = storage.FormatBorrowedText(print.TerminatorText, terminatorValue, print: false);
        long payloadByteLength = print.HasPreparedText ? finalText.Length : 0;
        var capturedCount = print.HasPreparedText ? print.Count - 1 : print.Count;
        for (var index = 0; index < capturedCount; index++)
        {
            if ((index & 63) == 0) state.ThrowIfCancellationRequested();
            if (!fieldViews || !borrowed[index])
            {
                var argument = print.Arguments[index];
                if (argument.Kind == AwkRegionPrintArgumentKind.Number)
                {
                    var number = storage.Numbers[argument.Index];
                    state.Counters.RecordPrintNumberArgument();
                    var start = numbers?.Length ?? 0;
                    if (numbers is not null && state.TryAppendPrintInteger(number, numbers))
                        fields[index] = AwkFieldText.FromRecordSlice(start, numbers.Length - start);
                    else
                    {
                        var text = storage.FormatBorrowedText(print.Texts + index, AwkValue.FromNumber(number), print: true);
                        fields[index] = AwkFieldText.FromOwned(text);
                    }
                }
                else if (argument.Kind == AwkRegionPrintArgumentKind.Text)
                {
                    storage.MoveText(argument.Index, print.Texts + index);
                    fields[index] = AwkFieldText.FromOwned(storage.Texts[print.Texts + index]);
                }
                else if (argument.Kind == AwkRegionPrintArgumentKind.Current)
                {
                    var start = numbers?.Length ?? 0;
                    if (numbers is not null && state.TryAppendPrintInteger(current, numbers))
                        fields[index] = AwkFieldText.FromRecordSlice(start, numbers.Length - start);
                    else fields[index] = AwkFieldText.FromOwned(storage.FormatBorrowedText(print.Texts + index, current, print: true));
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
                        var text = storage.FormatCapturedPrintText(valueSlot, print.Texts + index);
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
                separator.ByteLength, Math.Max(0, print.Count - 1))));
        var payloadStart = 0;
        var parts = print.HasPreparedText ? new AwkRecordFields(fields, finalText) : new AwkRecordFields(fields);
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
                        terminator.ByteLength))));
                record = numbers.AsSpan();
            }
            redirected.AppendRecord(parts, record, separator,
                terminator, recordByteLength, state.CancellationCheckpoint);
        }
        else
        {
            if (numbers is not null) record = numbers.AsSpan();
            output.AppendRecord(parts, record, separator,
                terminator, recordByteLength);
            numbers?.Clear();
        }
        // Local or array-backed segment references retire before their text claim.
        fields.Clear();
        borrowed.Clear();
        storage.ReleaseTexts(print.Texts, print.TextCount);
        return payloadStart;
    }

    private static void AppendSingle(AwkRuntimeState state, AwkRegionStorage storage, AwkOutputSink output,
        AwkRegionPrint print, in AwkValue current, ReadOnlySpan<byte> finalText)
    {
        state.ThrowIfCancellationRequested();
        state.Counters.RecordSmallValuePrint();
        var terminator = storage.FormatBorrowedText(print.TerminatorText,
            state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator), print: false);
        var argument = print.Arguments[0];
        var value = argument.Kind switch
        {
            AwkRegionPrintArgumentKind.Current => current,
            AwkRegionPrintArgumentKind.Number => AwkValue.FromNumber(storage.Numbers[argument.Index]),
            AwkRegionPrintArgumentKind.Value => storage.Values[argument.Index],
            _ => default,
        };
        if (value.IsNumberValue && double.IsInteger(value.StoredNumber))
        {
            if (argument.Kind == AwkRegionPrintArgumentKind.Value)
                storage.MoveValueToText(argument.Index, print.Texts, AwkText.Empty);
            if (argument.Kind == AwkRegionPrintArgumentKind.Number) state.Counters.RecordPrintNumberArgument();
            AppendInteger(state, output, value.StoredNumber, terminator);
        }
        else
        {
            if (argument.Kind == AwkRegionPrintArgumentKind.PreparedText)
            {
                state.ThrowIfCancellationRequested();
                output.Append(finalText, terminator);
            }
            else
            {
                AwkText text;
                if (argument.Kind == AwkRegionPrintArgumentKind.Text)
                {
                    storage.MoveText(argument.Index, print.Texts);
                    text = storage.Texts[print.Texts];
                }
                else if (argument.Kind == AwkRegionPrintArgumentKind.Value)
                    text = storage.FormatCapturedPrintText(argument.Index, print.Texts);
                else text = storage.FormatBorrowedText(print.Texts, value, print: true);
                state.ThrowIfCancellationRequested();
                output.Append([text, terminator]);
            }
        }
        storage.ReleaseTexts(print.Texts, print.TextCount);
    }

    private static void AppendInteger(AwkRuntimeState state, AwkOutputSink output, double number, AwkText terminator)
    {
        // Keep the formatter's concrete bound out of the list renderer's frame.
        Span<byte> bytes = stackalloc byte[AwkNumberFormatter.FormattedByteCapacity(number)];
        var content = AwkNumberFormatter.Format(number, bytes);
        state.Counters.RecordPrintIntegerSegment();
        state.ThrowIfCancellationRequested();
        output.Append(content, terminator);
    }
}
