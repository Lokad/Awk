namespace Lokad.Awk;

internal sealed class AwkExecutionControlRuntime
{
    private AwkOptionalExitCode _exitCode = AwkOptionalExitCode.None;

    public bool ExitRequested { get; private set; }

    internal AwkActionPhase CurrentPhase { get; private set; } = AwkActionPhase.None;

    internal AwkActionPhaseScope EnterPhase(AwkActionPhase phase)
    {
        var previous = CurrentPhase;
        CurrentPhase = phase;
        return new(this, previous);
    }

    internal void RestorePhase(AwkActionPhase phase) => CurrentPhase = phase;

    internal void ValidateFunctionFlow(AwkStatementFlow flow)
    {
        if (flow == AwkStatementFlow.NextRecord && CurrentPhase != AwkActionPhase.Main)
            throw new AwkRuntimeException(AwkMessages.ErrorNextOutsideInput);
        if (flow == AwkStatementFlow.NextFile && CurrentPhase is not (AwkActionPhase.Main or AwkActionPhase.BeginFile))
            throw new AwkRuntimeException(AwkMessages.ErrorNextFileOutsideInput);
    }

    public void RequestExit(AwkOptionalExitCode exitCode)
    {
        if (CurrentPhase == AwkActionPhase.SortComparison)
            throw new AwkRuntimeException(AwkMessages.ErrorExitInsideSortComparison);
        ExitRequested = true;
        if (exitCode.HasValue)
            _exitCode = exitCode;
    }

    public bool TryGetExitCode(out AwkExitCode exitCode)
    {
        exitCode = _exitCode.Value;
        return _exitCode.HasValue;
    }
}

internal readonly struct AwkActionPhaseScope(
    AwkExecutionControlRuntime? control, AwkActionPhase previous) : System.IDisposable
{
    public void Dispose() => control?.RestorePhase(previous);
}
