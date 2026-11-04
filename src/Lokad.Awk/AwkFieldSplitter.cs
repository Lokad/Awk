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
    AwkMemoryAccounting memoryAccounting,
    AwkRuntimeCounters counters,
    AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
    Action<object>? resetRegexWarningSite = null,
    bool csv = false)
{
    private static readonly AwkFieldSeparatorSpec NewlineSeparator = AwkFieldSeparatorSpec.FromText("\n");
    private readonly AwkByteFieldSplitter _byteSplitter = new(
        createRegex,
        createLiteralRegex,
        enumerateMatchSpans,
        resolveRegexCaseSensitivity,
        stringUnitMode,
        throwIfCancellationRequested,
        memoryAccounting,
        counters,
        resetRegexWarningSite);

    private AwkFieldSeparatorSpec _fieldSeparator = fieldSeparator;
    private AwkText _fieldPattern = AwkText.Empty;
    private List<AwkFieldWidth> _fieldWidths = [];
    private long _fieldWidthsBytes;
    private AwkFieldSplittingMode _fieldSplittingMode = csv
        ? AwkFieldSplittingMode.Csv : AwkFieldSplittingMode.FieldSeparator;
    private AwkRecordFieldSplittingMode _recordFieldSplittingMode;
    private PreparedRecordPlans _recordPlans = PrepareRecordPlans(
        csv ? AwkFieldSplittingMode.Csv : AwkFieldSplittingMode.FieldSeparator, fieldSeparator, AwkText.Empty, []);

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _recordFieldSplittingMode = mode;

    public void AssignFieldSeparator(AwkText value)
    {
        var separator = AwkFieldSeparatorSpec.FromByteText(value);
        if (_fieldSplittingMode == AwkFieldSplittingMode.FieldSeparator && _fieldSeparator.HasSameStorage(separator))
            return;
        ClearUnusedFieldWidths();
        _fieldSeparator = separator;
        _fieldSplittingMode = AwkFieldSplittingMode.FieldSeparator;
        PrepareRecordPlans();
    }

    public void AssignFieldPattern(AwkText value)
    {
        if (_fieldSplittingMode == AwkFieldSplittingMode.FieldPattern && ReferenceEquals(_fieldPattern, value))
            return;
        ClearUnusedFieldWidths();
        _fieldPattern = value;
        _fieldSplittingMode = AwkFieldSplittingMode.FieldPattern;
        PrepareRecordPlans();
    }

    public void ValidateFieldSeparator(AwkText value, bool resetHistory = false)
    {
        if (resetHistory || !value.Equals(_fieldSeparator.ByteText))
            _byteSplitter.ResetWarningHistory();
        _byteSplitter.ValidateSeparator(AwkFieldSeparatorSpec.FromByteText(value));
    }

    public void ValidateFieldPattern(AwkText value)
    {
        if (!value.Equals(_fieldPattern))
            resetRegexWarningSite?.Invoke(this);
        var pattern = new AwkRegexPattern(value, warningSite: this);
        _ = createRegex(pattern, AwkRegexCaseSensitivity.CaseSensitive);
        _ = createRegex(pattern, AwkRegexCaseSensitivity.IgnoreCase);
    }

    public long EstimateFieldWidthsMemory(AwkText value)
    {
        throwIfCancellationRequested();
        return AwkRuntimeMemory.EstimateFieldWidthListBytes(AwkFieldWidthsParser.Parse(value.AsSpan(throwIfCancellationRequested), null, throwIfCancellationRequested));
    }

    public List<AwkFieldWidth> PrepareFieldWidths(AwkText value)
    {
        var count = AwkFieldWidthsParser.Parse(value.AsSpan(throwIfCancellationRequested), null, throwIfCancellationRequested);
        var widths = new List<AwkFieldWidth>(count);
        AwkFieldWidthsParser.Parse(value.AsSpan(throwIfCancellationRequested), widths, throwIfCancellationRequested);
        return widths;
    }

    public void AssignFieldWidths(List<AwkFieldWidth> widths)
    {
        var bytes = AwkRuntimeMemory.EstimateFieldWidthListBytes(widths.Capacity);
        if (bytes > _fieldWidthsBytes)
        {
            memoryAccounting.Reserve(bytes - _fieldWidthsBytes);
            memoryAccounting.CommitReserved(bytes - _fieldWidthsBytes);
        }
        else
            memoryAccounting.ReleaseCommitted(_fieldWidthsBytes - bytes);
        _fieldWidthsBytes = bytes;
        _fieldWidths = widths;
        _fieldSplittingMode = AwkFieldSplittingMode.FieldWidths;
        PrepareRecordPlans();
    }

    private void ClearUnusedFieldWidths()
    {
        memoryAccounting.ReleaseCommitted(_fieldWidthsBytes);
        _fieldWidthsBytes = 0;
        _fieldWidths = [];
    }

    public AwkRecordSplitPlan CaptureRecordPlan() =>
        CaptureRecordPlan(_recordFieldSplittingMode);

    public AwkRecordSplitPlan CaptureRecordPlan(AwkRecordFieldSplittingMode recordMode) =>
        _recordPlans.Get(recordMode, _fieldSplittingMode == AwkFieldSplittingMode.Csv
            ? AwkRegexCaseSensitivity.CaseSensitive : resolveRegexCaseSensitivity());

    private void PrepareRecordPlans() =>
        _recordPlans = PrepareRecordPlans(_fieldSplittingMode, _fieldSeparator, _fieldPattern, _fieldWidths);

    private static PreparedRecordPlans PrepareRecordPlans(AwkFieldSplittingMode mode,
        AwkFieldSeparatorSpec separator, AwkText pattern, IReadOnlyList<AwkFieldWidth> widths)
    {
        separator = mode == AwkFieldSplittingMode.FieldSeparator ? separator : default;
        pattern = mode == AwkFieldSplittingMode.FieldPattern ? pattern : AwkText.Empty;
        widths = mode == AwkFieldSplittingMode.FieldWidths ? widths : [];
        var configuration = new AwkRecordSplitPlan.Configuration(mode, separator, pattern, widths);
        return new(
            new(configuration, AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Ordinary),
            new(configuration, AwkRegexCaseSensitivity.CaseSensitive, AwkRecordFieldSplittingMode.Paragraph),
            new(configuration, AwkRegexCaseSensitivity.IgnoreCase, AwkRecordFieldSplittingMode.Ordinary),
            new(configuration, AwkRegexCaseSensitivity.IgnoreCase, AwkRecordFieldSplittingMode.Paragraph));
    }

    private readonly record struct PreparedRecordPlans(AwkRecordSplitPlan Ordinary,
        AwkRecordSplitPlan Paragraph, AwkRecordSplitPlan OrdinaryIgnoreCase, AwkRecordSplitPlan ParagraphIgnoreCase)
    {
        public AwkRecordSplitPlan Get(AwkRecordFieldSplittingMode mode, AwkRegexCaseSensitivity sensitivity) =>
            sensitivity == AwkRegexCaseSensitivity.IgnoreCase
                ? mode == AwkRecordFieldSplittingMode.Paragraph ? ParagraphIgnoreCase : OrdinaryIgnoreCase
                : mode == AwkRecordFieldSplittingMode.Paragraph ? Paragraph : Ordinary;
    }

    internal int ExtendRecordFields(ReadOnlySpan<byte> line, AwkRecordSplitPlan plan, int position,
        int targetCount, List<AwkFieldText> fields) =>
        _byteSplitter.ExtendRecordFields(line, plan.Separator, position, targetCount, fields);

    internal List<AwkFieldText> SplitRecordByteFields(ReadOnlySpan<byte> line, AwkRecordSplitPlan plan,
        List<AwkFieldText>? fieldBuffer) =>
        _byteSplitter.SplitRecordByteFields(line, plan.Separator, fieldBuffer);

    public int ExtendRecordFields(AwkText line, AwkRecordSplitPlan plan, int position,
        int targetCount, List<AwkFieldText> fields) =>
        _byteSplitter.ExtendRecordFields(line, plan.Separator, position, targetCount, fields);

    public List<AwkFieldText> SplitRecordFields(AwkText line) =>
        SplitRecordFields(line, CaptureRecordPlan());

    public List<AwkFieldText> SplitRecordFields(AwkText line, AwkRecordSplitPlan plan,
        List<AwkFieldText>? fieldBuffer = null)
    {
        throwIfCancellationRequested();
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
                void AdvanceRuneOffset(
                    ReadOnlySpan<byte> content,
                    ref int runePosition,
                    ref int bytePosition,
                    int targetRunePosition,
                    ref long nextCheckpoint)
                {
                    while (runePosition < targetRunePosition && bytePosition < content.Length)
                    {
                        if (bytePosition >= nextCheckpoint)
                        {
                            throwIfCancellationRequested();
                            nextCheckpoint = (long)bytePosition + AwkRuntimeLimits.CancellationCheckBytes;
                        }
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
                long nextCheckpoint = 0;
                var content = line.AsSpan(throwIfCancellationRequested);
                for (var index = 0; index < fieldWidths.Count; index++)
                {
                    if ((index & 1023) == 0)
                        throwIfCancellationRequested();
                    if (bytePosition >= content.Length)
                        break;
                    var width = fieldWidths[index];
                    var fieldStartRune = AddFieldWidthUnits(position, width.Skip.Value);
                    AdvanceRuneOffset(content, ref runePosition, ref bytePosition, fieldStartRune, ref nextCheckpoint);
                    if (runePosition < fieldStartRune)
                    {
                        fields.Add(AwkFieldText.Empty);
                        position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
                        continue;
                    }

                    var fieldStartByte = bytePosition;
                    position = AddFieldWidthUnits(fieldStartRune, width.Width.Value);
                    AdvanceRuneOffset(content, ref runePosition, ref bytePosition, position, ref nextCheckpoint);
                    fields.Add(AwkFieldText.FromRecordSlice(fieldStartByte, bytePosition - fieldStartByte));
                }

                return fields;
            }

            List<AwkFieldText> SplitByteFields()
            {
                void ThrowIfNotUtf8Boundary(AwkText text, int position)
                {
                    if (position <= 0 || position >= text.ByteLength)
                        return;

                    if ((text.AsSpan(throwIfCancellationRequested)[position] & 0xC0) != 0x80)
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
                    if ((index & 1023) == 0)
                        throwIfCancellationRequested();
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
            AwkFieldSplittingMode.Csv => AwkCsvFields.SplitRecord(line, memoryAccounting, counters, throwIfCancellationRequested),
            AwkFieldSplittingMode.FieldPattern => _byteSplitter.PatsplitRecordFields(
                line, new AwkRegexPattern(plan.FieldPattern, warningSite: this), plan.CaseSensitivity),
            AwkFieldSplittingMode.FieldWidths => SplitFieldWidthRecordFields(),
            _ => plan.RecordMode == AwkRecordFieldSplittingMode.Paragraph
                ? _byteSplitter.SplitParagraphRecordFieldsBySeparator(
                    line,
                    plan.Separator,
                    plan.CaseSensitivity, fieldBuffer)
                : _byteSplitter.SplitRecordFieldsBySeparator(
                    line,
                    plan.Separator,
                    plan.CaseSensitivity, fieldBuffer)
        };
    }

    public AwkTextSplitResult SplitFieldTextsBySeparator(
        AwkText line,
        AwkFieldSeparatorSpec separator,
        AwkSeparatorCaptureMode captureMode,
        AwkRuntimeCounters counters)
    {
        throwIfCancellationRequested();
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
        throwIfCancellationRequested();
        return _byteSplitter.Patsplit(input, pattern, captureMode, counters);
    }

}
