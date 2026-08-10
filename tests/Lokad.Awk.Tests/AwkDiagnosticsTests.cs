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
            "awk: warning: escape sequence.",
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
    public void AwkDiagnostics_ClassifiesShellFacingFailures()
    {
        AssertFailure(
            AwkDiagnostics.Invocation(
                ToolAwkMessages.FormatInvalidOption("bad"),
                AwkDiagnosticId.OptionInvalid),
            AwkDiagnosticId.OptionInvalid,
            AwkDiagnosticCategory.Option);
        AssertFailure(
            AwkDiagnostics.Invocation(
                ToolAwkMessages.ErrorMissingScript,
                AwkDiagnosticId.OptionMissingScript),
            AwkDiagnosticId.OptionMissingScript,
            AwkDiagnosticCategory.Option);
        AssertFailure(
            AwkDiagnostics.Invocation(
                ToolAwkMessages.FormatUnsupportedGnuOption("--load"),
                AwkDiagnosticId.UnsupportedPolicy),
            AwkDiagnosticId.UnsupportedPolicy,
            AwkDiagnosticCategory.UnsupportedPolicy);
        AssertFailure(
            AwkDiagnostics.SourceLoad(
                ToolAwkMessages.FormatPathError(MissingScriptPath),
                AwkDiagnosticId.SourceLoadFailure),
            AwkDiagnosticId.SourceLoadFailure,
            AwkDiagnosticCategory.Source);
        AssertFailure(
            AwkDiagnostics.SourceLoad(
                ToolAwkMessages.ErrorUnsupportedNamespace,
                AwkDiagnosticId.UnsupportedPolicy),
            AwkDiagnosticId.UnsupportedPolicy,
            AwkDiagnosticCategory.UnsupportedPolicy);
        AssertFailure(
            AwkDiagnostics.Parse([new AwkDiagnostic(new AwkTextSpan(0, 1), "Invalid regex.", AwkDiagnosticId.ParseInvalidRegex)]),
            AwkDiagnosticId.ParseInvalidRegex,
            AwkDiagnosticCategory.Parse);
        AssertFailure(
            AwkDiagnostics.Parse([new AwkDiagnostic(new AwkTextSpan(0, 1), "Invalid action block.", AwkDiagnosticId.BindInvalidProgram)]),
            AwkDiagnosticId.BindInvalidProgram,
            AwkDiagnosticCategory.Bind);
        Assert.Equal(
            "awk: invalid action block.\n",
            Decode(AwkDiagnostics.Parse(
                [new AwkDiagnostic(new AwkTextSpan(0, 1), "Invalid action block.", AwkDiagnosticId.BindInvalidProgram)])
                .Message));
        AssertFailure(
            AwkDiagnostics.Runtime(new AwkRuntimeException(ToolAwkMessages.ErrorDivisionByZero)),
            AwkDiagnosticId.RuntimeError,
            AwkDiagnosticCategory.Runtime);
        AssertFailure(
            AwkDiagnostics.Runtime(new AwkRuntimeException(
                ToolAwkMessages.FormatPathError(OutputPath),
                AwkDiagnosticId.HostIoFailure)),
            AwkDiagnosticId.HostIoFailure,
            AwkDiagnosticCategory.HostIo);
        AssertFailure(
            AwkDiagnostics.Runtime(new AwkRuntimeException(
                ToolAwkMessages.ErrorUnsupportedSystem,
                AwkDiagnosticId.UnsupportedPolicy)),
            AwkDiagnosticId.UnsupportedPolicy,
            AwkDiagnosticCategory.UnsupportedPolicy);
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

    private static void AssertFailure(
        AwkFailureDiagnostic failure,
        AwkDiagnosticId expectedId,
        AwkDiagnosticCategory expectedCategory)
    {
        Assert.Equal(AwkDiagnostics.FatalExitCode, failure.ExitCode);
        Assert.Equal(expectedId, failure.Id);
        Assert.Equal(expectedCategory, failure.Category);
    }

    private static string Decode(ReadOnlyMemory<byte> bytes) =>
        Encoding.UTF8.GetString(bytes.Span);

    private static readonly byte[] MissingScriptPath =
    [
        (byte)'/',
        (byte)'m',
        (byte)'i',
        (byte)'s',
        (byte)'s',
        (byte)'i',
        (byte)'n',
        (byte)'g',
        (byte)'.',
        (byte)'a',
        (byte)'w',
        (byte)'k'
    ];

    private static readonly byte[] OutputPath =
    [
        (byte)'/',
        (byte)'o',
        (byte)'u',
        (byte)'t',
        (byte)'.',
        (byte)'t',
        (byte)'x',
        (byte)'t'
    ];
}
