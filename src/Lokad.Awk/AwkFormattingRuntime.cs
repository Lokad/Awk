using System;

namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeLimits limits,
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
    Action? checkpoint = null)
{
    private AwkText _outputFormatText = AwkText.Empty;
    private string _outputFormatString = string.Empty;
    private bool _hasOutputFormatString;
    private AwkText _conversionFormatText = AwkText.Empty;
    private string _conversionFormatString = string.Empty;
    private bool _hasConversionFormatString;

    public AwkText FormatPrintByteText(AwkValue value)
    {
        value = value.ForceScalar();
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        if (AwkNumberFormatter.IsInteger(number))
            return AwkNumberFormatter.ToAwkText(number, counters, governor);

        if (UsesDefaultFormat(AwkFormattingVariable.OutputFormat))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor);

        counters.RecordFormattingFallback();
        return PrintfEvaluation.FormatByteText(
            ResolveFormatString(AwkFormattingVariable.OutputFormat),
            new AwkSingleValuePrintfArguments(value, counters, checkpoint),
            PrintfMissingArgumentPolicy.OmitDirective,
            limits,
            counters,
            governor,
            compatibilityMode,
            checkpoint);
    }

    public AwkText FormatStringByteText(AwkValue value)
    {
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        if (AwkNumberFormatter.IsInteger(number))
            return AwkNumberFormatter.ToAwkText(number, counters, governor);

        if (UsesDefaultFormat(AwkFormattingVariable.ConversionFormat))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor);

        counters.RecordFormattingFallback();
        return PrintfEvaluation.FormatByteText(
            ResolveFormatString(AwkFormattingVariable.ConversionFormat),
            new AwkSingleValuePrintfArguments(value, counters, checkpoint),
            PrintfMissingArgumentPolicy.OmitDirective,
            limits,
            counters,
            governor,
            compatibilityMode,
            checkpoint);
    }

    public static AwkText FormatPrintfStrictByteText<TArguments>(
        string formatText,
        TArguments arguments,
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
        where TArguments : IPrintfArguments
    {
        counters.RecordFormattingFallback();
        counters.RecordFormattingStringInteropInput();
        return PrintfEvaluation.FormatByteText(
            formatText,
            arguments,
            PrintfMissingArgumentPolicy.Throw,
            limits,
            counters,
            governor,
            compatibilityMode,
            checkpoint);
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
