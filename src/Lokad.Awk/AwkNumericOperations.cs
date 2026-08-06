using System;

namespace Lokad.Awk;

internal static class AwkNumericOperations
{
    public static double Apply(double leftValue, double rightValue, AwkNumericOperation operation) =>
        operation switch
        {
            AwkNumericOperation.Add => leftValue + rightValue,
            AwkNumericOperation.Subtract => leftValue - rightValue,
            AwkNumericOperation.Multiply => leftValue * rightValue,
            AwkNumericOperation.Divide => Divide(leftValue, rightValue, AwkMessages.ErrorDivisionByZero),
            AwkNumericOperation.Modulo => Modulo(leftValue, rightValue, AwkMessages.ErrorDivisionByZeroInModulo),
            AwkNumericOperation.Power => Math.Pow(leftValue, rightValue),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    public static double Divide(
        double leftValue,
        double rightValue,
        ReadOnlyMemory<byte> divisionByZeroMessage)
    {
        if (IsZero(rightValue))
            throw new AwkRuntimeException(divisionByZeroMessage);

        return leftValue / rightValue;
    }

    public static double Modulo(
        double leftValue,
        double rightValue,
        ReadOnlyMemory<byte> divisionByZeroMessage)
    {
        if (IsZero(rightValue))
            throw new AwkRuntimeException(divisionByZeroMessage);

        return leftValue % rightValue;
    }

    public static double ApplyCompoundAssignment(
        double leftValue,
        double rightValue,
        AwkCompoundAssignmentOperation operation) =>
        operation switch
        {
            AwkCompoundAssignmentOperation.Add => leftValue + rightValue,
            AwkCompoundAssignmentOperation.Subtract => leftValue - rightValue,
            AwkCompoundAssignmentOperation.Multiply => leftValue * rightValue,
            AwkCompoundAssignmentOperation.Divide => Divide(
                leftValue,
                rightValue,
                AwkMessages.ErrorDivisionByZeroInDivide),
            AwkCompoundAssignmentOperation.Modulo => Modulo(
                leftValue,
                rightValue,
                AwkMessages.ErrorDivisionByZeroInModuloAssignment),
            AwkCompoundAssignmentOperation.Power => Math.Pow(leftValue, rightValue),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    public static double Increment(double value) => value + 1;

    public static double Decrement(double value) => value - 1;

    public static bool IsZero(double value) => Math.Abs(value) < double.Epsilon;
}

internal enum AwkNumericOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power
}
