using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkParser
{
    public static AwkParseResult Parse(string source)
    {
        var syntax = AwkSyntaxParser.Parse(source);
        var warnings = new List<AwkDiagnostic>();
        var errors = new List<AwkDiagnostic>();
        foreach (var diagnostic in syntax.Diagnostics)
        {
            (diagnostic.IsWarning ? warnings : errors).Add(diagnostic);
        }

        var phase = errors.Count == 0
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
