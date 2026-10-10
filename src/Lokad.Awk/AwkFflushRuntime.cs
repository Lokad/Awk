namespace Lokad.Awk;

internal static class AwkFflushRuntime
{
    internal const string NonStringWarning = "awk: warning: fflush: received non-string argument";
    internal const string NonStringFatal = "awk: fatal: fflush: received non-string argument\n";
    private const string UnopenedPrefix = "awk: warning: fflush: `";
    private const string UnopenedSuffix = "' is not an open file, pipe or co-process";
    private const string InputFilePrefix = "awk: warning: fflush: cannot flush: file `";
    private const string InputPipePrefix = "awk: warning: fflush: cannot flush: pipe `";
    private const string InputSuffix = "' opened for reading, not writing";

    public static async ValueTask<int> FlushAllAsync(AwkExecutionContext execution)
    {
        await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        await execution.Io.FlushAllAsync().ConfigureAwait(false);
        return 0;
    }

    public static async ValueTask<int> FlushAsync(
        AwkValue value, AwkRuntimeState state, AwkExecutionContext execution)
    {
        await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        if (state.LintMode != AwkLintMode.Off && (value.IsNumberValue || value.IsNumericStringValue))
        {
            if (state.LintMode == AwkLintMode.Fatal)
                throw new AwkRuntimeException(AwkUtf8Codec.Encode(NonStringFatal), AwkDiagnosticId.WarningRuntime);
            execution.Output.ReportDiagnostic(new AwkDiagnostic(
                default, NonStringWarning, AwkDiagnosticId.WarningRuntime));
            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        }

        // Redirection names cross the counted host-path boundary without consuming UNTYPE cells.
        var path = state.FormatStringByteText(value).AsString(state.Counters);
        var status = path.Length == 0
            ? await execution.Io.FlushAllAsync().ConfigureAwait(false)
            : await execution.Io.FlushPathAsync(path).ConfigureAwait(false);
        if (status == AwkFlushStatus.Success)
            return 0;

        var prefix = status switch
        {
            AwkFlushStatus.Unopened => UnopenedPrefix,
            AwkFlushStatus.InputFile => InputFilePrefix,
            AwkFlushStatus.InputPipe => InputPipePrefix,
            _ => throw new InvalidOperationException("Unexpected fflush status.")
        };
        var suffix = status == AwkFlushStatus.Unopened ? UnopenedSuffix : InputSuffix;
        var characters = AwkRuntimeMemory.CheckedAdd(path.Length, prefix.Length + suffix.Length);
        if (characters > int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        state.MemoryGovernor.PreflightAllocation(AwkRuntimeMemory.EstimateDiagnosticMessageBytes((int)characters));
        execution.Output.ReportDiagnostic(new AwkDiagnostic(
            default, string.Concat(prefix, path, suffix), AwkDiagnosticId.WarningRuntime));
        await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        // These warnings are not IO failures and do not assign ERRNO.
        return -1;
    }
}
