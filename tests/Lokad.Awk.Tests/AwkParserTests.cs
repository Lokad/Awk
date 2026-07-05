using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkParserTests
{
    [Fact]
    public void AwkParser_ReturnsSyntaxWithoutLowering()
    {
        var result = AwkParser.Parse("BEGIN{print 1} {print $1}");

        Assert.True(result.Success);
        Assert.True(result.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Parse, result.PhaseResult.Phase);
        Assert.Empty(result.Errors);
        Assert.Equal([AwkRuleKind.Begin, AwkRuleKind.Main], result.Syntax.Rules.Select(rule => rule.Kind).ToArray());
    }

    [Fact]
    public void AwkProgramLowerer_ConsumesSyntaxProgram()
    {
        const string source = "function join(a,b){ return a b }\nBEGIN{x=\"A\"; y=\"B\"; print join(x, y)}";
        var syntax = AwkSyntaxParser.Parse(source);

        var result = AwkProgramLowerer.Lower(syntax);

        Assert.Equal(source, syntax.SourceText);
        Assert.True(result.Success);
        Assert.True(result.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Lower, result.PhaseResult.Phase);
        Assert.Empty(result.Diagnostics);
        Assert.True(result.Program.HasProgram);
        Assert.Single(result.Program.Program.BeginActions);
        Assert.Single(result.Program.Program.Functions);
    }

    [Fact]
    public void AwkParser_ReturnsSyntaxErrors()
    {
        var result = AwkParser.Parse("BEGIN{print");

        Assert.False(result.Success);
        Assert.False(result.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Parse, result.PhaseResult.Phase);
        Assert.Contains(result.Errors, diagnostic => diagnostic.Message.Contains("Unterminated action block"));
    }

    [Fact]
    public void AwkBinder_ReturnsLoweringDiagnosticsWithoutExecutableProgram()
    {
        var result = AwkBinder.Bind(AwkParser.Parse("BEGIN{print \"x\" >}"));

        Assert.False(result.Success);
        Assert.False(result.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Lower, result.Phase);
        Assert.False(result.Program.HasProgram);
        Assert.True(result.CompatibilityParseFailed);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public async Task AwkParser_PreservesParseDiagnosticsIntoShellFacingMessage()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print"));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.Equal(AwkDiagnosticId.ParseSyntax, diagnostic.Id);
        Assert.True(diagnostic.Span.Start > 0);
        Assert.Equal("awk: unterminated action block.\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));

        var result = await AwkTestHarness.RunProgramAsync(
            "BEGIN{print",
            [],
            [],
            CancellationToken.None);

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.StdOut);
        Assert.Equal("awk: unterminated action block.\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkFailure(result, 2, expectNoStdOut: true);
    }

    [Fact]
    public async Task AwkParser_PreservesBinderDiagnosticsIntoShellFacingMessage()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{break}"));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: break used outside loop or switch\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));

        var result = await AwkTestHarness.RunProgramAsync(
            "BEGIN{break}",
            [],
            [],
            CancellationToken.None);

        Assert.Equal(2, result.ExitCode);
        Assert.Equal("awk: break used outside loop or switch\n", result.StdErr);
        AwkTestHarness.AssertFriendlyAwkFailure(result, 2, expectNoStdOut: true);
    }

    [Theory]
    [InlineData("BEGIN{getline 1}", "Invalid getline target.")]
    [InlineData("BEGIN{sub(/x/, \"y\", 1)}", "Argument 3 of `sub' must be an lvalue.")]
    [InlineData("BEGIN{split($0, 1)}", "Argument 2 of `split' must be an array name.")]
    public void AwkBinder_ReturnsPreciseLoweringDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, diagnostic.Message);
    }

    [Theory]
    [InlineData(@"[[:alpha:]]+", true)]
    [InlineData(@"\<word\>", true)]
    [InlineData(@"[[=a=]]", true)]
    [InlineData("[z-a]", false)]
    public void AwkRegexSyntaxValidator_ValidatesTranslatedFrontendPattern(string pattern, bool expected)
    {
        Assert.Equal(expected, AwkRegexSyntaxValidator.IsValidPattern(pattern));
    }

    private static string Decode(ReadOnlyMemory<byte> bytes) =>
        Encoding.UTF8.GetString(bytes.Span);
}
