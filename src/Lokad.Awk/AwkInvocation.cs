namespace Lokad.Awk;

internal sealed class AwkInvocation
{
    public AwkInvocation(
        AwkSourcePlan sourcePlan,
        AwkInputPlan inputPlan,
        AwkRuntimeArgumentPlan runtimeArguments,
        AwkStdioPlan stdio,
        AwkExecutionOptions executionOptions,
        AwkTerminalInvocationResult terminalResult)
    {
        SourcePlan = sourcePlan;
        InputPlan = inputPlan;
        RuntimeArgumentsPlan = runtimeArguments;
        Stdio = stdio;
        ExecutionOptions = executionOptions;
        TerminalResult = terminalResult;
    }

    public AwkSourcePlan SourcePlan { get; }

    public AwkInputPlan InputPlan { get; }

    public AwkRuntimeArgumentPlan RuntimeArgumentsPlan { get; }

    public AwkStdioPlan Stdio { get; }

    public AwkExecutionOptions ExecutionOptions { get; }

    public AwkTerminalInvocationResult TerminalResult { get; }

    public IReadOnlyList<AwkDiagnostic> GetOptionWarnings(int maxDiagnostics, CancellationToken cancellationToken,
        AwkStringEscapeWarnings? stringEscapeWarnings = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var history = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        List<AwkDiagnostic>? warnings = null;
        if (ExecutionOptions.CompatibilityProfile.OverridesCharactersAsBytes)
        {
            if (maxDiagnostics <= 0)
                throw new AwkRuntimeException(AwkMessages.ErrorFrontendDiagnosticLimit,
                    AwkDiagnosticId.RuntimeResourceLimit);
            warnings = [new AwkDiagnostic(default, AwkMessages.WarningPosixOverridesCharactersAsBytes,
                AwkDiagnosticId.WarningRuntime)];
        }
        foreach (var assignment in RuntimeArgumentsPlan.Variables)
        {
            if (assignment.Source == AwkVariableAssignmentSource.InputArgument ||
                !assignment.RawValue.Contains('\\'))
                continue;
            AwkEscapes.ReportStringWarnings(assignment.RawValue, ExecutionOptions.CompatibilityProfile.Mode,
                history, warning =>
                {
                    if ((warnings?.Count ?? 0) >= maxDiagnostics)
                        throw new AwkRuntimeException(AwkMessages.ErrorFrontendDiagnosticLimit,
                            AwkDiagnosticId.RuntimeResourceLimit);
                    warnings ??= [];
                    warnings.Add(new AwkDiagnostic(default, warning));
                }, cancellationToken.ThrowIfCancellationRequested);
        }
        return warnings is null ? [] : warnings;
    }
}
