using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkLoweringResult(
    AwkPhaseResult<AwkProgramResult> phaseResult)
{
    public AwkPhaseResult<AwkProgramResult> PhaseResult { get; } = phaseResult;

    public AwkProgramResult Program => PhaseResult.Value;

    public IReadOnlyList<AwkDiagnostic> Diagnostics => PhaseResult.Diagnostics;

    public bool Success => Program.HasProgram && PhaseResult.Success;

    public static AwkLoweringResult SuccessResult(AwkProgram program) =>
        new(AwkPhaseResult<AwkProgramResult>.SuccessResult(
            AwkPipelinePhase.Lower,
            AwkProgramResult.Available(program),
            []));

    public static AwkLoweringResult Failure(IReadOnlyList<AwkDiagnostic> diagnostics) =>
        new(AwkPhaseResult<AwkProgramResult>.Failure(
            AwkPipelinePhase.Lower,
            AwkProgramResult.None,
            diagnostics,
            []));

    public static AwkLoweringResult Failure(AwkSyntaxProgram syntax, string message) =>
        Failure([new AwkDiagnostic(ResolveFailureSpan(syntax), message, AwkDiagnosticId.BindInvalidProgram)]);

    private static AwkTextSpan ResolveFailureSpan(AwkSyntaxProgram syntax)
    {
        if (syntax.SourceText.Length == 0)
            return new AwkTextSpan(0, 0);

        return new AwkTextSpan(0, 1);
    }
}
