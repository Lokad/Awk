using System;

namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
    Action? checkpoint = null)
{
    public AwkText FormatPrintByteText(AwkValue value) => FormatPrintByteText(value, null);

    public AwkText FormatPrintByteText(AwkValue value, AwkMemoryLease? retainedTextLease)
    {
        value = value.ForceScalar();
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        if (AwkNumberFormatter.IsInteger(number))
            return AwkNumberFormatter.ToAwkText(number, counters, governor, retainedTextLease);

        if (UsesDefaultFormat(AwkFormattingVariable.OutputFormat))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor, retainedTextLease);

        counters.RecordFormattingFallback();
        return PrintfEvaluation.FormatByteText(
            resolveVariable(AwkFormattingVariable.OutputFormat).AsString(counters, checkpoint), new AwkSingleValuePrintfArguments(value, counters, checkpoint), PrintfMissingArgumentPolicy.OmitDirective, counters, governor, compatibilityMode, checkpoint, retainedTextLease);
    }

    public AwkText FormatStringByteText(AwkValue value) => FormatStringByteText(value, null);

    public AwkText FormatStringByteText(AwkValue value, AwkMemoryLease? retainedTextLease)
    {
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        if (AwkNumberFormatter.IsInteger(number))
            return AwkNumberFormatter.ToAwkText(number, counters, governor, retainedTextLease);

        if (UsesDefaultFormat(AwkFormattingVariable.ConversionFormat))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor, retainedTextLease);

        counters.RecordFormattingFallback();
        return PrintfEvaluation.FormatByteText(
            resolveVariable(AwkFormattingVariable.ConversionFormat).AsString(counters, checkpoint), new AwkSingleValuePrintfArguments(value, counters, checkpoint), PrintfMissingArgumentPolicy.OmitDirective, counters, governor, compatibilityMode, checkpoint, retainedTextLease);
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
