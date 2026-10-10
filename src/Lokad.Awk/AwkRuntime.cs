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
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        ExecuteWithNewResourceMonitorAsync(
            host,
            invocation,
            program,
            cancellationToken,
            runtimeOptions,
            executableProgram: null);

    internal static Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram? executableProgram) =>
        ExecuteWithNewResourceMonitorAsync(
            host,
            invocation,
            program,
            cancellationToken,
            runtimeOptions,
            executableProgram);

    private static Task<AwkExitCode> ExecuteWithNewResourceMonitorAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram? executableProgram) =>
        ExecuteAsync(
            host,
            invocation,
            program,
            cancellationToken,
            runtimeOptions,
            executableProgram,
            new AwkRuntimeResourceMonitor(runtimeOptions.Limits, runtimeOptions.Counters, cancellationToken));

    internal static async Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram? executableProgram,
        AwkRuntimeResourceMonitor resources)
    {
        if (executableProgram == null &&
            runtimeOptions.ExecutionEngineKind == AwkExecutionEngineKind.Executable)
        {
            throw new InvalidOperationException("Executable execution requires a compiled executable program.");
        }

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
        var state = new AwkRuntimeState(
            initialSeparator,
            cancellationToken,
            runtimeOptions,
            resources,
            executionOptions.CompatibilityProfile);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeArguments(runtimeArguments.RuntimeArguments);
        state.InitializeEnvironment(
            runtimeOptions.ExposeEnvironment
                ? runtimeArguments.Environment
                : Array.Empty<AwkEnvironmentVariable>());
        state.InitializeProcInfo();
        if (executableProgram == null)
            state.InitializeFunctions(program.Functions);
        else
            state.InitializeFunctions(executableProgram.Functions);
        var cleanupDeadline = new AwkCleanupDeadline(runtimeOptions.CleanupTimeout);
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
            runtimeOptions.CommandTimeout,
            runtimeOptions.CleanupTimeout,
            runtimeOptions.ForwardCommandEnvironment,
            runtimeOptions.HostOperationTimeout,
            runtimeOptions.ExposeHostErrorDetails,
            runtimeOptions.MaxHostErrorBytes,
            cleanupDeadline);
        using var actionExecutor = new AwkRuntimeActionExecutor(program, executableProgram, state);
        var inputScheduler = new AwkInputScheduler(
            host,
            invocation,
            actionExecutor,
            state,
            io,
            cancellationToken,
            runtimeOptions.AccessiblePathRoot,
            runtimeOptions.CleanupTimeout,
            runtimeOptions.AllowDynamicMainInputPaths,
            runtimeOptions.HostOperationTimeout,
            runtimeOptions.ExposeHostErrorDetails,
            runtimeOptions.MaxHostErrorBytes,
            cleanupDeadline);
        io.BindMainInputReader(inputScheduler);
        using var execution = state.PushExecutionContext(actionExecutor.Output, io);
        try
        {
            foreach (var variable in runtimeArguments.Variables)
                state.AssignVariableFromExternalText(variable);
            await actionExecutor.Output.FlushDiagnosticsAsync(io).ConfigureAwait(false);
            if (executionOptions.CompatibilityProfile.OverridesCharactersAsBytes)
                await io.WriteStandardErrorAsync(AwkMessages.WarningPosixOverridesCharactersAsBytes)
                    .ConfigureAwait(false);

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
                    runtimeOptions.HostOperationTimeout,
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
