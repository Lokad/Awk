namespace Lokad.Awk;

internal static class AwkBinder
{
    public static AwkPhaseResult<AwkProgramResult> Bind(
        AwkParseResult parseResult,
        AwkCompatibilityProfile compatibilityProfile,
        Action? checkpoint = null,
        AwkRuntimeArgumentPlan? externalArguments = null)
    {
        checkpoint?.Invoke();
        if (!parseResult.Success)
            return AwkPhaseResult<AwkProgramResult>.Failure(
                AwkPipelinePhase.Parse, AwkProgramResult.None, parseResult.Errors, []);

        var symbols = AwkSymbolCollector.Collect(parseResult.Syntax, out var globalNames, compatibilityProfile.Mode, checkpoint);
        var semanticModel = AwkSemanticModel.Create(symbols, checkpoint);
        var semanticDiagnostics = AwkSemanticValidator.Validate(
            parseResult.Syntax,
            semanticModel,
            compatibilityProfile,
            checkpoint);
        if (semanticDiagnostics.Count > 0)
            return AwkPhaseResult<AwkProgramResult>.Failure(
                AwkPipelinePhase.Bind, AwkProgramResult.None, semanticDiagnostics, []);

        var lowering = AwkProgramLowerer.Lower(parseResult.Syntax, globalNames, compatibilityProfile.Mode, checkpoint,
            symbols, externalArguments);
        if (lowering.PhaseResult.Success) checkpoint?.Invoke();
        return lowering.PhaseResult;
    }
}
