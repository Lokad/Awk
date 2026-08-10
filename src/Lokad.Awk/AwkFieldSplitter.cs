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
    AwkMemoryGovernor memoryGovernor,
    AwkRuntimeCounters counters)
{
    private readonly AwkByteFieldSplitter _byteSplitter = new(
        createRegex,
        createLiteralRegex,
        enumerateMatchSpans,
        isIgnoreCaseEnabled,
        charactersAsBytes,
        throwIfCancellationRequested,
        memoryGovernor,
        counters);

    private AwkFieldSeparatorSpec _fieldSeparator = fieldSeparator;
    private string _fieldPattern = string.Empty;
    private IReadOnlyList<AwkFieldWidth> _fieldWidths = [];
    private AwkFieldSplittingMode _fieldSplittingMode;
    private AwkRecordFieldSplittingMode _recordFieldSplittingMode;

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _recordFieldSplittingMode = mode;

    public void AssignFieldSeparator(AwkText value)
    {
        _fieldSeparator = AwkFieldSeparatorSpec.FromByteText(value);
        _fieldSplittingMode = AwkFieldSplittingMode.FieldSeparator;
    }

    public void AssignFieldPattern(AwkText value)
    {
        _fieldPattern = value.AsString(counters);
        if (value.ByteLength > 0)
            _fieldSplittingMode = AwkFieldSplittingMode.FieldPattern;
    }

    public void AssignFieldWidths(AwkText value)
    {
        static IReadOnlyList<AwkFieldWidth> ParseFieldWidths(ReadOnlySpan<byte> text)
        {
            static bool IsWhitespace(byte value) =>
                value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or (byte)'\v' or (byte)'\f';

            static bool TryParseNonNegativeInteger(ReadOnlySpan<byte> value, out int result)
            {
                result = 0;
                if (value is [(byte)'+', .. var positive])
                    value = positive;

                if (value.IsEmpty)
                    return false;

                for (var index = 0; index < value.Length; index++)
                {
                    var digit = value[index] - (byte)'0';
                    if (digit > 9 || result > (int.MaxValue - digit) / 10)
                        return false;

                    result = result * 10 + digit;
                }

                return true;
            }

            var widths = new List<AwkFieldWidth>();
            var position = 0;
            while (position < text.Length)
            {
                while (position < text.Length && IsWhitespace(text[position]))
                    position++;

                var start = position;
                while (position < text.Length && !IsWhitespace(text[position]))
                    position++;

                if (start == position)
                    break;

                var part = text[start..position];
                var separatorIndex = part.IndexOf((byte)':');
                var skipText = separatorIndex >= 0 ? part[..separatorIndex] : "0"u8;
                var widthText = separatorIndex >= 0 ? part[(separatorIndex + 1)..] : part;
                if (!TryParseNonNegativeInteger(skipText, out var skip) ||
                    !TryParseNonNegativeInteger(widthText, out var width))
                {
                    continue;
                }

                widths.Add(new AwkFieldWidth(new AwkFieldSkip(skip), new AwkFieldWidthLength(width)));
            }

            return widths;
        }

        _fieldWidths = ParseFieldWidths(value.AsSpan());
        if (_fieldWidths.Count > 0)
            _fieldSplittingMode = AwkFieldSplittingMode.FieldWidths;
    }

    public IReadOnlyList<AwkFieldText> SplitRecordFields(AwkText line)
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
                    _fieldSeparator)
                : _byteSplitter.SplitRecordFieldsBySeparator(
                    line,
                    _fieldSeparator)
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
