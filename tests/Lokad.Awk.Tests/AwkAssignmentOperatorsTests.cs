using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkAssignmentOperatorsTests
{
    [Theory]
    [InlineData((int)AwkTokenKind.PlusEqual, (int)AwkCompoundAssignmentOperation.Add)]
    [InlineData((int)AwkTokenKind.MinusEqual, (int)AwkCompoundAssignmentOperation.Subtract)]
    [InlineData((int)AwkTokenKind.StarEqual, (int)AwkCompoundAssignmentOperation.Multiply)]
    [InlineData((int)AwkTokenKind.SlashEqual, (int)AwkCompoundAssignmentOperation.Divide)]
    [InlineData((int)AwkTokenKind.PercentEqual, (int)AwkCompoundAssignmentOperation.Modulo)]
    [InlineData((int)AwkTokenKind.CaretEqual, (int)AwkCompoundAssignmentOperation.Power)]
    public void AwkAssignmentOperators_MapsCompoundTokens(
        int kindValue,
        int expectedValue)
    {
        var kind = (AwkTokenKind)kindValue;
        var expected = (AwkCompoundAssignmentOperation)expectedValue;

        Assert.True(AwkAssignmentOperators.TryGetCompoundOperation(kind, out var operation));
        Assert.Equal(expected, operation);
    }
}
