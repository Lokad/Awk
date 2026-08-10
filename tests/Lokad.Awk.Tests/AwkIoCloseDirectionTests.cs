using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkIoCloseDirectionTests
{
    [Theory]
    [InlineData("to")]
    [InlineData("from")]
    public void AwkCloseDirectionParser_ParsesKnownDirections(string text)
    {
        Assert.True(AwkCloseDirectionParser.TryParse(text, out var direction));

        var expected = text == "to"
            ? AwkCloseDirection.To
            : AwkCloseDirection.From;
        Assert.Equal(expected, direction);
    }

    [Fact]
    public void AwkCloseDirectionParser_RejectsUnknownDirection()
    {
        Assert.False(AwkCloseDirectionParser.TryParse("both", out _));
    }

    [Fact]
    public void AwkRedirectedOutput_StoresTypedPath()
    {
        var path = new AwkRedirectionPath("/out.txt");
        var output = AwkTestHarness.CreateRedirectedOutput(
            path.Text,
            AwkOutputRedirectionKind.Write,
            "content");

        Assert.Equal(path, output.RedirectionPath);
        Assert.Equal("/out.txt", output.RedirectionPath.Text);
    }
}
