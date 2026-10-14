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

        return EvaluateTextComparison(Compare(left, right, state), comparison);
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
