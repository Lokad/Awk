using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkCompiledProgram(
    AwkInvocation invocation,
    AwkProgram program,
    IReadOnlyList<AwkDiagnostic> warnings,
    AwkExecutionMemoryOwner owner) : IAsyncDisposable
{
    private readonly object _lifetime = new();
    private TaskCompletionSource? _disposeCompletion;
    private AwkInvocation? _invocation = invocation;
    private AwkProgram _program = program;
    private IReadOnlyList<AwkDiagnostic> _warnings = warnings;

    public IReadOnlyList<AwkDiagnostic> Warnings
    {
        get
        {
            lock (_lifetime)
            {
                ObjectDisposedException.ThrowIf(_disposeCompletion is not null, this);
                return _warnings;
            }
        }
    }

    public Task<AwkExitCode> ExecuteAsync(IAwkHost host, CancellationToken cancellationToken) =>
        ExecuteAsync(host, cancellationToken, AwkRuntimeOptions.CreateSystemDefault());

    internal Task<AwkExitCode> ExecuteAsync(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
    {
        AwkInvocation invocation;
        AwkProgram program;
        IReadOnlyList<AwkDiagnostic> warnings;
        AwkRuntimeOptions options;
        TaskCompletionSource<AwkExitCode>? completion = null;
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_disposeCompletion is not null, this);
            invocation = _invocation ?? throw new ObjectDisposedException(nameof(AwkCompiledProgram));
            program = _program;
            warnings = _warnings;
            options = owner.IsEnabled ? runtimeOptions with { MemoryBudget = owner.Scope } : runtimeOptions;
            if (owner.IsEnabled)
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = owner.Track(completion.Task);
            }
        }
        // Register the started use before releasing the gate, but never invoke a host
        // while holding it: callbacks may synchronize concurrent executions.
        if (completion is { } active)
        {
            _ = ExecuteTrackedAsync(active, host, invocation, program, warnings, cancellationToken, options);
            return active.Task;
        }
        return AwkExecutor.ExecuteCompiledAsync(
            host, invocation, program, warnings, cancellationToken, options);
    }

    private static async Task ExecuteTrackedAsync(TaskCompletionSource<AwkExitCode> completion,
        IAwkHost host, AwkInvocation invocation, AwkProgram program, IReadOnlyList<AwkDiagnostic> warnings,
        CancellationToken cancellationToken, AwkRuntimeOptions options)
    {
        try
        {
            completion.SetResult(await AwkExecutor.ExecuteCompiledAsync(
                host, invocation, program, warnings, cancellationToken, options).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception)
        {
            completion.SetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (_lifetime)
        {
            if (_disposeCompletion is { } existing) return new(existing.Task);
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeCompletion = completion;
            // Started executions already hold their snapshots. Stop retaining the artifact
            // payload here; its charge remains until those executions and cleanup finish.
            _invocation = null;
            _program = default;
            _warnings = [];
        }
        _ = CompleteDisposeAsync(completion);
        return new(completion.Task);
    }

    private async Task CompleteDisposeAsync(TaskCompletionSource completion)
    {
        try
        {
            await owner.DisposeAsync().ConfigureAwait(false);
            completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }
}
