using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkFieldSplitter(
    AwkFieldSeparatorSpec fieldSeparator,
    Func<AwkRegexPattern, AwkRegexCaseSensitivity, AwkRegex> createRegex,
    Func<AwkRegexLiteralText, AwkRegexCaseSensitivity, AwkRegex> createLiteralRegex,
    Func<AwkRegex, AwkText, AwkRegexMatchSpanEnumerator> enumerateMatchSpans,
    Func<AwkRegexCaseSensitivity> resolveRegexCaseSensitivity,
    AwkStringUnitMode stringUnitMode,
    Action throwIfCancellationRequested,
    AwkMemoryGovernor memoryGovernor,
    AwkRuntimeCounters counters,
    Action<int>? recordByteWork = null,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
{
    private static readonly AwkFieldSeparatorSpec NewlineSeparator = AwkFieldSeparatorSpec.FromText("\n");
    private readonly AwkByteFieldSplitter _byteSplitter = new(
        createRegex,
        createLiteralRegex,
        enumerateMatchSpans,
        resolveRegexCaseSensitivity,
        stringUnitMode,
        throwIfCancellationRequested,
        memoryGovernor,
        counters);

    private AwkFieldSeparatorSpec _fieldSeparator = fieldSeparator;
    private AwkText _fieldPattern = AwkText.Empty;
    private List<AwkFieldWidth> _fieldWidths = [];
    private long _fieldWidthsBytes;
    private AwkFieldSplittingMode _fieldSplittingMode;
    private AwkRecordFieldSplittingMode _recordFieldSplittingMode;

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _recordFieldSplittingMode = mode;

    public void AssignFieldSeparator(AwkText value)
    {
        ClearUnusedFieldWidths();
        _fieldSeparator = AwkFieldSeparatorSpec.FromByteText(value);
        _fieldSplittingMode = AwkFieldSplittingMode.FieldSeparator;
    }

    public void AssignFieldPattern(AwkText value)
    {
        ClearUnusedFieldWidths();
        _fieldPattern = value;
        _fieldSplittingMode = AwkFieldSplittingMode.FieldPattern;
    }

    public void ValidateFieldSeparator(AwkText value)
    {
        _byteSplitter.ValidateSeparator(AwkFieldSeparatorSpec.FromByteText(value));
    }

    public void ValidateFieldPattern(AwkText value) =>
        _ = createRegex(new AwkRegexPattern(value.AsString(counters), warningSite: this), resolveRegexCaseSensitivity());

    public long EstimateFieldWidthsMemory(AwkText value)
    {
        recordByteWork?.Invoke(value.ByteLength);
        return AwkRuntimeMemory.EstimateFieldWidthListBytes(AwkFieldWidthsParser.Parse(value.AsSpan(), null));
    }

    public List<AwkFieldWidth> PrepareFieldWidths(AwkText value)
    {
        var count = AwkFieldWidthsParser.Parse(value.AsSpan(), null);
        memoryGovernor.PreflightAllocation(AwkRuntimeMemory.EstimateFieldWidthListBytes(count));
        var widths = new List<AwkFieldWidth>(count);
        AwkFieldWidthsParser.Parse(value.AsSpan(), widths);
        return widths;
    }

    public void AssignFieldWidths(List<AwkFieldWidth> widths)
    {
        var bytes = AwkRuntimeMemory.EstimateFieldWidthListBytes(widths.Capacity);
        if (bytes > _fieldWidthsBytes)
        {
            memoryGovernor.Reserve(bytes - _fieldWidthsBytes);
            memoryGovernor.CommitReserved(bytes - _fieldWidthsBytes);
        }
        else
            memoryGovernor.ReleaseCommitted(_fieldWidthsBytes - bytes);
        _fieldWidthsBytes = bytes;
        _fieldWidths = widths;
        _fieldSplittingMode = AwkFieldSplittingMode.FieldWidths;
    }

    private void ClearUnusedFieldWidths()
    {
        memoryGovernor.ReleaseCommitted(_fieldWidthsBytes);
        _fieldWidthsBytes = 0;
        _fieldWidths = [];
    }

    public AwkRecordSplitPlan CaptureRecordPlan() =>
        CaptureRecordPlan(_recordFieldSplittingMode);

    public AwkRecordSplitPlan CaptureRecordPlan(AwkRecordFieldSplittingMode recordMode) =>
        _fieldSplittingMode switch
        {
            AwkFieldSplittingMode.FieldPattern => new(
                _fieldSplittingMode, default, _fieldPattern, [], resolveRegexCaseSensitivity(), recordMode),
            AwkFieldSplittingMode.FieldWidths => new(
                _fieldSplittingMode, default, AwkText.Empty, _fieldWidths, resolveRegexCaseSensitivity(), recordMode),
            _ => new(
                _fieldSplittingMode, _fieldSeparator, AwkText.Empty, [], resolveRegexCaseSensitivity(), recordMode)
        };

    public List<AwkFieldText> SplitRecordFields(AwkText line) =>
        SplitRecordFields(line, CaptureRecordPlan());

    public List<AwkFieldText> SplitRecordFields(AwkText line, AwkRecordSplitPlan plan)
    {
        recordByteWork?.Invoke(line.ByteLength);
        if (compatibilityMode != AwkCompatibilityMode.GnuDefault &&
            plan.Mode == AwkFieldSplittingMode.FieldSeparator && plan.Separator.ByteText.ByteLength == 0)
        {
            if (line.ByteLength == 0)
                return [];
            return plan.RecordMode == AwkRecordFieldSplittingMode.Paragraph
                ? _byteSplitter.SplitRecordFieldsBySeparator(line,
                    NewlineSeparator, plan.CaseSensitivity)
                : [AwkFieldText.FromRecordSlice(0, line.ByteLength)];
        }
        var fieldWidths = plan.FieldWidths;
        static int AddFieldWidthUnits(int position, int value)
        {
            if (position > int.MaxValue - value)
                return int.MaxValue;

            return position + value;
        }

        List<AwkFieldText> SplitFieldWidthRecordFields()
        {
            List<AwkFieldText> SplitRuneFields()
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

                var fields = new List<AwkFieldText>(fieldWidths.Count);
                var position = 0;
                var bytePosition = 0;
                var runePosition = 0;
                var content = line.AsSpan();
                for (var index = 0; index < fieldWidths.Count; index++)
                {
                    if (bytePosition >= content.Length)
                        break;
                    var width = fieldWidths[index];
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

            List<AwkFieldText> SplitByteFields()
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

                var fields = new List<AwkFieldText>(fieldWidths.Count);
                var position = 0;
                var sourceLength = line.ByteLength;
                for (var index = 0; index < fieldWidths.Count; index++)
                {
                    if (position >= sourceLength)
                        break;
                    var width = fieldWidths[index];
                    position = AddFieldWidthUnits(position, width.Skip.Value);
                    ThrowIfNotUtf8Boundary(line, position);
                    if (position >= sourceLength)
                    {
                        fields.Add(AwkFieldText.Empty);
                        break;
                    }

                    var length = Math.Min(width.Width.Value, sourceLength - position);
                    ThrowIfNotUtf8Boundary(line, position + length);
                    fields.Add(AwkFieldText.FromRecordSlice(position, length));
                    position = AddFieldWidthUnits(position, width.Width.Value);
                }

                return fields;
            }

            if (fieldWidths.Count == 0)
                return [];

            return stringUnitMode is AwkStringUnitMode.Byte
                ? SplitByteFields()
                : SplitRuneFields();
        }

        return plan.Mode switch
        {
            AwkFieldSplittingMode.FieldPattern => _byteSplitter.PatsplitRecordFields(
                line, new AwkRegexPattern(plan.FieldPattern.AsString(counters), warningSite: this), plan.CaseSensitivity),
            AwkFieldSplittingMode.FieldWidths => SplitFieldWidthRecordFields(),
            _ => plan.RecordMode == AwkRecordFieldSplittingMode.Paragraph
                ? _byteSplitter.SplitParagraphRecordFieldsBySeparator(
                    line,
                    plan.Separator,
                    plan.CaseSensitivity)
                : _byteSplitter.SplitRecordFieldsBySeparator(
                    line,
                    plan.Separator,
                    plan.CaseSensitivity)
        };
    }

    public AwkTextSplitResult SplitFieldTextsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        AwkSeparatorCaptureMode captureMode,
        AwkRuntimeCounters counters)
    {
        recordByteWork?.Invoke(line.ByteLength);
        return _byteSplitter.SplitBySeparator(
            line,
            separator,
            captureMode,
            counters);
    }

    public AwkTextSplitResult Patsplit(
        AwkText input,
        AwkRegexPattern pattern,
        AwkSeparatorCaptureMode captureMode,
        AwkRuntimeCounters counters)
    {
        recordByteWork?.Invoke(input.ByteLength);
        return _byteSplitter.Patsplit(input, pattern, captureMode, counters);
    }

}
