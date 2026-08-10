using System;

namespace Lokad.Awk;

internal sealed class AwkSpecialVariableDispatcher(
    AwkVariables variables,
    AwkFieldSplitter fieldSplitter,
    Func<AwkValue, AwkText> formatString,
    Action<AwkSpecialVariableId> recordAssignment)
{
    public void InitializeScalars(AwkFieldSeparatorSpec fieldSeparator)
    {
        variables.SetScalarValue(
            AwkSpecialVariables.VariableName(AwkSpecialVariableId.FieldSeparator),
            AwkValue.FromText(fieldSeparator.ByteText));
        foreach (var variable in AwkSpecialVariables.Scalars)
        {
            if (variable.Id == AwkSpecialVariableId.FieldSeparator ||
                !variable.InitialValue.HasValue)
            {
                continue;
            }

            variables.SetScalarValue(variable.VariableName, variable.InitialValue.Value);
        }
    }

    public void ApplyAssignment(string name, AwkValue value)
    {
        if (AwkSpecialVariables.TryGetScalar(name, out var id))
            ApplyAssignment(id, value);
    }

    public void ApplyAssignment(AwkSpecialVariableId id, AwkValue value)
    {
        recordAssignment(id);
        if (RequiresFieldSplitterUpdate(id))
            ApplyAssignmentEffects(id, formatString(value));
    }

    public void ApplyAssignment(AwkSpecialVariableId id, AwkText value)
    {
        recordAssignment(id);
        ApplyAssignmentEffects(id, value);
    }

    private void ApplyAssignmentEffects(AwkSpecialVariableId id, AwkText value)
    {
        switch (id)
        {
            case AwkSpecialVariableId.FieldSeparator:
                fieldSplitter.AssignFieldSeparator(value);
                break;
            case AwkSpecialVariableId.FieldPattern:
                fieldSplitter.AssignFieldPattern(value);
                break;
            case AwkSpecialVariableId.FieldWidths:
                fieldSplitter.AssignFieldWidths(value);
                break;
        }
    }

    private static bool RequiresFieldSplitterUpdate(AwkSpecialVariableId id) =>
        id is AwkSpecialVariableId.FieldSeparator or
            AwkSpecialVariableId.FieldPattern or
            AwkSpecialVariableId.FieldWidths;
}
