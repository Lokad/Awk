namespace Lokad.Awk;

internal static class AwkParser
{
    public static AwkParseResult Parse(
        string source,
        AwkFrontendBudget budget,
        CancellationToken cancellationToken,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault)
    {
        var syntax = AwkSyntaxParser.Parse(
            source,
            budget,
            cancellationToken,
            compatibilityMode);
        if (syntax.Diagnostics.Count == 0)
        {
            return new AwkParseResult(syntax, [], []);
        }

        var warningCount = 0;
        foreach (var diagnostic in syntax.Diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.IsWarning)
                warningCount++;
        }

        var warnings = new AwkDiagnostic[warningCount];
        var errors = new AwkDiagnostic[syntax.Diagnostics.Count - warningCount];
        var warningIndex = 0;
        var errorIndex = 0;
        foreach (var diagnostic in syntax.Diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.IsWarning)
                warnings[warningIndex++] = diagnostic;
            else
                errors[errorIndex++] = diagnostic;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new AwkParseResult(syntax, errors, warnings);
    }
}

internal sealed class AwkParseResult(AwkSyntaxProgram syntax,
    IReadOnlyList<AwkDiagnostic> errors, IReadOnlyList<AwkDiagnostic> warnings)
{
    public AwkSyntaxProgram Syntax { get; } = syntax;
    public IReadOnlyList<AwkDiagnostic> Errors { get; } = errors;
    public IReadOnlyList<AwkDiagnostic> Warnings { get; } = warnings;
    public bool Success => Errors.Count == 0;
}
