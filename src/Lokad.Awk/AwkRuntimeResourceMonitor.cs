using System;

namespace Lokad.Awk;

internal sealed class AwkRuntimeResourceMonitor
{
    private readonly AwkRuntimeLimits _limits;
    private readonly AwkRuntimeCounters _counters;
    private long _executionSteps;
    private long _hostCalls;
    private long _outputBytes;
    private long _regexRecordSeparatorBytesScanned;
    private int _regexCompilations;
    private long _sourceBytes;
    private long _expandedSourceBytes;
    private int _includeFiles;

    public AwkRuntimeResourceMonitor(AwkRuntimeLimits limits, AwkRuntimeCounters counters)
    {
        _limits = limits;
        _counters = counters;
    }

    public AwkRuntimeLimits Limits => _limits;

    public void RecordHostCall()
    {
        if (_limits.MaxHostCalls != AwkRuntimeLimits.Unlimited)
        {
            var next = AwkRuntimeMemory.CheckedAdd(_hostCalls, 1);
            if (next > _limits.MaxHostCalls)
                throw new AwkRuntimeException(AwkMessages.ErrorHostCallLimit);

            _hostCalls = next;
        }

        _counters.RecordHostCall();
    }

    public void RecordCleanupHostCall() => _counters.RecordHostCall();

    public void RecordExecutionStep()
    {
        var next = AwkRuntimeMemory.CheckedAdd(_executionSteps, 1);
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
            var next = AwkRuntimeMemory.CheckedAdd(_outputBytes, byteCount);
            if (next > _limits.MaxOutputBytes)
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

            _outputBytes = next;
        }

        _counters.RecordOutputBytes(byteCount);
    }

    public void RecordRegexRecordSeparatorBytesScanned(int byteCount)
    {
        if (byteCount <= 0)
            return;

        var next = AwkRuntimeMemory.CheckedAdd(_regexRecordSeparatorBytesScanned, byteCount);
        if (next > _limits.MaxRegexRecordSeparatorBytesScanned)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexRecordSeparatorScanLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        _regexRecordSeparatorBytesScanned = next;
        _counters.RecordRecordSeparatorBytesScanned(byteCount);
    }

    public void RecordRegexCompilation()
    {
        ReserveRegexCompilations(1);
        _counters.RecordRegexCompile();
    }

    public void ReserveRegexCompilations(int count)
    {
        var next = AwkRuntimeMemory.CheckedAdd(_regexCompilations, count);
        if (next > _limits.MaxRegexCompilations)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexCompilationLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        _regexCompilations = next;
    }

    public void RecordSourceBytes(long byteCount)
    {
        if (byteCount <= 0)
            return;

        var next = AwkRuntimeMemory.CheckedAdd(_sourceBytes, byteCount);
        if (next > _limits.MaxSourceBytes)
            throw new AwkRuntimeException(AwkMessages.ErrorSourceLimit, AwkDiagnosticId.RuntimeResourceLimit);

        _sourceBytes = next;
    }

    public void ThrowIfSourceUnitCountExceeded(int count)
    {
        if (count > _limits.MaxSourceUnits)
            throw new AwkRuntimeException(AwkMessages.ErrorSourceUnitLimit, AwkDiagnosticId.RuntimeResourceLimit);
    }

    public void RecordExpandedSourceBytes(long byteCount)
    {
        if (byteCount <= 0)
            return;

        var next = AwkRuntimeMemory.CheckedAdd(_expandedSourceBytes, byteCount);
        if (next > _limits.MaxExpandedSourceBytes)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorExpandedSourceLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        _expandedSourceBytes = next;
    }

    public void ThrowIfFrontendCharactersExceeded(int count)
    {
        if (count > _limits.MaxFrontendCharacters)
            throw new AwkRuntimeException(AwkMessages.ErrorFrontendCharacterLimit, AwkDiagnosticId.RuntimeResourceLimit);
    }

    public void RecordInclude(int depth)
    {
        if (depth > _limits.MaxIncludeDepth)
            throw new AwkRuntimeException(AwkMessages.ErrorIncludeDepthLimit, AwkDiagnosticId.RuntimeResourceLimit);

        var next = AwkRuntimeMemory.CheckedAdd(_includeFiles, 1);
        if (next > _limits.MaxIncludeFiles)
            throw new AwkRuntimeException(AwkMessages.ErrorIncludeFileLimit, AwkDiagnosticId.RuntimeResourceLimit);

        _includeFiles = next;
    }

    public void ThrowIfRecordBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxRecordBytes);

    public void ThrowIfValueBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxValueBytes);

    public void ThrowIfVmStackBytesExceeded(long byteCount) =>
        ThrowIfExceeded(byteCount, _limits.MaxVmStackBytes);

    private static void ThrowIfExceeded(long byteCount, long limit)
    {
        if (limit != AwkRuntimeLimits.Unlimited && byteCount > limit)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

}
