using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkRuntime
{
    public static async Task<int> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(
                host,
                invocation,
                program,
                cancellationToken,
                AwkRuntimeOptions.CreateSystemDefault())
            .ConfigureAwait(false);

    public static async Task<int> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        await ExecuteAsync(
                host,
                invocation,
                program,
                cancellationToken,
                runtimeOptions,
                executableProgram: null)
            .ConfigureAwait(false);

    internal static async Task<int> ExecuteAsync(
        IAwkHost host,
        AwkInvocation invocation,
        AwkProgram program,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram? executableProgram)
    {
        if (executableProgram == null &&
            runtimeOptions.ExecutionEngineKind == AwkExecutionEngineKind.Executable)
        {
            throw new InvalidOperationException("Executable execution requires a compiled executable program.");
        }

        var executionOptions = invocation.ExecutionOptions;
        var runtimeArguments = invocation.RuntimeArgumentsPlan;
        var stdio = invocation.Stdio;
        var state = new AwkRuntimeState(AwkRuntimeStatePlan.Create(
            executionOptions.FieldSeparator,
            cancellationToken,
            runtimeOptions,
            executionOptions.CompatibilityProfile.Mode,
            executionOptions.CompatibilityProfile.LintMode,
            executionOptions.CompatibilityProfile.CharactersAsBytes,
            executionOptions.CompatibilityProfile.NonDecimalData,
            runtimeOptions.Limits.MaxArrayElements));
        state.InitializeArguments(runtimeArguments.RuntimeArguments);
        state.InitializeEnvironment(runtimeArguments.Environment);
        state.InitializeProcInfo();
        if (executableProgram == null)
            state.InitializeFunctions(program.Functions);
        else
            state.InitializeFunctions(executableProgram.Functions);
        foreach (var variable in runtimeArguments.Variables)
        {
            state.AssignVariableFromExternalText(variable);
        }

        var io = new AwkIo(
            host,
            runtimeArguments.CurrentDirectory,
            stdio.StdIn,
            stdio.StdOut,
            stdio.StdErr,
            cancellationToken,
            runtimeOptions.Counters,
            runtimeOptions.Limits,
            state.SnapshotEnvironment);
        var actionExecutor = new AwkRuntimeActionExecutor(program, executableProgram);
        var inputScheduler = new AwkInputScheduler(host, invocation, actionExecutor, state, io, cancellationToken);
        try
        {
            var inputPreparation = await inputScheduler.TryPrepareFirstMainInputAsync().ConfigureAwait(false);
            var beginFlow = await actionExecutor.ExecuteBeginActionsAsync(state, io).ConfigureAwait(false);

            if (beginFlow != AwkStatementFlow.ExitProgram && !inputScheduler.HasExplicitInputSource)
            {
                await inputScheduler.ProcessDefaultInputAsync(inputPreparation).ConfigureAwait(false);
            }
            else if (beginFlow != AwkStatementFlow.ExitProgram)
            {
                await inputScheduler.ProcessExplicitInputsAsync(inputPreparation).ConfigureAwait(false);
            }

            await actionExecutor.ExecuteEndActionsAsync(state, io).ConfigureAwait(false);
            await inputScheduler.CloseRemainingDescriptorsAsync().ConfigureAwait(false);
            await io.CloseAsync().ConfigureAwait(false);
        }
        catch (AwkRuntimeException exception)
        {
            await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false);
            var failure = AwkDiagnostics.Runtime(exception);
            runtimeOptions.Counters.RecordHostCall();
            runtimeOptions.Counters.RecordOutputBytes(failure.Message.Length);
            var appendStatus = await host.AppendAsync(stdio.StdErr, failure.Message, cancellationToken)
                .ConfigureAwait(false);
            return appendStatus.Succeeded
                ? failure.ExitCode.Value
                : AwkDiagnostics.FatalExitCode.Value;
        }
        catch
        {
            await CleanupAfterFailureAsync(inputScheduler, io).ConfigureAwait(false);
            throw;
        }

        if (state.TryGetExitCode(out var exitCode))
            return exitCode.Value;

        if (state.ExitRequested)
            return AwkDiagnostics.SuccessExitCode.Value;

        return AwkDiagnostics.SuccessExitCode.Value;
    }

    private static async Task CleanupAfterFailureAsync(AwkInputScheduler inputScheduler, AwkIo io)
    {
        await inputScheduler.CloseRemainingDescriptorsForCleanupAsync().ConfigureAwait(false);
        await io.CloseForCleanupAsync().ConfigureAwait(false);
    }

}
