using System;

namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeCounters counters,
    AwkMemoryAccounting governor,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
    Action? checkpoint = null)
{
    private AwkText _outputFormatText = AwkText.Empty;
    private string _outputFormatString = string.Empty;
    private bool _hasOutputFormatString;
    private AwkText _conversionFormatText = AwkText.Empty;
    private string _conversionFormatString = string.Empty;
    private bool _hasConversionFormatString;

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
            ResolveFormatString(AwkFormattingVariable.OutputFormat), new AwkSingleValuePrintfArguments(value, counters, checkpoint), PrintfMissingArgumentPolicy.OmitDirective, counters, governor, compatibilityMode, checkpoint, retainedTextLease);
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
            ResolveFormatString(AwkFormattingVariable.ConversionFormat), new AwkSingleValuePrintfArguments(value, counters, checkpoint), PrintfMissingArgumentPolicy.OmitDirective, counters, governor, compatibilityMode, checkpoint, retainedTextLease);
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

    private string ResolveFormatString(AwkFormattingVariable variable)
    {
        string ResolveConversionFormatString(AwkText text)
        {
            if (_hasConversionFormatString && _conversionFormatText.Equals(text, checkpoint))
                return _conversionFormatString;

            var defaultFormat = AwkFormattingVariableFacts.DefaultFormat(AwkFormattingVariable.ConversionFormat);
            if (text.EqualsAscii(defaultFormat, checkpoint))
            {
                _conversionFormatText = text;
                _conversionFormatString = defaultFormat;
                _hasConversionFormatString = true;
                return _conversionFormatString;
            }

            counters.RecordFormattingStringInteropInput();
            var decoded = text.AsString(counters, checkpoint);
            checkpoint?.Invoke();
            _conversionFormatText = text;
            _conversionFormatString = decoded;
            _hasConversionFormatString = true;
            return _conversionFormatString;
        }

        var format = resolveVariable(variable);
        var text = format.Text;
        if (variable == AwkFormattingVariable.ConversionFormat)
            return ResolveConversionFormatString(text);

        if (_hasOutputFormatString && _outputFormatText.Equals(text, checkpoint))
            return _outputFormatString;

        var defaultFormat = AwkFormattingVariableFacts.DefaultFormat(variable);
        if (text.EqualsAscii(defaultFormat, checkpoint))
        {
            _outputFormatText = text;
            _outputFormatString = defaultFormat;
            _hasOutputFormatString = true;
            return _outputFormatString;
        }

        counters.RecordFormattingStringInteropInput();
        var decoded = text.AsString(counters, checkpoint);
        checkpoint?.Invoke();
        _outputFormatText = text;
        _outputFormatString = decoded;
        _hasOutputFormatString = true;
        return _outputFormatString;
    }

}
