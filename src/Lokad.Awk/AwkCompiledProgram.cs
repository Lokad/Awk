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

    public Task<int> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken) =>
        AwkRuntime.ExecuteAsync(host, invocation, program, cancellationToken);

    internal Task<int> ExecuteAsync(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        AwkRuntime.ExecuteAsync(
            host,
            invocation,
            program,
            cancellationToken,
            runtimeOptions,
            runtimeOptions.ExecutionEngineKind == AwkExecutionEngineKind.Executable
                ? ExecutableProgram
                : null);

    internal AwkExecutableProgram ExecutableProgram { get; } = executableProgram;

    internal Task<int> ExecuteAsync(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkExecutableProgram executableProgram) =>
        AwkRuntime.ExecuteAsync(host, invocation, program, cancellationToken, runtimeOptions, executableProgram);
}
