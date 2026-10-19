using System;
using System.Threading;

namespace Lokad.Awk;

internal sealed class AwkRuntimeResourceMonitor
{
    private readonly AwkRuntimeLimits _limits;
    private readonly AwkRuntimeCounters _counters;
    private readonly CancellationToken _cancellationToken;
    private long _outputBytes;
    private long _inputBytes;
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

    public void RecordRegexMatch()
    {
        Checkpoint();
        _counters.RecordRegexMatch();
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

    public void RecordRegexRecordSeparatorBytesScanned(int byteCount) =>
        _counters.RecordRecordSeparatorBytesScanned(byteCount);

    public void RecordRegexCompilation() => _counters.RecordRegexCompile();

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

    private static void ThrowIfExceeded(long byteCount, long limit)
    {
        if (limit != AwkRuntimeLimits.Unlimited && byteCount > limit)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

}
