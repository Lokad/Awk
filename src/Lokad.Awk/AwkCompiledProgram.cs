using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkCompiledProgram(
    AwkInvocation invocation,
    AwkProgram program,
    AwkExecutableProgram executableProgram,
    IReadOnlyList<AwkDiagnostic> warnings)
{
    public IReadOnlyList<AwkDiagnostic> Warnings { get; } = warnings;

    public Task<AwkExitCode> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken) =>
        ExecuteAsync(host, cancellationToken, AwkRuntimeOptions.CreateSystemDefault());

    internal Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        ExecuteAsync(
            host,
            cancellationToken,
            runtimeOptions,
            runtimeOptions.ExecutionEngineKind == AwkExecutionEngineKind.Executable
                ? ExecutableProgram
                : null);

    internal AwkExecutableProgram ExecutableProgram { get; } = executableProgram;

    internal Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram? executableProgram) =>
        AwkExecutor.ExecuteCompiledAsync(
            host, invocation, program, Warnings, cancellationToken, runtimeOptions, executableProgram);
}
