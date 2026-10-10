using System;

namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeLimits limits,
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
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
            new AwkSingleValuePrintfArguments(value, counters),
            PrintfMissingArgumentPolicy.OmitDirective,
            limits,
            counters,
            governor,
            compatibilityMode);
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
            new AwkSingleValuePrintfArguments(value, counters),
            PrintfMissingArgumentPolicy.OmitDirective,
            limits,
            counters,
            governor,
            compatibilityMode);
    }

    public static AwkText FormatPrintfStrictByteText<TArguments>(
        string formatText,
        TArguments arguments,
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
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
            compatibilityMode);
    }

    private bool UsesDefaultFormat(AwkFormattingVariable variable)
    {
        var format = resolveVariable(variable);
        var text = format.Text;
        return text.EqualsAscii(AwkFormattingVariableFacts.DefaultFormat(variable));
    }

    private string ResolveFormatString(AwkFormattingVariable variable)
    {
        string ResolveConversionFormatString(AwkText text)
        {
            if (_hasConversionFormatString && _conversionFormatText.Equals(text))
                return _conversionFormatString;

            var defaultFormat = AwkFormattingVariableFacts.DefaultFormat(AwkFormattingVariable.ConversionFormat);
            if (text.EqualsAscii(defaultFormat))
            {
                _conversionFormatText = text;
                _conversionFormatString = defaultFormat;
                _hasConversionFormatString = true;
                return _conversionFormatString;
            }

            counters.RecordFormattingStringInteropInput();
            _conversionFormatText = text;
            _conversionFormatString = text.AsString(counters);
            _hasConversionFormatString = true;
            return _conversionFormatString;
        }

        var format = resolveVariable(variable);
        var text = format.Text;
        if (variable == AwkFormattingVariable.ConversionFormat)
            return ResolveConversionFormatString(text);

        if (_hasOutputFormatString && _outputFormatText.Equals(text))
            return _outputFormatString;

        var defaultFormat = AwkFormattingVariableFacts.DefaultFormat(variable);
        if (text.EqualsAscii(defaultFormat))
        {
            _outputFormatText = text;
            _outputFormatString = defaultFormat;
            _hasOutputFormatString = true;
            return _outputFormatString;
        }

        counters.RecordFormattingStringInteropInput();
        _outputFormatText = text;
        _outputFormatString = text.AsString(counters);
        _hasOutputFormatString = true;
        return _outputFormatString;
    }

}
