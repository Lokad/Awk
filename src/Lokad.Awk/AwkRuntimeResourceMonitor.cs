namespace Lokad.Awk;

internal sealed class AwkRuntimeResourceMonitor
{
    private readonly AwkRuntimeLimits _limits;
    private readonly AwkRuntimeCounters _counters;
    private readonly CancellationToken _cancellationToken;

    public AwkRuntimeResourceMonitor(
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        CancellationToken cancellationToken = default)
    {
        _limits = limits;
        _counters = counters;
        _cancellationToken = cancellationToken;
    }

    public AwkRuntimeLimits Limits => _limits;

    public void RecordHostCall()
    {
        Checkpoint();
        _counters.Add(AwkCounter.HostCalls, 1);
    }

    public void RecordCleanupHostCall() => _counters.Add(AwkCounter.HostCalls);

    public void Checkpoint() => ThrowIfCancellationRequested(_cancellationToken);

    public void Checkpoint(CancellationToken cancellationToken) =>
        ThrowIfCancellationRequested(cancellationToken);

    public void ThrowIfCancellationRequested(CancellationToken cancellationToken)
    {

        cancellationToken.ThrowIfCancellationRequested();
    }

    public void RecordBytesRead(long byteCount)
    {
        _counters.Add(AwkCounter.BytesRead, byteCount);
    }

    public void RecordOutputBytes(long byteCount)
    {
        if (byteCount <= 0)
            return;

        _counters.Add(AwkCounter.OutputBytes, byteCount);
    }

    public void RecordInclude(int depth)
    {
        if (depth > _limits.MaxIncludeDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorIncludeDepthLimit, AwkDiagnosticId.RuntimeResourceLimit);
    }

}
