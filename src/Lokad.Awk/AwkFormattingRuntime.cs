namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
    Action? checkpoint = null)
{
    internal AwkText CaptureNumericFormat(AwkValue value, AwkMemoryLease lease) =>
        double.IsFinite(value.StoredNumber)
            ? FormatStringByteText(value, lease)
            : AwkNumberFormatter.ToDefaultFormatAwkText(value.StoredNumber, counters, governor, lease);

    public AwkText FormatPrintByteText(AwkValue value) => FormatPrintByteText(value, null);

    internal bool TryAppendPrintInteger(AwkValue value, AwkUtf8Builder destination) =>
        value.IsNumberValue && TryAppendPrintInteger(value.StoredNumber, destination);

    internal bool TryAppendPrintInteger(double number, AwkUtf8Builder destination)
    {
        // Integers already ignore OFMT. Reuse the same bounded emission without
        // constructing an owning text value for this immediate print consumer.
        if (!double.IsInteger(number)) return false;
        AwkNumberFormatter.Append(number, destination, checkpoint);
        counters.RecordPrintIntegerSegment();
        return true;
    }

    public AwkText FormatPrintByteText(AwkValue value, AwkMemoryLease? retainedTextLease)
    {
        value = value.ForceScalar();
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        return FormatNumberByteText(number, AwkFormattingVariable.OutputFormat, retainedTextLease);
    }

    public AwkText FormatStringByteText(AwkValue value) => FormatStringByteText(value, null);

    public AwkText FormatStringByteText(AwkValue value, AwkMemoryLease? retainedTextLease)
    {
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        return FormatNumberByteText(number, AwkFormattingVariable.ConversionFormat, retainedTextLease);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private AwkText FormatNumberByteText(double number, AwkFormattingVariable variable,
        AwkMemoryLease? retainedTextLease)
    {
        if (double.IsInteger(number))
            return AwkNumberFormatter.ToAwkText(number, counters, governor, retainedTextLease);

        if (UsesDefaultFormat(variable))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor, retainedTextLease);

        counters.RecordFormattingFallback();
        using var argumentText = governor.CreateTransientLease();
        return PrintfEvaluation.FormatByteText(
            resolveVariable(variable).AsString(counters, checkpoint), new NumericFormatArguments(number, variable, this, argumentText),
            PrintfMissingArgumentPolicy.OmitDirective, counters, governor, compatibilityMode, checkpoint, retainedTextLease);
    }

    private AwkText FormatNumberArgumentText(double number, AwkFormattingVariable variable, AwkMemoryLease lease) =>
        variable == AwkFormattingVariable.OutputFormat
            ? FormatStringByteText(AwkValue.FromNumber(number), lease)
            // A string conversion within CONVFMT cannot apply CONVFMT again.
            // Keep the existing round-trip spelling at this nonrecursive boundary.
            : AwkNumberFormatter.ToAwkText(number, counters, governor, lease);

    private readonly struct NumericFormatArguments(double number, AwkFormattingVariable variable,
        AwkFormattingRuntime runtime, AwkMemoryLease textLease) : IPrintfArguments
    {
        public int Count => 1;

        public AwkText GetByteText(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
            return runtime.FormatNumberArgumentText(number, variable, textLease);
        }

        public double GetNumber(int index) =>
            index == 0 ? number : throw new ArgumentOutOfRangeException(nameof(index));

        public bool IsNumeric(int index) =>
            index == 0 ? true : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal static string PrepareStringValue(AwkValue value, AwkRuntimeState state, AwkMemoryLease lease, out long retainedBytes)
    {
        retainedBytes = 0;
        var before = lease.Bytes;
        try
        {
            // Numeric conversion may create native text. Reuse this owner's
            // lease until decoding finishes; no separate per-call lease or cache.
            var text = state.FormatStringByteText(value, lease);
            var nativeBytes = lease.Bytes - before;
            // Valid UTF-8 has at most one UTF-16 code unit per byte. Admit before
            // decoding, then retain only the actual string until formatting ends.
            var bound = state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateUtf16StringBytes(text.ByteLength) : 0;
            if (bound > 0) lease.ReserveAdditional(bound);
            state.ThrowIfCancellationRequested();
            var converted = text.AsString(state.Counters, state.CancellationCheckpoint);
            retainedBytes = state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.EstimateUtf16StringBytes(converted.Length) : 0;
            if (bound > retainedBytes) lease.Release(bound - retainedBytes);
            if (nativeBytes > 0) lease.Release(nativeBytes);
            return converted;
        }
        catch
        {
            var added = lease.Bytes - before;
            if (added > 0) lease.Release(added);
            retainedBytes = 0;
            throw;
        }
    }

    public static AwkText FormatPrintfStrictByteText<TArguments>(
        string formatText,
        TArguments arguments,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null,
        AwkMemoryLease? retainedTextLease = null,
        AwkStringUnitMode stringUnitMode = AwkStringUnitMode.Utf8Rune)
        where TArguments : IPrintfArguments
    {
        counters.RecordFormattingFallback();
        counters.RecordFormattingStringInteropInput();
        return PrintfEvaluation.FormatByteText(
            formatText, arguments, PrintfMissingArgumentPolicy.Throw, counters, governor, compatibilityMode, checkpoint,
            retainedTextLease, stringUnitMode);
    }

    public static void FormatPrintfToBuilder<TArguments>(
        AwkUtf8Builder output,
        string format,
        TArguments arguments,
        AwkRuntimeState state)
        where TArguments : IPrintfArguments
    {
        state.Counters.RecordFormattingFallback();
        state.Counters.RecordFormattingStringInteropInput();
        PrintfEvaluation.FormatToBuilder(output, format, arguments, PrintfMissingArgumentPolicy.Throw, state.Counters,
            state.MemoryAccounting, state.CompatibilityMode, state.CancellationCheckpoint, state.StringUnitMode);
    }

    private bool UsesDefaultFormat(AwkFormattingVariable variable)
    {
        var format = resolveVariable(variable);
        var text = format.Text;
        return text.EqualsAscii(AwkFormattingVariableFacts.DefaultFormat(variable), checkpoint);
    }

}
