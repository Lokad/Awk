using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkHostBoundaryPolicy
{
    public static async Task<T> AwaitAsync<T>(
        Task<T> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorHostOperationTimeout,
                AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    public static string ResolveError(
        AwkOperationError error,
        bool exposeDetails,
        int maxErrorBytes)
    {
        if (!exposeDetails ||
            error.Message.Length > maxErrorBytes ||
            Encoding.UTF8.GetByteCount(error.Message) > maxErrorBytes)
            return default(AwkOperationError).Message;

        return error.Message;
    }
}

internal sealed class AwkCleanupDeadline
{
    private readonly long _deadlineTimestamp;

    public AwkCleanupDeadline(TimeSpan timeout)
    {
        var now = Stopwatch.GetTimestamp();
        var timeoutTicks = timeout.TotalSeconds * Stopwatch.Frequency;
        _deadlineTimestamp = timeoutTicks >= long.MaxValue - now
            ? long.MaxValue
            : now + (long)timeoutTicks;
    }

    public async Task AwaitAsync<T>(Task<T> operation)
    {
        var remainingTicks = _deadlineTimestamp - Stopwatch.GetTimestamp();
        if (remainingTicks <= 0)
            return;

        var remaining = TimeSpan.FromSeconds((double)remainingTicks / Stopwatch.Frequency);
        try
        {
            _ = await operation.WaitAsync(remaining).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Cleanup is best effort within one aggregate deadline.
        }
    }
}
