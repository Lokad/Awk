using System.Threading;

namespace Lokad.Awk;

/// <summary>The shared logical-work allowance; observational counters do not control admission.</summary>
internal sealed class AwkExecutionGovernor
{
    // Aligned with Lokad.Lython's execution defaults. Memory and size limits remain awk-specific.
    public const int DefaultMaxExecutionSteps = 50_000_000;
    public const long DefaultMaxHostCalls = 100_000;
    public const int DefaultMaxFunctionCallDepth = 1_000;
    public const int WorkChunkBytes = 4_096;

    private readonly int _maxExecutionSteps;
    private readonly AwkRuntimeCounters _counters;
    private long _executionSteps;

    public AwkExecutionGovernor(int maxExecutionSteps, AwkRuntimeCounters counters)
    {
        _maxExecutionSteps = maxExecutionSteps;
        _counters = counters;
    }

    public void Check(int steps, CancellationToken cancellationToken)
    {
        ThrowIfCancellationRequested(cancellationToken);
        if (steps > _maxExecutionSteps - _executionSteps)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorExecutionStepLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        _executionSteps += steps;
        _counters.RecordExecutionSteps(steps);
    }

    public void ThrowIfCancellationRequested(CancellationToken cancellationToken)
    {
        _counters.RecordCancellationCheck();
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static int ByteWorkSteps(int byteCount) =>
        byteCount <= 0 ? 0 : 1 + (byteCount - 1) / WorkChunkBytes;
}
