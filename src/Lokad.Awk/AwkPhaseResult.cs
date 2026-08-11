using System.Collections.Generic;

namespace Lokad.Awk;

internal enum AwkPipelinePhase
{
    Parse,
    Bind,
    Lower
}

internal sealed class AwkPhaseResult<T>(
    AwkPipelinePhase phase,
    T value,
    IReadOnlyList<AwkDiagnostic> diagnostics,
    IReadOnlyList<AwkDiagnostic> warnings)
{
    public AwkPipelinePhase Phase { get; } = phase;

    public T Value { get; } = value;

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; } = diagnostics;

    public IReadOnlyList<AwkDiagnostic> Warnings { get; } = warnings;

    public bool Success => Diagnostics.Count == 0;

    public static AwkPhaseResult<T> SuccessResult(
        AwkPipelinePhase phase,
        T value,
        IReadOnlyList<AwkDiagnostic> warnings) =>
        new(phase, value, [], warnings);

    public static AwkPhaseResult<T> Failure(
        AwkPipelinePhase phase,
        T value,
        IReadOnlyList<AwkDiagnostic> diagnostics,
        IReadOnlyList<AwkDiagnostic> warnings) =>
        new(phase, value, diagnostics, warnings);
}
