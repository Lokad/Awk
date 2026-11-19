namespace Lokad.Awk;

internal static class AwkBinder
{
    public static AwkBindingResult Bind(
        AwkParseResult parseResult,
        AwkCompatibilityProfile compatibilityProfile,
        Action? checkpoint = null,
        AwkRuntimeArgumentPlan? externalArguments = null, AwkFrontendMemory? memory = null)
    {
        checkpoint?.Invoke();
        if (!parseResult.Success)
            return AwkBindingResult.Failure(parseResult.Errors);

        var symbols = AwkSymbolCollector.Collect(parseResult.Syntax, compatibilityProfile.Mode, checkpoint);
        var semanticDiagnostics = AwkSemanticValidator.Validate(
            parseResult.Syntax,
            symbols,
            compatibilityProfile,
            checkpoint);
        if (semanticDiagnostics.Count > 0)
            return AwkBindingResult.Failure(semanticDiagnostics);

        var binding = AwkProgramLowerer.LowerValidated(parseResult.Syntax, symbols, compatibilityProfile.Mode, checkpoint,
            externalArguments, memory);
        if (binding.Success) checkpoint?.Invoke();
        return binding;
    }
}

internal sealed class AwkBindingResult
{
    private readonly AwkProgram? _program;

    private AwkBindingResult(AwkProgram? program, IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        _program = program;
        Diagnostics = diagnostics;
    }

    public bool Success => _program.HasValue;
    public AwkProgram Program => _program ?? throw new InvalidOperationException("No awk program is available.");
    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public static AwkBindingResult FromProgram(AwkProgram program) => new(program, []);
    public static AwkBindingResult Failure(IReadOnlyList<AwkDiagnostic> diagnostics) => new(null, diagnostics);
}
