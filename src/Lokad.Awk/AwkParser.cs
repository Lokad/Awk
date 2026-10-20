using System.Collections.Generic;
using System.Threading;

namespace Lokad.Awk;

internal static class AwkParser
{
    public static AwkParseResult Parse(string source)
        => Parse(source, int.MaxValue);

    public static AwkParseResult Parse(string source, int maxSyntaxNestingDepth)
        => Parse(
            source,
            new AwkFrontendBudget(maxSyntaxNestingDepth),
            CancellationToken.None);

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
            return new AwkParseResult(AwkPhaseResult<AwkSyntaxProgram>.SuccessResult(
                AwkPipelinePhase.Parse,
                syntax,
                []));
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
        var phase = errors.Length == 0
            ? AwkPhaseResult<AwkSyntaxProgram>.SuccessResult(AwkPipelinePhase.Parse, syntax, warnings)
            : AwkPhaseResult<AwkSyntaxProgram>.Failure(AwkPipelinePhase.Parse, syntax, errors, warnings);
        return new AwkParseResult(phase);
    }
}

internal sealed class AwkParseResult(AwkPhaseResult<AwkSyntaxProgram> phaseResult)
{
    public AwkPhaseResult<AwkSyntaxProgram> PhaseResult { get; } = phaseResult;

    public AwkSyntaxProgram Syntax => PhaseResult.Value;

    public IReadOnlyList<AwkDiagnostic> Warnings => PhaseResult.Warnings;

    public IReadOnlyList<AwkDiagnostic> Errors => PhaseResult.Diagnostics;

    public bool Success => PhaseResult.Success;
}
