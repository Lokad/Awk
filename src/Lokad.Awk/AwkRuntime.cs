using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkRuntime
{
    public static Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            host,
            invocation,
            program,
            cancellationToken,
            AwkRuntimeOptions.CreateSystemDefault());

    public static Task<AwkExitCode> ExecuteAsync(
        IAwkHost host, AwkInvocation invocation, AwkProgram program,
        CancellationToken cancellationToken, AwkRuntimeOptions runtimeOptions) =>
        ExecuteAsync(host, invocation, program, cancellationToken, runtimeOptions,
            new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters, cancellationToken));

    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkRuntimeResourceMonitor resources,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        var executionOptions = invocation.ExecutionOptions;
        var runtimeArguments = invocation.RuntimeArgumentsPlan;
        var stdio = invocation.Stdio;
        var initialSeparator = executionOptions.FieldSeparator;
        foreach (var assignment in runtimeArguments.Variables)
            if (assignment.Source == AwkVariableAssignmentSource.FieldSeparatorOption)
            {
                initialSeparator = AwkFieldSeparatorSpec.Default;
                break;
            }
        using var state = new AwkRuntimeState(
            initialSeparator,
            cancellationToken,
            runtimeOptions,
            resources,
            executionOptions.CompatibilityProfile,
            stringEscapeWarnings, regexEscapeWarnings);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
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
            runtimeOptions.Counters,
            runtimeOptions.Limits,
            state,
            state.SnapshotEnvironment,
            executionOptions.Sandbox,
            runtimeOptions.AllowFileRedirections,
            runtimeOptions.AllowCommandPipes,
            runtimeOptions.AccessiblePathRoot,
            runtimeOptions.ForwardCommandEnvironment,
            runtimeOptions.ExposeHostErrorDetails,
            runtimeOptions.MaxHostErrorBytes);
        using var actionExecutor = new AwkRuntimeActionExecutor(program, state);
        var inputScheduler = new AwkInputScheduler(
            host,
            invocation,
            actionExecutor,
            state,
            io,
            cancellationToken,
            runtimeOptions.AccessiblePathRoot,
            runtimeOptions.AllowDynamicMainInputPaths,
            runtimeOptions.ExposeHostErrorDetails,
            runtimeOptions.MaxHostErrorBytes);
        io.BindMainInputReader(inputScheduler);
        using var execution = state.PushExecutionContext(actionExecutor.Output, io);
        try
        {
            foreach (var variable in runtimeArguments.Variables)
                state.AssignVariableFromExternalText(variable);
            await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
            var beginFlow = await actionExecutor.ExecuteBeginActionsAsync(state, io).ConfigureAwait(false);
            if (beginFlow != AwkStatementFlow.ExitProgram)
                await inputScheduler.ProcessRemainingInputAsync().ConfigureAwait(false);

            await actionExecutor.ExecuteEndActionsAsync(state, io).ConfigureAwait(false);
            await inputScheduler.CloseRemainingDescriptorsAsync().ConfigureAwait(false);
            await io.CloseAsync().ConfigureAwait(false);
        }
        catch (AwkRuntimeException exception)
        {
            try
            {
                await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
            }
            catch (AwkRuntimeException warningFailure)
            {
                exception = warningFailure;
            }
            await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false);
            var failure = AwkDiagnostics.Runtime(exception);
            runtimeOptions.Counters.RecordHostCall();
            runtimeOptions.Counters.RecordOutputBytes(failure.Message.Length);
            var appendStatus = await AwkHostBoundaryPolicy.AwaitAsync(
                    host.AppendAsync(stdio.StdErr, failure.Message, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            return appendStatus.Succeeded
                ? failure.ExitCode
                : AwkDiagnostics.FatalExitCode;
        }
        catch
        {
            await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false);
            throw;
        }

        if (state.TryGetExitCode(out var exitCode))
            return exitCode;

        return AwkDiagnostics.SuccessExitCode;

        static async Task CleanupAfterFailureAsync(AwkInputScheduler inputScheduler, AwkIo io)
        {
            await inputScheduler.CloseRemainingDescriptorsForCleanupAsync().ConfigureAwait(false);
            await io.CloseForCleanupAsync().ConfigureAwait(false);
        }
    }

}
