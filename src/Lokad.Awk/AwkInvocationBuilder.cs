using System;

namespace Lokad.Awk;

internal sealed class AwkInvocationBuilder
{
    private readonly AwkFileDescriptor _stdIn;
    private readonly AwkFileDescriptor _stdOut;
    private readonly AwkFileDescriptor _stdErr;
    private AwkSourcePlan _sourcePlan = new([]);
    private AwkInputPlan _inputPlan = new([], []);
    private AwkRuntimeArgumentPlan _runtimeArgumentsPlan;
    private AwkTerminalInvocationResult _terminalResult = AwkTerminalInvocationResult.None;
    private AwkFieldSeparatorSpec _fieldSeparator = AwkFieldSeparatorSpec.Default;
    private AwkCompatibilityProfile _compatibilityProfile = AwkCompatibilityProfile.Default;

    public AwkInvocationBuilder(AwkCommandInvocation invocation)
    {
        _stdIn = invocation.StdIn;
        _stdOut = invocation.StdOut;
        _stdErr = invocation.StdErr;
        _runtimeArgumentsPlan = new AwkRuntimeArgumentPlan([], [], [], invocation.CurrentDirectory);
    }

    public AwkInvocationBuilder WithPlan(AwkInvocationPlan plan)
    {
        _sourcePlan = plan.Source;
        _inputPlan = plan.Input;
        _runtimeArgumentsPlan = plan.RuntimeArguments;
        return this;
    }

    public AwkInvocationBuilder WithFieldSeparator(AwkFieldSeparatorSpec fieldSeparator)
    {
        _fieldSeparator = fieldSeparator;
        return this;
    }

    public AwkInvocationBuilder WithCompatibilityProfile(AwkCompatibilityProfile compatibilityProfile)
    {
        _compatibilityProfile = compatibilityProfile;
        return this;
    }

    public AwkInvocationBuilder WithErrorMessage(ReadOnlyMemory<byte> errorMessage, AwkDiagnosticId errorId)
    {
        _terminalResult = AwkTerminalInvocationResult.Error(errorMessage, errorId);
        return this;
    }

    public AwkInvocationBuilder WithHelp()
    {
        _terminalResult = AwkTerminalInvocationResult.Help;
        return this;
    }

    public AwkInvocationBuilder WithVersion()
    {
        _terminalResult = AwkTerminalInvocationResult.Version;
        return this;
    }

    public AwkInvocation Build() =>
        new(
            _sourcePlan,
            _inputPlan,
            _runtimeArgumentsPlan,
            new AwkStdioPlan(_stdIn, _stdOut, _stdErr),
            new AwkExecutionOptions(_fieldSeparator, _compatibilityProfile),
            _terminalResult);

}
