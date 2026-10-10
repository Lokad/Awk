namespace Lokad.Awk;

internal static class AwkBinder
{
    public static AwkBindingResult Bind(AwkParseResult parseResult) =>
        Bind(parseResult, AwkCompatibilityProfile.Default);

    public static AwkBindingResult Bind(
        AwkParseResult parseResult,
        AwkCompatibilityProfile compatibilityProfile)
    {
        var symbols = AwkSymbolCollector.Collect(parseResult.Syntax, out var globalNames, compatibilityProfile.Mode);
        var semanticModel = AwkSemanticModel.Create(symbols);
        if (parseResult.Errors.Count > 0)
            return new AwkBindingResult(
                parseResult.PhaseResult.Phase,
                parseResult.Syntax,
                parseResult.Errors,
                symbols,
                semanticModel,
                AwkProgramResult.None,
                compatibilityParseFailed: false);

        var semanticDiagnostics = AwkSemanticValidator.Validate(
            parseResult.Syntax,
            semanticModel,
            compatibilityProfile);
        if (semanticDiagnostics.Count > 0)
            return new AwkBindingResult(
                AwkPipelinePhase.Bind,
                parseResult.Syntax,
                semanticDiagnostics,
                symbols,
                semanticModel,
                AwkProgramResult.None,
                compatibilityParseFailed: true);

        var lowering = AwkProgramLowerer.Lower(parseResult.Syntax, globalNames, compatibilityProfile.Mode);
        if (lowering.Diagnostics.Count > 0)
            return new AwkBindingResult(
                lowering.PhaseResult.Phase,
                parseResult.Syntax,
                lowering.Diagnostics,
                symbols,
                semanticModel,
                AwkProgramResult.None,
                compatibilityParseFailed: true);

        return new AwkBindingResult(
            lowering.PhaseResult.Phase,
            parseResult.Syntax,
            [],
            symbols,
            semanticModel,
            lowering.Program,
            compatibilityParseFailed: false);
    }
}
