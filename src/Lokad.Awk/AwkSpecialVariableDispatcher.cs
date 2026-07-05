using System;

namespace Lokad.Awk;

internal sealed class AwkSpecialVariableDispatcher(
    AwkVariables variables,
    AwkFieldSplitter fieldSplitter,
    Action<AwkSpecialVariableId> recordAssignment)
{
    public void InitializeScalars(AwkFieldSeparatorSpec fieldSeparator)
    {
        variables.SetScalarText(
            AwkSpecialVariables.Name(AwkSpecialVariableId.FieldSeparator),
            fieldSeparator.Text);
        foreach (var variable in AwkSpecialVariables.Scalars)
        {
            if (variable.Id == AwkSpecialVariableId.FieldSeparator ||
                !variable.InitialValue.HasValue)
            {
                continue;
            }

            variables.SetScalarValue(variable.Name, variable.InitialValue.Value);
        }
    }

    public void ApplyAssignment(string name, string value)
    {
        if (AwkSpecialVariables.TryGetScalar(name, out var id))
            ApplyAssignment(id, value);
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
            ApplyAssignmentEffects(id, value.AsString());
    }

    public void ApplyAssignment(AwkSpecialVariableId id, string value)
    {
        recordAssignment(id);
        ApplyAssignmentEffects(id, value);
    }

    private void ApplyAssignmentEffects(AwkSpecialVariableId id, string value)
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
