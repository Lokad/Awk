using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkVariableAssignmentTests
{
    [Theory]
    [InlineData(nameof(AwkCompatibilityMode.GnuDefault), "line\nnextA")]
    [InlineData(nameof(AwkCompatibilityMode.Posix), "line\nnextu0041")]
    [InlineData(nameof(AwkCompatibilityMode.Traditional), "line\nnextA")]
    public void AwkVariableAssignment_PreparesEscapesUnderTheFinalProfile(string modeName, string expected)
    {
        var mode = Enum.Parse<AwkCompatibilityMode>(modeName);
        Assert.True(AwkVariableAssignment.TryParse(
            "name=line\\nnext\\u0041",
            AwkVariableAssignmentSource.Option,
            out var parsed));
        var assignment = parsed.WithCompatibilityMode(mode, null);

        Assert.Equal("name", assignment.Identifier.Text);
        Assert.Equal(expected, assignment.Value);
        Assert.Equal("line\\nnext\\u0041", assignment.RawValue);
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
