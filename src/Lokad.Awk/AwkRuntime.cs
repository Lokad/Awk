namespace Lokad.Awk;

internal static class AwkRuntime
{

    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        var executionOptions = invocation.ExecutionOptions;
        var runtimeArguments = invocation.RuntimeArgumentsPlan;
        var stdio = invocation.Stdio;
        var unobservedHeaders = program.ResolveUnobservedRecordHeaders(runtimeArguments, cancellationToken);
        using var state = new AwkRuntimeState(
            AwkFieldSeparatorSpec.Default,
            cancellationToken,
            runtimeOptions,
            executionOptions.CompatibilityProfile,
            stringEscapeWarnings, regexEscapeWarnings, executionOptions.Csv, unobservedHeaders);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeRangePatterns(program.RangeCount);
        state.InitializeArguments(runtimeArguments.RuntimeArguments);
        state.InitializeEnvironment(
            runtimeOptions.ExposeEnvironment
                ? runtimeArguments.Environment
                : Array.Empty<AwkEnvironmentVariable>());
        state.InitializeProcInfo();
        state.InitializeFunctions(program.Functions);

        var io = new AwkIo(
            host,
            runtimeArguments.CurrentDirectory,
            stdio.StdIn,
            stdio.StdOut,
            stdio.StdErr,
            cancellationToken,
            runtimeOptions.Limits,
            state,
            state.SnapshotEnvironment,
            executionOptions.Sandbox,
            runtimeOptions.AllowFileRedirections,
            runtimeOptions.AllowCommandPipes,
            runtimeOptions.AccessiblePathRoot,
            runtimeOptions.ForwardCommandEnvironment);
        using var actionExecutor = new AwkRuntimeActionExecutor(program, state);
        var inputScheduler = new AwkInputScheduler(
            invocation,
            actionExecutor,
            state,
            io,
            cancellationToken,
            runtimeOptions.AccessiblePathRoot,
            runtimeOptions.AllowDynamicMainInputPaths);
        io.BindMainInputReader(inputScheduler);
        using var execution = state.PushExecutionContext(actionExecutor.Output, io);
        try
        {
            foreach (var variable in runtimeArguments.Variables)
                state.AssignVariableFromExternalText(variable);
            await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
            var beginFlow = await actionExecutor.ExecuteBeginActionsAsync(state, io).ConfigureAwait(false);
            if (beginFlow != AwkStatementFlow.ExitProgram &&
                (program.HasMainRules || program.EndActions.Length > 0 ||
                 program.BeginFileActions.Length > 0 || program.EndFileActions.Length > 0))
                await inputScheduler.ProcessRemainingInputAsync().ConfigureAwait(false);

            await actionExecutor.ExecuteEndActionsAsync(state, io).ConfigureAwait(false);
            await inputScheduler.CloseRemainingDescriptorsAsync().ConfigureAwait(false);
            await io.CloseAsync().ConfigureAwait(false);
        }
        catch (AwkRuntimeException exception)
        {
            io.CancelCommandsForCleanup();
            var canReport = exception is not AwkHostAppendException append || append.Descriptor != stdio.StdErr;
            try
            {
                try
                {
                    if (canReport)
                        await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
                }
                catch (AwkHostAppendException appendFailure) when (appendFailure.Descriptor == stdio.StdErr)
                { canReport = false; }
                catch (AwkRuntimeException) { /* Preserve the failure that triggered cleanup. */ }
            }
            finally { await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false); }
            if (!canReport)
                return AwkExitCode.FatalError;
            _ = await AwkHostBoundaryPolicy.AppendResultAsync(host, stdio.StdErr, exception.MessageBytes,
                runtimeOptions.Counters, cancellationToken).ConfigureAwait(false);
            return AwkExitCode.FatalError;
        }
        catch
        {
            io.CancelCommandsForCleanup();
            await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false);
            throw;
        }

        if (state.TryGetExitCode(out var exitCode))
            return exitCode;

        return AwkExitCode.Success;

        static async Task CleanupAfterFailureAsync(AwkInputScheduler inputScheduler, AwkIo io)
        {
            await inputScheduler.CloseRemainingDescriptorsForCleanupAsync().ConfigureAwait(false);
            await io.CloseForCleanupAsync().ConfigureAwait(false);
        }
    }

}
