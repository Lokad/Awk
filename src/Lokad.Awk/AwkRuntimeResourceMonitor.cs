using System;
using System.Threading;

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
        _counters.RecordHostCall();
    }

    public void RecordCleanupHostCall() => _counters.RecordHostCall();

    public void Checkpoint() => ThrowIfCancellationRequested(_cancellationToken);

    public void Checkpoint(CancellationToken cancellationToken) =>
        ThrowIfCancellationRequested(cancellationToken);

    public void ThrowIfCancellationRequested(CancellationToken cancellationToken)
    {
        _counters.RecordCancellationCheck();
        cancellationToken.ThrowIfCancellationRequested();
    }

    public void RecordBytesRead(long byteCount)
    {
        _counters.RecordBytesRead(byteCount);
    }

    public void RecordRegexMatch()
    {
        Checkpoint();
        _counters.RecordRegexMatch();
    }

    public void RecordOutputBytes(long byteCount)
    {
        if (byteCount <= 0)
            return;

        _counters.RecordOutputBytes(byteCount);
    }

    public void RecordRegexRecordSeparatorBytesScanned(int byteCount) =>
        _counters.RecordRecordSeparatorBytesScanned(byteCount);

    public void RecordRegexCompilation() => _counters.RecordRegexCompile();

    public void RecordInclude(int depth)
    {
        if (depth > _limits.MaxIncludeDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorIncludeDepthLimit, AwkDiagnosticId.RuntimeResourceLimit);
    }

}
