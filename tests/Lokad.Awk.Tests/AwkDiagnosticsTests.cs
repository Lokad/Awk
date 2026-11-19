using System.Text;
using Lokad.Awk;
using ToolAwkMessages = Lokad.Awk.AwkMessages;

namespace Lokad.Awk.Tests;

public sealed class AwkDiagnosticsTests
{
    [Fact]
    public void AwkDiagnostic_ClassifiesParserBinderAndWarningDiagnostics()
    {
        var syntax = new AwkDiagnostic(new AwkTextSpan(0, 1), "Unexpected token.");
        var regex = new AwkDiagnostic(
            new AwkTextSpan(1, 1),
            "Invalid regular expression.",
            AwkDiagnosticId.ParseInvalidRegex);
        var warning = new AwkDiagnostic(
            new AwkTextSpan(2, 1),
            "escape sequence.",
            AwkDiagnosticId.WarningStringEscape);
        var bind = new AwkDiagnostic(
            new AwkTextSpan(3, 1),
            "Invalid action block.",
            AwkDiagnosticId.BindInvalidProgram);

        Assert.Equal(AwkDiagnosticId.ParseSyntax, syntax.Id);
        Assert.Equal(AwkDiagnosticCategory.Parse, syntax.Category);
        Assert.False(syntax.IsWarning);

        Assert.Equal(AwkDiagnosticId.ParseInvalidRegex, regex.Id);
        Assert.Equal(AwkDiagnosticCategory.Parse, regex.Category);

        Assert.Equal(AwkDiagnosticId.WarningStringEscape, warning.Id);
        Assert.Equal(AwkDiagnosticCategory.Warning, warning.Category);
        Assert.True(warning.IsWarning);

        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, bind.Id);
        Assert.Equal(AwkDiagnosticCategory.Bind, bind.Category);
    }

    [Fact]
    public void AwkDiagnostics_SelectsTheFirstErrorWithInvalidRegexTakingPrecedence()
    {
        var span = new AwkTextSpan(0, 1);
        var warning = new AwkDiagnostic(span, "escape sequence.", AwkDiagnosticId.WarningStringEscape);
        var first = new AwkDiagnostic(span, "Invalid action block.", AwkDiagnosticId.BindInvalidProgram);
        var later = new AwkDiagnostic(span, "Unexpected token.", AwkDiagnosticId.ParseSyntax);
        var regex = new AwkDiagnostic(span, "Invalid regex.", AwkDiagnosticId.ParseInvalidRegex);

        var ordinary = AwkDiagnostics.Parse([warning, first, later]);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, ordinary.Id);
        Assert.Equal("awk: invalid action block.\n", Decode(ordinary.Message));

        var invalidRegex = AwkDiagnostics.Parse([warning, first, later, regex]);
        Assert.Equal(AwkDiagnosticId.ParseInvalidRegex, invalidRegex.Id);
        Assert.Equal("awk: invalid regular expression\n", Decode(invalidRegex.Message));
    }

    [Fact]
    public void AwkDiagnostics_FormatsWarningsThroughSharedPath()
    {
        var warning = new AwkDiagnostic(
            new AwkTextSpan(0, 0),
            ToolAwkMessages.FormatGensubThirdArgumentWarningText("0"),
            AwkDiagnosticId.WarningRuntime);

        Assert.Equal(AwkDiagnosticCategory.Warning, warning.Category);
        Assert.Equal("awk: warning: gensub: third argument `0' treated as 1\n", Decode(AwkDiagnostics.Warning(warning)));
    }

    private static string Decode(ReadOnlyMemory<byte> bytes) =>
        Encoding.UTF8.GetString(bytes.Span);
}
