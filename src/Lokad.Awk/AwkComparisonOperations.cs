namespace Lokad.Awk;

internal static class AwkComparisonOperations
{
    public static bool Evaluate(
        AwkValue left,
        AwkValue right,
        AwkComparison comparison,
        AwkRuntimeState state)
    {
        if (left.TryGetNumber(out var leftNumber) && right.TryGetNumber(out var rightNumber))
            return EvaluateNumbers(leftNumber, rightNumber, comparison);

        return EvaluateTextComparison(Compare(left, right, state), comparison);
    }

    internal static bool Evaluate(AwkComparisonOperand left, AwkComparisonOperand right,
        AwkComparison comparison, AwkRuntimeState state)
    {
        if (!left.IsBorrowed && !right.IsBorrowed)
            return Evaluate(left.Value, right.Value, comparison, state);
        if (left.Value.TryGetNumber(out var first) && right.Value.TryGetNumber(out var second))
            return EvaluateNumbers(first, second, comparison);
        state.Counters.RecordByteTextComparison();
        return EvaluateTextComparison(state.CompareByteText(left.Text(state), right.Text(state)), comparison);
    }

    internal static bool EvaluateNumbers(double leftNumber, double rightNumber, AwkComparison comparison)
    {
        return comparison switch
        {
            AwkComparison.GreaterOrEqual => leftNumber >= rightNumber,
            AwkComparison.GreaterThan => leftNumber > rightNumber,
            AwkComparison.LessOrEqual => leftNumber <= rightNumber,
            AwkComparison.LessThan => leftNumber < rightNumber,
            AwkComparison.Equal => leftNumber == rightNumber,
            AwkComparison.NotEqual => leftNumber != rightNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison))
        };
    }

    internal static bool EvaluateTextComparison(int result, AwkComparison comparison) =>
        comparison switch
        {
            AwkComparison.GreaterOrEqual => result >= 0,
            AwkComparison.GreaterThan => result > 0,
            AwkComparison.LessOrEqual => result <= 0,
            AwkComparison.LessThan => result < 0,
            AwkComparison.Equal => result == 0,
            AwkComparison.NotEqual => result != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison))
        };

    public static int Compare(AwkValue left, AwkValue right, AwkRuntimeState state)
    {
        if (left.TryGetNumber(out var leftNumber) && right.TryGetNumber(out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        state.Counters.RecordByteTextComparison();
        return state.CompareByteText(state.FormatStringByteText(left), state.FormatStringByteText(right));
    }
}
