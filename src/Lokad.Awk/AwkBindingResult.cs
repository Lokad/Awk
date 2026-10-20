using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkBindingResult(
    AwkPipelinePhase phase,
    IReadOnlyList<AwkDiagnostic> diagnostics,
    IReadOnlyList<AwkSymbolReference> symbols,
    AwkSemanticModel semanticModel,
    AwkProgramResult program,
    bool compatibilityParseFailed)
{
    public AwkPhaseResult<AwkProgramResult> PhaseResult { get; } =
        diagnostics.Count == 0
            ? AwkPhaseResult<AwkProgramResult>.SuccessResult(phase, program, [])
            : AwkPhaseResult<AwkProgramResult>.Failure(phase, program, diagnostics, []);

    public AwkPipelinePhase Phase => PhaseResult.Phase;

    public IReadOnlyList<AwkDiagnostic> Diagnostics => PhaseResult.Diagnostics;

    public IReadOnlyList<AwkSymbolReference> Symbols { get; } = symbols;

    public AwkSemanticModel SemanticModel { get; } = semanticModel;

    public AwkProgramResult Program => PhaseResult.Value;

    public bool CompatibilityParseFailed { get; } = compatibilityParseFailed;

    public bool Success => Program.HasProgram && PhaseResult.Success;
}
