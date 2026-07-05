using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkVariableAssignmentTests
{
    [Fact]
    public void AwkVariableAssignment_ParsesOptionWithEscapeDecoding()
    {
        Assert.True(AwkVariableAssignment.TryParse(
            "name=line\\nnext",
            AwkVariableAssignmentSource.Option,
            out var assignment));

        Assert.Equal("name", assignment.Identifier.Text);
        Assert.Equal("line\nnext", assignment.Value);
        Assert.Equal(AwkVariableAssignmentSource.Option, assignment.Source);
    }

    [Fact]
    public void AwkVariableAssignment_ParsesInputArgumentWithoutEscapeDecoding()
    {
        Assert.True(AwkVariableAssignment.TryParse(
            "name=line\\nnext",
            AwkVariableAssignmentSource.InputArgument,
            out var assignment));

        Assert.Equal("name", assignment.Identifier.Text);
        Assert.Equal("line\\nnext", assignment.Value);
        Assert.Equal(AwkVariableAssignmentSource.InputArgument, assignment.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("=value")]
    [InlineData("not-valid=value")]
    [InlineData("name")]
    public void AwkVariableAssignment_RejectsInvalidAssignments(string value)
    {
        Assert.False(AwkVariableAssignment.TryParse(
            value,
            AwkVariableAssignmentSource.Option,
            out _));
    }
}
