namespace Lokad.Awk;

internal sealed class AwkInvocation
{
    public AwkInvocation(
        AwkCommandInvocation command,
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkInputArgument> inputArguments,
        AwkRuntimeArgumentPlan runtimeArguments,
        AwkExecutionOptions executionOptions,
        AwkTerminalInvocationResult terminalResult)
    {
        Sources = sources;
        InputArguments = inputArguments;
        RuntimeArgumentsPlan = runtimeArguments;
        Stdio = new(command.StdIn, command.StdOut, command.StdErr);
        ExecutionOptions = executionOptions;
        TerminalResult = terminalResult;
    }

    public IReadOnlyList<AwkSourceReference> Sources { get; }

    public IReadOnlyList<AwkInputArgument> InputArguments { get; }

    public AwkRuntimeArgumentPlan RuntimeArgumentsPlan { get; }

    public AwkStdioPlan Stdio { get; }

    public AwkExecutionOptions ExecutionOptions { get; }

    public AwkTerminalInvocationResult TerminalResult { get; }

    public static AwkInvocation ForTerminal(AwkCommandInvocation command, AwkTerminalInvocationResult result,
        bool sandbox = false, bool csv = false) =>
        new(command, [], [], new AwkRuntimeArgumentPlan([], [], [], command.CurrentDirectory),
            new AwkExecutionOptions(AwkCompatibilityProfile.Default, sandbox, csv), result);

    public IReadOnlyList<AwkDiagnostic> GetOptionWarnings(CancellationToken cancellationToken,
        AwkStringEscapeWarnings? stringEscapeWarnings = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var history = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        List<AwkDiagnostic>? warnings = null;
        if (ExecutionOptions.CompatibilityProfile.OverridesCharactersAsBytes)
        {
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
                    warnings ??= [];
                    warnings.Add(new AwkDiagnostic(default, warning));
                }, cancellationToken.ThrowIfCancellationRequested);
        }
        return warnings is null ? [] : warnings;
    }
}
