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
}
