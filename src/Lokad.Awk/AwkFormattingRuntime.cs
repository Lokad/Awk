using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFormattingRuntime(
    Func<AwkFormattingVariable, AwkFormatString> resolveVariable,
    AwkRuntimeCounters counters,
    AwkMemoryGovernor governor)
{
    private AwkText _outputFormatText = AwkText.Empty;
    private string _outputFormatString = string.Empty;
    private bool _hasOutputFormatString;
    private AwkText _conversionFormatText = AwkText.Empty;
    private string _conversionFormatString = string.Empty;
    private bool _hasConversionFormatString;

    public AwkText FormatPrintByteText(AwkValue value)
    {
        if (!value.IsNumberValue || !value.TryGetNumber(out var number))
            return value.ByteText;

        if (UsesDefaultFormat(AwkFormattingVariable.OutputFormat))
            return AwkNumberFormatter.ToDefaultFormatAwkText(number, counters, governor);

        counters.RecordFormattingFallback();
        return AwkText.FromString(
            PrintfEvaluation.Format(
                ResolveFormatString(AwkFormattingVariable.OutputFormat),
                [FormatStringInput(value)]),
            counters,
            governor);
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
        return AwkText.FromString(
            PrintfEvaluation.Format(
                ResolveFormatString(AwkFormattingVariable.ConversionFormat),
                [FormatStringInput(value)]),
            counters,
            governor);
    }

    public static string FormatPrintfStrict(string formatText, IReadOnlyList<string> arguments)
    {
        return FormatPrintfStrict(formatText, arguments, AwkRuntimeCounters.Disabled);
    }

    public static string FormatPrintfStrict(
        string formatText,
        IReadOnlyList<string> arguments,
        AwkRuntimeCounters counters) =>
        FormatPrintfStrict(
            formatText,
            new PrintfStringListArguments(arguments, counters),
            counters);

    public static AwkText FormatPrintfStrictByteText(
        string formatText,
        IReadOnlyList<string> arguments,
        AwkRuntimeCounters counters)
    {
        return FormatPrintfStrictByteText(formatText, arguments, counters, AwkMemoryGovernor.Disabled);
    }

    public static AwkText FormatPrintfStrictByteText(
        string formatText,
        IReadOnlyList<string> arguments,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        FormatPrintfStrictByteText(
            formatText,
            new PrintfStringListArguments(arguments, counters),
            counters,
            governor);

    public static AwkText FormatPrintfStrictByteText<TArguments>(
        string formatText,
        TArguments arguments,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor)
        where TArguments : IPrintfArguments
    {
        return AwkText.FromString(
            FormatPrintfStrict(formatText, arguments, counters),
            counters,
            governor);
    }

    private static string FormatPrintfStrict<TArguments>(
        string formatText,
        TArguments arguments,
        AwkRuntimeCounters counters)
        where TArguments : IPrintfArguments
    {
        try
        {
            counters.RecordFormattingFallback();
            counters.RecordFormattingStringInteropInput();
            return PrintfEvaluation.Format(formatText, arguments, strictMissingArguments: true);
        }
        catch (FormatException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
        }
    }

    private string FormatStringInput(AwkValue value)
    {
        counters.RecordFormattingStringInteropInput();
        return value.AsString(counters);
    }

    private bool UsesDefaultFormat(AwkFormattingVariable variable)
    {
        var format = resolveVariable(variable);
        var text = format.Text ?? AwkText.Empty;
        return text.EqualsAscii(AwkFormattingVariableFacts.DefaultFormat(variable));
    }

    private string ResolveFormatString(AwkFormattingVariable variable)
    {
        var format = resolveVariable(variable);
        var text = format.Text ?? AwkText.Empty;
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

    private string ResolveConversionFormatString(AwkText text)
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

}
