namespace Lokad.Awk;

internal sealed class AwkSpecialVariableDispatcher(
    AwkVariables variables,
    AwkFieldSplitter fieldSplitter,
    AwkFormattingRuntime formatting,
    Action<AwkSpecialVariableId> recordAssignment,
    AwkMemoryAccounting memoryAccounting,
    AwkCompatibilityMode mode,
    Action checkpoint,
    Action<AwkText> validateRecordSeparator,
    bool csv,
    Action<bool> reportCsvWarning) : IDisposable
{
    private static readonly AwkArrayName ProcInfo = AwkArrayName.FromText("PROCINFO");
    private static readonly AwkArrayKey FieldModeKey = new(AwkText.FromSourceLiteralString("FS"));
    private static readonly AwkValue SeparatorMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FS"));
    private static readonly AwkValue PatternMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FPAT"));
    private static readonly AwkValue WidthsMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FIELDWIDTHS"));
    private static readonly KeyValuePair<AwkArrayKey, AwkValue>[] InitialProcInfo =
    [
        new(new AwkArrayKey(AwkText.FromSourceLiteralString("version")),
            AwkValue.FromText(AwkText.FromSourceLiteralString(AwkVersion.Text))),
        new(FieldModeKey, SeparatorMode)
    ];
    private static readonly KeyValuePair<AwkArrayKey, AwkValue>[] InitialCsvProcInfo =
    [
        .. InitialProcInfo,
        new(new AwkArrayKey(AwkText.FromSourceLiteralString("CSV")), AwkValue.FromNumber(1))
    ];
    private FieldParser _parser;
    private bool _csvFieldsWarned;
    private bool _csvRecordsWarned;
    private NumericFormat? _numericConversionFormat;
    private NumericFormat? _numericOutputFormat;

    private readonly struct NumericFormat(AwkText text, AwkMemoryLease memory) : IDisposable
    {
        public AwkText Text { get; } = text;
        public void Dispose() => memory?.Dispose();
    }

    internal bool TryGetNumericFormat(AwkSpecialVariableId id, out AwkText text)
    {
        var format = id == AwkSpecialVariableId.ConversionFormat
            ? _numericConversionFormat : _numericOutputFormat;
        text = format?.Text ?? default;
        return format.HasValue;
    }

    private void AssignFormat(AwkVariableName name, AwkSpecialVariableId id, AwkValue value,
        AwkVariables.AwkScalarCell? cell)
    {
        AwkMemoryLease? staging = null;
        try
        {
            NumericFormat? prepared = null;
            if (value.IsNumberValue)
            {
                staging = memoryAccounting.CreateTransientLease();
                // Capture with the previous CONVFMT before publishing the number.
                // Keeping this text separate preserves typeof and numeric origins.
                var text = formatting.CaptureNumericFormat(value, staging);
                prepared = new(text, staging);
            }
            checkpoint();
            StoreScalar(name, value, cell);
            ref var current = ref (id == AwkSpecialVariableId.ConversionFormat
                ? ref _numericConversionFormat : ref _numericOutputFormat);
            var previous = current;
            current = prepared;
            staging = null;
            previous?.Dispose();
            checkpoint();
        }
        finally { staging?.Dispose(); }
    }

    public void Dispose()
    {
        var conversion = _numericConversionFormat;
        var output = _numericOutputFormat;
        _numericConversionFormat = null;
        _numericOutputFormat = null;
        try { conversion?.Dispose(); }
        finally { output?.Dispose(); }
    }

    public void InitializeScalars(AwkFieldSeparatorSpec fieldSeparator)
    {
        _parser = SelectSeparatorParser(fieldSeparator.ByteText, paragraph: false).Last;
        variables.SetScalarValue(
            AwkSpecialVariables.VariableName(AwkSpecialVariableId.FieldSeparator),
            AwkValue.FromText(fieldSeparator.ByteText, AwkNumberParsingMode.Decimal, checkpoint));
        foreach (var variable in AwkSpecialVariables.Scalars)
        {
            if (variable.Id == AwkSpecialVariableId.FieldSeparator ||
                !AwkCompatibilityNames.EnablesSpecial(variable.Id, mode) ||
                !variable.InitialValue.HasValue)
            {
                continue;
            }

            variables.SetScalarValue(variable.VariableName, variable.InitialValue.Value);
        }
    }

    public void InitializeProcInfo()
    {
        variables.InitializeArrayValues(ProcInfo, csv ? InitialCsvProcInfo : InitialProcInfo);
    }

    private void StoreScalar(AwkVariableName name, AwkValue value, AwkVariables.AwkScalarCell? cell)
    {
        if (cell is null) variables.SetScalarValue(name, value);
        else variables.SetScalarValue(cell, value);
    }

    public void Assign(AwkVariableName name, AwkValue value)
    {
        if (!AwkSpecialVariables.TryGetScalar(name, out var id) ||
            !AwkCompatibilityNames.EnablesSpecial(id, mode))
        {
            variables.SetScalarValue(name, value);
            return;
        }
        AssignSpecial(name, id, value, variables.GetSpecialScalarCell(id));
    }

    internal void AssignBound(AwkSpecialVariableId id, AwkVariables.AwkScalarCell cell, AwkValue value) =>
        AssignSpecial(cell.Name, id, value, cell);

    private void AssignSpecial(AwkVariableName name, AwkSpecialVariableId id, AwkValue value,
        AwkVariables.AwkScalarCell? cell)
    {
        if (id is not (AwkSpecialVariableId.FieldSeparator or AwkSpecialVariableId.FieldPattern or
            AwkSpecialVariableId.FieldWidths or AwkSpecialVariableId.RecordSeparator))
        {
            if (id is AwkSpecialVariableId.ConversionFormat or AwkSpecialVariableId.OutputFormat)
                AssignFormat(name, id, value, cell);
            else StoreScalar(name, value, cell);
            recordAssignment(id);
            return;
        }

        if (csv)
        {
            // Assignments remain observable as values; they do not change CSV parsing.
            using var staging = memoryAccounting.ReserveTransient(variables.EstimateScalarValueMemoryGrowth(name, value));
            var records = id == AwkSpecialVariableId.RecordSeparator;
            ref var warned = ref (records ? ref _csvRecordsWarned : ref _csvFieldsWarned);
            if (!warned)
            {
                reportCsvWarning(records);
            }
            if (id == AwkSpecialVariableId.FieldPattern)
                fieldSplitter.ValidateFieldPattern(formatting.FormatStringByteText(value));
            else if (id == AwkSpecialVariableId.FieldWidths)
                _ = fieldSplitter.CountFieldWidths(formatting.FormatStringByteText(value));
            StoreScalar(name, value, cell);
            warned = true;
            return;
        }

        var parser = _parser;
        var publish = false;
        var text = AwkText.Empty;
        List<AwkFieldWidth>? widths = null;
        var widthCount = 0;
        switch (id)
        {
            case AwkSpecialVariableId.FieldSeparator:
                text = formatting.FormatStringByteText(value);
                fieldSplitter.ValidateFieldSeparator(text);
                SelectSeparator(text, ResolveText(AwkSpecialVariableId.RecordSeparator),
                    !text.Equals(ResolveText(id)));
                break;
            case AwkSpecialVariableId.FieldPattern:
                text = formatting.FormatStringByteText(value);
                fieldSplitter.ValidateFieldPattern(text);
                parser = FieldParser.Pattern;
                publish = parser != _parser;
                break;
            case AwkSpecialVariableId.FieldWidths:
                text = formatting.FormatStringByteText(value);
                widthCount = fieldSplitter.CountFieldWidths(text);
                parser = FieldParser.Widths;
                publish = parser != _parser;
                break;
            case AwkSpecialVariableId.RecordSeparator:
                text = formatting.FormatStringByteText(value);
                validateRecordSeparator(text);
                if (IsSeparatorParser(_parser))
                {
                    var changed = !text.Equals(ResolveText(id));
                    var separator = ResolveText(AwkSpecialVariableId.FieldSeparator);
                    if (changed)
                        fieldSplitter.ValidateFieldSeparator(separator, resetHistory: true);
                    SelectSeparator(separator, text, changed);
                }
                break;
        }

        publish &= mode == AwkCompatibilityMode.GnuDefault;
        var fieldMode = parser switch
        {
            FieldParser.Pattern => PatternMode,
            FieldParser.Widths => WidthsMode,
            _ => SeparatorMode
        };
        var growth = variables.EstimateScalarValueMemoryGrowth(name, value);
        if (publish)
            growth = AwkRuntimeMemory.CheckedAdd(growth,
                variables.EstimateArrayValueMemoryGrowth(ProcInfo, FieldModeKey, fieldMode));
        using var admission = memoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(growth,
            AwkRuntimeMemory.EstimateFieldWidthListBytes(widthCount)));
        checkpoint();
        if (id == AwkSpecialVariableId.FieldWidths)
            widths = fieldSplitter.PrepareFieldWidths(text, widthCount);

        // Every check precedes storage, splitter changes, and metadata publication.
        StoreScalar(name, value, cell);
        if (publish)
            variables.SetArrayValue(ProcInfo, FieldModeKey, fieldMode);
        switch (id)
        {
            case AwkSpecialVariableId.FieldSeparator:
                fieldSplitter.AssignFieldSeparator(text);
                break;
            case AwkSpecialVariableId.FieldPattern:
                fieldSplitter.AssignFieldPattern(text);
                break;
            case AwkSpecialVariableId.FieldWidths when widths is not null:
                fieldSplitter.AssignFieldWidths(widths);
                break;
        }
        _parser = parser;
        recordAssignment(id);

        AwkText ResolveText(AwkSpecialVariableId special) =>
            formatting.FormatStringByteText(variables.GetSpecialScalarCell(special) is { } bound
                ? variables.GetScalarValue(bound)
                : variables.GetOrCreateScalarValue(AwkSpecialVariables.VariableName(special)));

        void SelectSeparator(AwkText separator, AwkText recordSeparator, bool changed)
        {
            if (!changed && IsSeparatorParser(_parser))
                return;
            var selection = SelectSeparatorParser(separator, recordSeparator.ByteLength == 0);
            parser = selection.Last;
            publish = _parser != selection.First || selection.First != selection.Last;
        }
    }

    // GNU can pass through two parser kinds even when both publish the label "FS".
    private static (FieldParser First, FieldParser Last) SelectSeparatorParser(AwkText value, bool paragraph)
    {
        if (value.ByteLength == 0)
            return (FieldParser.Characters, FieldParser.Characters);
        if (value.ByteLength > 1)
            return (FieldParser.Regex, FieldParser.Regex);
        if (paragraph)
            return (FieldParser.Character, value.EqualsAscii("\n") ? FieldParser.Character : FieldParser.Regex);
        return (FieldParser.Whitespace, value.EqualsAscii(" ") ? FieldParser.Whitespace :
            value.EqualsAscii("\\") ? FieldParser.Regex : FieldParser.Character);
    }

    private static bool IsSeparatorParser(FieldParser parser) =>
        parser is not (FieldParser.Pattern or FieldParser.Widths);

    private enum FieldParser { Whitespace, Character, Regex, Characters, Pattern, Widths }
}
