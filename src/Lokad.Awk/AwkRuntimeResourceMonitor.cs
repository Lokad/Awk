using System;
using System.Threading;

namespace Lokad.Awk;

internal sealed class AwkRuntimeResourceMonitor
{
    private readonly AwkRuntimeLimits _limits;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkExecutionGovernor _execution;
    private readonly CancellationToken _cancellationToken;
    private long _hostCalls;
    private long _outputBytes;
    private long _inputBytes;
    private long _records;
    private long _regexOperations;
    private long _sortComparisons;
    private long _regexRecordSeparatorBytesScanned;
    private int _regexCompilations;
    private long _sourceBytes;
    private long _expandedSourceBytes;
    private int _includeFiles;

    public AwkRuntimeResourceMonitor(
        AwkRuntimeLimits limits,
        AwkRuntimeCounters counters,
        CancellationToken cancellationToken = default)
    {
        _limits = limits;
        _counters = counters;
        _execution = new AwkExecutionGovernor(limits.MaxExecutionSteps, counters);
        _cancellationToken = cancellationToken;
    }

    public AwkRuntimeLimits Limits => _limits;

    public void RecordHostCall()
    {
        RecordExecutionStep();
        if (_limits.MaxHostCalls != AwkRuntimeLimits.Unlimited)
        {
            var next = AwkRuntimeMemory.CheckedAdd(_hostCalls, 1);
            if (next > _limits.MaxHostCalls)
                throw new AwkRuntimeException(AwkMessages.ErrorHostCallLimit);
        }

        if (_limits.MaxHostCalls != AwkRuntimeLimits.Unlimited)
            _hostCalls++;
        _counters.RecordHostCall();
    }

    public void RecordCleanupHostCall() => _counters.RecordHostCall();

    public void RecordExecutionStep() => RecordExecutionStep(_cancellationToken);

    public void RecordExecutionStep(CancellationToken cancellationToken) =>
        _execution.Check(1, cancellationToken);

    public void ThrowIfCancellationRequested(CancellationToken cancellationToken) =>
        _execution.ThrowIfCancellationRequested(cancellationToken);

    public void RecordByteWork(int byteCount) =>
        _execution.Check(AwkExecutionGovernor.ByteWorkSteps(byteCount), _cancellationToken);

    public void RecordBytesRead(long byteCount)
    {
        _counters.RecordBytesRead(byteCount);
    }

    public void RecordInputBytes(long byteCount)
    {
        if (byteCount > 0 && _limits.MaxInputBytes != AwkRuntimeLimits.Unlimited)
        {
            var next = AwkRuntimeMemory.CheckedAdd(_inputBytes, byteCount);
            if (next > _limits.MaxInputBytes)
                throw new AwkRuntimeException(AwkMessages.ErrorInputByteLimit, AwkDiagnosticId.RuntimeResourceLimit);

            _inputBytes = next;
        }

        _counters.RecordBytesRead(byteCount);
    }

    public void RecordInputRecord()
    {
        if (_limits.MaxRecords == AwkRuntimeLimits.Unlimited)
            return;

        var next = AwkRuntimeMemory.CheckedAdd(_records, 1);
        if (next > _limits.MaxRecords)
            throw new AwkRuntimeException(AwkMessages.ErrorRecordLimit, AwkDiagnosticId.RuntimeResourceLimit);

        _records = next;
    }

    public void RecordRegexOperation(int inputBytes)
    {
        var next = _limits.MaxRegexOperations == AwkRuntimeLimits.Unlimited
            ? 0 : AwkRuntimeMemory.CheckedAdd(_regexOperations, 1);
        if (next > _limits.MaxRegexOperations)
            throw new AwkRuntimeException(AwkMessages.ErrorRegexOperationLimit, AwkDiagnosticId.RuntimeResourceLimit);

        _execution.Check(1 + AwkExecutionGovernor.ByteWorkSteps(inputBytes), _cancellationToken);
        if (_limits.MaxRegexOperations != AwkRuntimeLimits.Unlimited)
            _regexOperations = next;
        _counters.RecordRegexMatch();
    }

    public void RecordSortComparison()
    {
        var next = _limits.MaxSortComparisons == AwkRuntimeLimits.Unlimited
            ? 0 : AwkRuntimeMemory.CheckedAdd(_sortComparisons, 1);
        if (next > _limits.MaxSortComparisons)
            throw new AwkRuntimeException(AwkMessages.ErrorSortComparisonLimit, AwkDiagnosticId.RuntimeResourceLimit);

        RecordExecutionStep();
        if (_limits.MaxSortComparisons != AwkRuntimeLimits.Unlimited)
            _sortComparisons = next;
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

        var next = _limits.MaxRegexRecordSeparatorBytesScanned == AwkRuntimeLimits.Unlimited
            ? 0 : AwkRuntimeMemory.CheckedAdd(_regexRecordSeparatorBytesScanned, byteCount);
        if (next > _limits.MaxRegexRecordSeparatorBytesScanned)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexRecordSeparatorScanLimit,
                AwkDiagnosticId.RuntimeResourceLimit);
        }

        if (_limits.MaxRegexRecordSeparatorBytesScanned != AwkRuntimeLimits.Unlimited)
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
