namespace Lokad.Awk;

internal static class AwkComparisonOperations
{
    public static bool Evaluate(
        AwkValue left,
        AwkValue right,
        AwkComparison comparison,
        AwkRuntimeCounters counters)
    {
        var result = AwkValue.Compare(left, right, counters);
        return comparison switch
        {
            AwkComparison.GreaterOrEqual => result >= 0,
            AwkComparison.GreaterThan => result > 0,
            AwkComparison.LessOrEqual => result <= 0,
            AwkComparison.LessThan => result < 0,
            AwkComparison.Equal => result == 0,
            AwkComparison.NotEqual => result != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison))
        };
    }
}
