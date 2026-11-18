namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkText> resolveVariable,
    AwkRuntimeState state)
{
    internal AwkText CaptureNumericFormat(AwkValue value, AwkMemoryLease lease) =>
        double.IsFinite(value.StoredNumber)
            ? FormatStringByteText(value, lease)
            : FormatDefaultNumberText(value.StoredNumber, lease);

    public AwkText FormatPrintByteText(AwkValue value) => FormatPrintByteText(value, null);

    internal bool TryAppendPrintInteger(AwkValue value, AwkUtf8Builder destination) =>
        value.IsNumberValue && TryAppendPrintInteger(value.StoredNumber, destination);

    internal bool TryAppendPrintInteger(double number, AwkUtf8Builder destination)
    {
        // Integers already ignore OFMT. Reuse the same bounded emission without
        // constructing an owning text value for this immediate print consumer.
        if (!double.IsInteger(number)) return false;
        AwkNumberFormatter.Append(number, destination, state.CancellationCheckpoint);

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
            return FormatNumberText(number, retainedTextLease);

        if (UsesDefaultFormat(variable))
            return FormatDefaultNumberText(number, retainedTextLease);

        using var argumentText = state.MemoryAccounting.CreateTransientLease();
        return PrintfEvaluation.FormatByteText(
            state.DecodeText(resolveVariable(variable)), new NumericFormatArguments(number, variable, this, argumentText),
            PrintfMissingArgumentPolicy.OmitDirective, state, retainedTextLease);
    }

    private AwkText FormatNumberArgumentText(double number, AwkFormattingVariable variable, AwkMemoryLease lease) =>
        variable == AwkFormattingVariable.OutputFormat
            ? FormatStringByteText(AwkValue.FromNumber(number), lease)
            // A string conversion within CONVFMT cannot apply CONVFMT again.
            // Keep the existing round-trip spelling at this nonrecursive boundary.
            : FormatNumberText(number, lease);

    private AwkText FormatNumberText(double number, AwkMemoryLease? retainedTextLease)
    {
        Span<byte> buffer = stackalloc byte[AwkNumberFormatter.FormattedByteCapacity(number)];
        return CopyNumericText(AwkNumberFormatter.Format(number, buffer), retainedTextLease);
    }

    private AwkText FormatDefaultNumberText(double number, AwkMemoryLease? retainedTextLease)
    {
        Span<byte> buffer = stackalloc byte[AwkNumberFormatter.MaxFormattedDoubleBytes];
        return CopyNumericText(AwkNumberFormatter.FormatDefault(number, buffer), retainedTextLease);
    }

    private AwkText CopyNumericText(ReadOnlySpan<byte> bytes, AwkMemoryLease? retainedTextLease)
    {
        retainedTextLease?.ReserveAdditional(AwkRuntimeMemory.EstimateTextBytes(bytes.Length));
        // A supplied lease already admits the complete owned text, including its bytes.
        var owned = AwkText.AllocateOwnedBytes(bytes.Length,
            retainedTextLease is null ? state.MemoryAccounting : AwkMemoryAccounting.Disabled);
        bytes.CopyTo(owned);
        return AwkText.FromTrustedOwnedBytes(owned, state.Counters);
    }

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
            var converted = state.DecodeText(text);
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

    public static void FormatPrintfToBuilder<TArguments>(
        AwkUtf8Builder output,
        string format,
        TArguments arguments,
        AwkRuntimeState state)
        where TArguments : IPrintfArguments
    {

        PrintfEvaluation.FormatToBuilder(output, format, arguments, PrintfMissingArgumentPolicy.Throw, state, state.StringUnitMode);
    }

    private bool UsesDefaultFormat(AwkFormattingVariable variable)
    {
        var text = resolveVariable(variable);
        return text.EqualsAscii(AwkFormattingVariableFacts.DefaultFormat(variable), state.CancellationCheckpoint);
    }

}
