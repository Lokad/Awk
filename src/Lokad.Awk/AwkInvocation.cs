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

    public IReadOnlyList<AwkDiagnostic> GetOptionUnicodeWarnings(int maxDiagnostics, CancellationToken cancellationToken)
    {
        if (ExecutionOptions.CompatibilityProfile.Mode == AwkCompatibilityMode.Posix)
            return [];
        List<AwkDiagnostic>? warnings = null;
        foreach (var assignment in RuntimeArgumentsPlan.Variables)
        {
            if (assignment.Source == AwkVariableAssignmentSource.InputArgument ||
                !assignment.RawValue.Contains("\\u", StringComparison.Ordinal))
                continue;
            AwkEscapes.ReportUnicodeWarnings(assignment.RawValue, ExecutionOptions.CompatibilityProfile.Mode,
                message =>
                {
                    if ((warnings?.Count ?? 0) >= maxDiagnostics)
                        throw new AwkRuntimeException(AwkMessages.ErrorFrontendDiagnosticLimit,
                            AwkDiagnosticId.RuntimeResourceLimit);
                    warnings ??= [];
                    warnings.Add(new AwkDiagnostic(default, message, AwkDiagnosticId.WarningStringEscape));
                }, cancellationToken.ThrowIfCancellationRequested);
        }
        return warnings is null ? [] : warnings;
    }
}
