namespace Lokad.Awk;

internal static class AwkAssignmentOperators
{
    public static bool TryGetCompoundOperation(
        AwkTokenKind kind,
        out AwkCompoundAssignmentOperation operation)
    {
        switch (kind)
        {
            case AwkTokenKind.PlusEqual:
                operation = AwkCompoundAssignmentOperation.Add;
                return true;
            case AwkTokenKind.MinusEqual:
                operation = AwkCompoundAssignmentOperation.Subtract;
                return true;
            case AwkTokenKind.StarEqual:
                operation = AwkCompoundAssignmentOperation.Multiply;
                return true;
            case AwkTokenKind.SlashEqual:
                operation = AwkCompoundAssignmentOperation.Divide;
                return true;
            case AwkTokenKind.PercentEqual:
                operation = AwkCompoundAssignmentOperation.Modulo;
                return true;
            case AwkTokenKind.CaretEqual:
                operation = AwkCompoundAssignmentOperation.Power;
                return true;
            default:
                operation = default;
                return false;
        }
    }
}
