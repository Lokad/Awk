namespace Lokad.Awk;

internal sealed class AwkExecutionControlRuntime
{
    private AwkOptionalExitCode _exitCode = AwkOptionalExitCode.None;

    public bool ExitRequested { get; private set; }

    public void RequestExit(AwkOptionalExitCode exitCode)
    {
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
