using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeResourceMonitor
{
    private readonly AwkRuntimeLimits _limits;
    private readonly AwkRuntimeCounters _counters;
    private long _executionSteps;
    private long _hostCalls;
    private long _outputBytes;

    public AwkRuntimeResourceMonitor(AwkRuntimeLimits limits, AwkRuntimeCounters counters)
    {
        _limits = limits;
        _counters = counters;
    }

    public void RecordHostCall()
    {
        if (_limits.MaxHostCalls != AwkRuntimeLimits.Unlimited)
        {
            var next = CheckedAdd(_hostCalls, 1);
            if (next > _limits.MaxHostCalls)
                throw new AwkRuntimeException(AwkMessages.ErrorHostCallLimit);

            _hostCalls = next;
        }

        _counters.RecordHostCall();
    }

    public void RecordExecutionStep()
    {
        var next = CheckedAdd(_executionSteps, 1);
        if (next > _limits.MaxExecutionSteps)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorExecutionStepLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        _executionSteps = next;
        _counters.RecordExecutionStep();
    }

    public void RecordBytesRead(long byteCount)
    {
        _counters.RecordBytesRead(byteCount);
    }

    public void RecordOutputBytes(long byteCount)
    {
        if (byteCount <= 0)
            return;

        if (_limits.MaxOutputBytes != AwkRuntimeLimits.Unlimited)
        {
            var next = CheckedAdd(_outputBytes, byteCount);
            if (next > _limits.MaxOutputBytes)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

            _outputBytes = next;
        }

        _counters.RecordOutputBytes(byteCount);
    }

    public void ThrowIfRecordBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxRecordBytes);

    public void ThrowIfValueBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxValueBytes);

    public void ThrowIfRegexCaptureBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxRegexCaptureBytes);

    public void ThrowIfVmStackBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxVmStackBytes);

    private static void ThrowIfExceeded(long byteCount, long limit)
    {
        if (limit != AwkRuntimeLimits.Unlimited && byteCount > limit)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

    private static long CheckedAdd(long left, long right)
    {
        if (left > long.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }
}
