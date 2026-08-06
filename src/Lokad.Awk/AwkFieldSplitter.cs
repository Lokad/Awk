using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk;

internal sealed class AwkFieldSplitter(
    AwkFieldSeparatorSpec fieldSeparator,
    Func<AwkRegexPattern, Regex> createRegex,
    Func<AwkRegexLiteralText, Regex> createLiteralRegex,
    Func<Regex, AwkText, AwkLeasedReadOnlyList<AwkRegexMatch>> enumerateTextMatches,
    Func<Regex, AwkText, AwkLeasedReadOnlyList<AwkRegexMatchSpan>> enumerateMatchSpans,
    Func<bool> isIgnoreCaseEnabled,
    Func<bool> charactersAsBytes,
    Action throwIfCancellationRequested,
    AwkMemoryGovernor memoryGovernor)
{
    private readonly AwkByteFieldSplitter _byteSplitter = new(
        createRegex,
        createLiteralRegex,
        enumerateTextMatches,
        enumerateMatchSpans,
        isIgnoreCaseEnabled,
        charactersAsBytes,
        throwIfCancellationRequested,
        memoryGovernor);

    private string _fieldSeparator = fieldSeparator.Text;
    private string _fieldPattern = string.Empty;
    private IReadOnlyList<AwkFieldWidth> _fieldWidths = [];
    private AwkFieldSplittingMode _fieldSplittingMode;
    private bool _useDefaultSeparator = fieldSeparator.UseDefaultSeparator;

    public void AssignFieldSeparator(string value)
    {
        _fieldSeparator = value;
        _useDefaultSeparator = value == " ";
        _fieldSplittingMode = AwkFieldSplittingMode.FieldSeparator;
    }

    public void AssignFieldPattern(string value)
    {
        _fieldPattern = value;
        if (value.Length > 0)
            _fieldSplittingMode = AwkFieldSplittingMode.FieldPattern;
    }

    public void AssignFieldWidths(string value)
    {
        _fieldWidths = ParseFieldWidths(value);
        if (_fieldWidths.Count > 0)
            _fieldSplittingMode = AwkFieldSplittingMode.FieldWidths;
    }

    public IReadOnlyList<AwkText> SplitFieldTexts(AwkText line, AwkRuntimeCounters counters) =>
        _fieldSplittingMode switch
        {
            AwkFieldSplittingMode.FieldPattern => SplitFieldPatternTexts(line, counters),
            AwkFieldSplittingMode.FieldWidths => SplitFieldWidthTexts(line, counters),
            _ => SplitFieldTextsBySeparator(
                    line,
                    _fieldSeparator,
                    _useDefaultSeparator,
                    captureSeparators: false,
                    counters)
                .Fields
        };

    public IReadOnlyList<AwkFieldText> SplitRecordFields(AwkText line, AwkRuntimeCounters counters) =>
        _fieldSplittingMode switch
        {
            AwkFieldSplittingMode.FieldPattern => ToOwnedFieldTexts(SplitFieldPatternTexts(line, counters)),
            AwkFieldSplittingMode.FieldWidths => ToOwnedFieldTexts(SplitFieldWidthTexts(line, counters)),
            _ => _byteSplitter.SplitRecordFieldsBySeparator(
                line,
                _fieldSeparator,
                _useDefaultSeparator,
                counters)
        };

    public AwkTextSplitResult SplitFieldTextsBySeparator(
        AwkText line,
        string separator,
        bool useDefaultSeparator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        return _byteSplitter.SplitBySeparator(
            line,
            separator,
            useDefaultSeparator,
            captureSeparators,
            counters);
    }

    public AwkTextSplitResult Patsplit(AwkText input, string pattern, bool captureSeparators, AwkRuntimeCounters counters) =>
        _byteSplitter.Patsplit(input, pattern, captureSeparators, counters);

    private IReadOnlyList<AwkText> SplitFieldPatternTexts(
        AwkText line,
        AwkRuntimeCounters counters)
    {
        return Patsplit(line, _fieldPattern, captureSeparators: false, counters).Fields;
    }

    private IReadOnlyList<AwkText> SplitFieldWidthTexts(
        AwkText line,
        AwkRuntimeCounters counters)
    {
        if (_fieldWidths.Count == 0)
            return [];

        return charactersAsBytes()
            ? SplitFieldWidthByteTexts(line, counters)
            : SplitFieldWidthRuneTexts(line, counters);
    }

    private IReadOnlyList<AwkText> SplitFieldWidthRuneTexts(
        AwkText line,
        AwkRuntimeCounters counters)
    {
        var fields = new List<AwkText>(_fieldWidths.Count);
        var position = 0;
        var bytePosition = 0;
        var runePosition = 0;
        var content = line.AsSpan();
        for (var index = 0; index < _fieldWidths.Count; index++)
        {
            var width = _fieldWidths[index];
            var fieldStartRune = AddFieldWidthUnits(position, width.Skip.Value);
            AdvanceRuneOffset(content, ref runePosition, ref bytePosition, fieldStartRune);
            if (runePosition < fieldStartRune)
            {
                fields.Add(AwkText.Empty);
                position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
                continue;
            }

            var fieldStartByte = bytePosition;
            position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
            AdvanceRuneOffset(content, ref runePosition, ref bytePosition, position);
            fields.Add(line.CopyByteSlice(fieldStartByte, bytePosition - fieldStartByte, counters, memoryGovernor));
        }

        return fields;
    }

    private IReadOnlyList<AwkText> SplitFieldWidthByteTexts(
        AwkText line,
        AwkRuntimeCounters counters)
    {
        var fields = new List<AwkText>(_fieldWidths.Count);
        var position = 0;
        var sourceLength = line.ByteLength;
        for (var index = 0; index < _fieldWidths.Count; index++)
        {
            var width = _fieldWidths[index];
            position = AddFieldWidthUnits(position, width.Skip.Value);
            ThrowIfNotUtf8Boundary(line, position);
            if (position >= sourceLength)
            {
                fields.Add(AwkText.Empty);
                continue;
            }

            var length = Math.Min(width.Width.Value, sourceLength - position);
            ThrowIfNotUtf8Boundary(line, position + length);
            fields.Add(line.CopyByteSlice(position, length, counters, memoryGovernor));
            position = AddFieldWidthUnits(position, width.Width.Value);
        }

        return fields;
    }

    private static int AddFieldWidthUnits(int position, int value)
    {
        if (position > int.MaxValue - value)
            return int.MaxValue;

        return position + value;
    }

    private static void AdvanceRuneOffset(
        ReadOnlySpan<byte> content,
        ref int runePosition,
        ref int bytePosition,
        int targetRunePosition)
    {
        while (runePosition < targetRunePosition && bytePosition < content.Length)
        {
            var status = Rune.DecodeFromUtf8(content[bytePosition..], out _, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            bytePosition += consumed;
            runePosition++;
        }
    }

    private static void ThrowIfNotUtf8Boundary(AwkText line, int position)
    {
        if (position <= 0 || position >= line.ByteLength)
            return;

        if ((line.AsSpan()[position] & 0xC0) != 0x80)
            return;

        throw new AwkRuntimeException(
            AwkMessages.ErrorUnsupportedByteModeFieldWidths,
            AwkDiagnosticId.UnsupportedPolicy);
    }

    private static IReadOnlyList<AwkFieldWidth> ParseFieldWidths(string value)
    {
        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return [];

        var widths = new List<AwkFieldWidth>(parts.Length);
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var separatorIndex = part.IndexOf(':', StringComparison.Ordinal);
            var skipText = separatorIndex >= 0 ? part[..separatorIndex] : "0";
            var widthText = separatorIndex >= 0 ? part[(separatorIndex + 1)..] : part;
            if (!AwkFieldSkip.TryParse(skipText, out var skip) ||
                !AwkFieldWidthLength.TryParse(widthText, out var width))
            {
                continue;
            }

            widths.Add(new AwkFieldWidth(skip, width));
        }

        return widths;
    }

    private static IReadOnlyList<AwkFieldText> ToOwnedFieldTexts(IReadOnlyList<AwkText> fields)
    {
        if (fields.Count == 0)
            return [];

        var result = new AwkFieldText[fields.Count];
        for (var index = 0; index < fields.Count; index++)
            result[index] = AwkFieldText.FromOwned(fields[index]);

        return result;
    }

}
