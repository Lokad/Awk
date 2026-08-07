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

        Assert.True(AwkAssignmentOperators.IsAssignment(kind));
        Assert.True(AwkAssignmentOperators.TryGetCompoundOperation(kind, out var operation));
        Assert.Equal(expected, operation);
    }

    [Theory]
    [InlineData("+=", (int)AwkCompoundAssignmentOperation.Add)]
    [InlineData("-=", (int)AwkCompoundAssignmentOperation.Subtract)]
    [InlineData("*=", (int)AwkCompoundAssignmentOperation.Multiply)]
    [InlineData("/=", (int)AwkCompoundAssignmentOperation.Divide)]
    [InlineData("%=", (int)AwkCompoundAssignmentOperation.Modulo)]
    [InlineData("^=", (int)AwkCompoundAssignmentOperation.Power)]
    public void AwkAssignmentOperators_ReadsCompoundText(
        string text,
        int expectedValue)
    {
        var index = 0;
        var expected = (AwkCompoundAssignmentOperation)expectedValue;

        Assert.True(AwkAssignmentOperators.TryRead(text, ref index, out var operation));
        Assert.Equal(2, index);
        Assert.Equal(expected, operation);
    }

    [Fact]
    public void AwkAssignmentOperators_ReadsPlainAssignmentWithoutConsumingComparison()
    {
        var assignmentIndex = 0;
        Assert.True(AwkAssignmentOperators.TryRead("=", ref assignmentIndex, out var operation));
        Assert.Equal(1, assignmentIndex);
        Assert.Null(operation);

        var comparisonIndex = 0;
        Assert.False(AwkAssignmentOperators.TryRead("==", ref comparisonIndex, out _));
        Assert.Equal(0, comparisonIndex);
    }
}
