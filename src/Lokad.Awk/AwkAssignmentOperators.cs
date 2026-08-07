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

    public static bool TryRead(
        string text,
        ref int index,
        out AwkCompoundAssignmentOperation? compoundOperation)
    {
        compoundOperation = null;
        if (index >= text.Length)
            return false;

        if (index + 1 < text.Length)
        {
            var first = text[index];
            var second = text[index + 1];
            if (second == '=' && TryGetCompoundOperation(first, out var operation))
            {
                compoundOperation = operation;
                index += 2;
                return true;
            }

            if (first == '=' && second == '=')
                return false;
        }

        if (text[index] != '=')
            return false;

        index++;
        return true;
    }

    private static bool TryGetCompoundOperation(
        char operatorToken,
        out AwkCompoundAssignmentOperation operation)
    {
        switch (operatorToken)
        {
            case '+':
                operation = AwkCompoundAssignmentOperation.Add;
                return true;
            case '-':
                operation = AwkCompoundAssignmentOperation.Subtract;
                return true;
            case '*':
                operation = AwkCompoundAssignmentOperation.Multiply;
                return true;
            case '/':
                operation = AwkCompoundAssignmentOperation.Divide;
                return true;
            case '%':
                operation = AwkCompoundAssignmentOperation.Modulo;
                return true;
            case '^':
                operation = AwkCompoundAssignmentOperation.Power;
                return true;
            default:
                operation = default;
                return false;
        }
    }
}
