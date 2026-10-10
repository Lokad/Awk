using System;

namespace Lokad.Awk;

internal sealed class AwkSpecialVariableDispatcher(
    AwkVariables variables,
    AwkFieldSplitter fieldSplitter,
    Func<AwkValue, AwkText> formatString,
    Action<AwkSpecialVariableId> recordAssignment,
    AwkMemoryGovernor memoryGovernor,
    AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
    Action? checkpoint = null)
{
    private static readonly AwkArrayName ProcInfo = AwkArrayName.FromText("PROCINFO");
    private static readonly AwkArrayKey FieldModeKey = new(AwkText.FromSourceLiteralString("FS"));
    private static readonly AwkValue SeparatorMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FS"));
    private static readonly AwkValue PatternMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FPAT"));
    private static readonly AwkValue WidthsMode = AwkValue.FromText(AwkText.FromSourceLiteralString("FIELDWIDTHS"));
    private static readonly KeyValuePair<AwkArrayKey, AwkValue>[] InitialProcInfo =
    [
        new(new AwkArrayKey(AwkText.FromSourceLiteralString("version")),
            AwkValue.FromText(AwkText.FromSourceLiteralString("Lokad awk"))),
        new(FieldModeKey, SeparatorMode)
    ];
    private FieldParser _parser;

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

    public void InitializeProcInfo() => variables.InitializeArrayValues(ProcInfo, InitialProcInfo);

    public void Assign(AwkVariableName name, AwkValue value)
    {
        if (!AwkSpecialVariables.TryGetScalar(name, out var id) ||
            !AwkCompatibilityNames.EnablesSpecial(id, mode))
        {
            variables.SetScalarValue(name, value);
            return;
        }

        if (id is not (AwkSpecialVariableId.FieldSeparator or AwkSpecialVariableId.FieldPattern or
            AwkSpecialVariableId.FieldWidths or AwkSpecialVariableId.RecordSeparator))
        {
            variables.SetScalarValue(name, value);
            recordAssignment(id);
            return;
        }

        var parser = _parser;
        var publish = false;
        var text = AwkText.Empty;
        List<AwkFieldWidth>? widths = null;
        var widthBytes = 0L;
        switch (id)
        {
            case AwkSpecialVariableId.FieldSeparator:
                text = formatString(value);
                fieldSplitter.ValidateFieldSeparator(text);
                SelectSeparator(text, ResolveText(AwkSpecialVariableId.RecordSeparator),
                    !text.Equals(ResolveText(id)));
                break;
            case AwkSpecialVariableId.FieldPattern:
                text = formatString(value);
                fieldSplitter.ValidateFieldPattern(text);
                parser = FieldParser.Pattern;
                publish = parser != _parser;
                break;
            case AwkSpecialVariableId.FieldWidths:
                text = formatString(value);
                widthBytes = fieldSplitter.EstimateFieldWidthsMemory(text);
                parser = FieldParser.Widths;
                publish = parser != _parser;
                break;
            case AwkSpecialVariableId.RecordSeparator when IsSeparatorParser(_parser):
                text = formatString(value);
                SelectSeparator(ResolveText(AwkSpecialVariableId.FieldSeparator), text,
                    !text.Equals(ResolveText(id)));
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
        memoryGovernor.PreflightAllocation(AwkRuntimeMemory.CheckedAdd(growth, widthBytes));
        if (id == AwkSpecialVariableId.FieldWidths)
            widths = fieldSplitter.PrepareFieldWidths(text);

        // Every check precedes storage, splitter changes, and metadata publication.
        variables.SetScalarValue(name, value);
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
            formatString(variables.GetOrCreateScalarValue(AwkSpecialVariables.VariableName(special)));

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
