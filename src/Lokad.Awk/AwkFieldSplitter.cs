using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkFieldSplitter(
    AwkFieldSeparatorSpec fieldSeparator,
    Func<AwkRegexPattern, AwkRegex> createRegex,
    Func<AwkRegexLiteralText, AwkRegex> createLiteralRegex,
    Func<AwkRegex, AwkText, AwkRegexMatchSpanEnumerator> enumerateMatchSpans,
    Func<bool> isIgnoreCaseEnabled,
    Func<bool> charactersAsBytes,
    Action throwIfCancellationRequested,
    AwkMemoryGovernor memoryGovernor)
{
    private readonly AwkByteFieldSplitter _byteSplitter = new(
        createRegex,
        createLiteralRegex,
        enumerateMatchSpans,
        isIgnoreCaseEnabled,
        charactersAsBytes,
        throwIfCancellationRequested,
        memoryGovernor);

    private AwkFieldSeparatorSpec _fieldSeparator = fieldSeparator;
    private string _fieldPattern = string.Empty;
    private IReadOnlyList<AwkFieldWidth> _fieldWidths = [];
    private AwkFieldSplittingMode _fieldSplittingMode;
    private AwkRecordFieldSplittingMode _recordFieldSplittingMode;

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _recordFieldSplittingMode = mode;

    public void AssignFieldSeparator(string value)
    {
        _fieldSeparator = AwkFieldSeparatorSpec.FromText(value);
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
        static IReadOnlyList<AwkFieldWidth> ParseFieldWidths(string text)
        {
            var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
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

        _fieldWidths = ParseFieldWidths(value);
        if (_fieldWidths.Count > 0)
            _fieldSplittingMode = AwkFieldSplittingMode.FieldWidths;
    }

    public IReadOnlyList<AwkFieldText> SplitRecordFields(AwkText line, AwkRuntimeCounters counters)
    {
        static int AddFieldWidthUnits(int position, int value)
        {
            if (position > int.MaxValue - value)
                return int.MaxValue;

            return position + value;
        }

        IReadOnlyList<AwkFieldText> SplitFieldWidthRecordFields()
        {
            IReadOnlyList<AwkFieldText> SplitRuneFields()
            {
                static void AdvanceRuneOffset(
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

                var fields = new List<AwkFieldText>(_fieldWidths.Count);
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
                        fields.Add(AwkFieldText.Empty);
                        position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
                        continue;
                    }

                    var fieldStartByte = bytePosition;
                    position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
                    AdvanceRuneOffset(content, ref runePosition, ref bytePosition, position);
                    fields.Add(AwkFieldText.FromRecordSlice(fieldStartByte, bytePosition - fieldStartByte));
                }

                return fields;
            }

            IReadOnlyList<AwkFieldText> SplitByteFields()
            {
                static void ThrowIfNotUtf8Boundary(AwkText text, int position)
                {
                    if (position <= 0 || position >= text.ByteLength)
                        return;

                    if ((text.AsSpan()[position] & 0xC0) != 0x80)
                        return;

                    throw new AwkRuntimeException(
                        AwkMessages.ErrorUnsupportedByteModeFieldWidths,
                        AwkDiagnosticId.UnsupportedPolicy);
                }

                var fields = new List<AwkFieldText>(_fieldWidths.Count);
                var position = 0;
                var sourceLength = line.ByteLength;
                for (var index = 0; index < _fieldWidths.Count; index++)
                {
                    var width = _fieldWidths[index];
                    position = AddFieldWidthUnits(position, width.Skip.Value);
                    ThrowIfNotUtf8Boundary(line, position);
                    if (position >= sourceLength)
                    {
                        fields.Add(AwkFieldText.Empty);
                        continue;
                    }

                    var length = Math.Min(width.Width.Value, sourceLength - position);
                    ThrowIfNotUtf8Boundary(line, position + length);
                    fields.Add(AwkFieldText.FromRecordSlice(position, length));
                    position = AddFieldWidthUnits(position, width.Width.Value);
                }

                return fields;
            }

            if (_fieldWidths.Count == 0)
                return [];

            return charactersAsBytes()
                ? SplitByteFields()
                : SplitRuneFields();
        }

        return _fieldSplittingMode switch
        {
            AwkFieldSplittingMode.FieldPattern => _byteSplitter.PatsplitRecordFields(line, _fieldPattern),
            AwkFieldSplittingMode.FieldWidths => SplitFieldWidthRecordFields(),
            _ => _recordFieldSplittingMode == AwkRecordFieldSplittingMode.Paragraph
                ? _byteSplitter.SplitParagraphRecordFieldsBySeparator(
                    line,
                    _fieldSeparator,
                    counters)
                : _byteSplitter.SplitRecordFieldsBySeparator(
                    line,
                    _fieldSeparator,
                    counters)
        };
    }

    public AwkTextSplitResult SplitFieldTextsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        bool captureSeparators,
        AwkRuntimeCounters counters)
    {
        return _byteSplitter.SplitBySeparator(
            line,
            separator,
            captureSeparators,
            counters);
    }

    public AwkTextSplitResult Patsplit(AwkText input, string pattern, bool captureSeparators, AwkRuntimeCounters counters) =>
        _byteSplitter.Patsplit(input, pattern, captureSeparators, counters);

}
