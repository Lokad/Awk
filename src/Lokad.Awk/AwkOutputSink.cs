using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkOutputSink : IDisposable
{
    private readonly AwkUtf8Builder _standardOutput;
    private readonly List<AwkDiagnostic> _diagnostics = [];
    private readonly AwkMemoryAccounting _governor;
    private readonly Action? _checkpoint;
    private long _diagnosticBufferBytes;
    private long _diagnosticMessageBytes;

    public AwkOutputSink(
        AwkRuntimeLimits limits,
        AwkMemoryAccounting governor,
        AwkRuntimeCounters counters,
        Action? checkpoint = null)
    {
        _standardOutput = new AwkUtf8Builder(limits, governor);
        _governor = governor;
        _checkpoint = checkpoint;
        counters.RecordOutputSinkCreation();
    }

    internal bool HasPendingOutput => _standardOutput.Length != 0 || _diagnostics.Count != 0;

    public void Append(AwkText value)
    {
        if (value.ByteLength == 0)
            return;

        _standardOutput.Append(value, _checkpoint);
    }

    public void Append(ReadOnlySpan<AwkText> values)
    {
        if (values.IsEmpty)
            return;

        _standardOutput.Append(values, _checkpoint);
    }

    public void Append(ReadOnlyMemory<byte> content)
    {
        if (content.IsEmpty)
            return;

        _standardOutput.Append(content.Span, _checkpoint);
    }

    public void Append(ReadOnlySpan<byte> content, AwkText terminator) =>
        _standardOutput.Append(content, terminator, _checkpoint);

    public void ReportDiagnostic(AwkDiagnostic diagnostic)
    {
        var messageBytes = AwkRuntimeMemory.EstimateDiagnosticMessageBytes(diagnostic.Message.Length);
        var retainedMessages = AwkRuntimeMemory.CheckedAdd(_diagnosticMessageBytes, messageBytes);
        var grow = _diagnostics.Count == _diagnostics.Capacity;
        if (grow && _diagnostics.Capacity == Array.MaxLength)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        var capacity = grow
            ? (int)Math.Min(Array.MaxLength, Math.Max(4L, (long)_diagnostics.Capacity * 2))
            : _diagnostics.Capacity;
        var bufferBytes = grow ? AwkRuntimeMemory.EstimateDiagnosticBufferBytes(capacity) : 0;
        var reservedBytes = AwkRuntimeMemory.CheckedAdd(messageBytes, bufferBytes);
        _governor.Reserve(reservedBytes);
        try
        {
            if (grow)
                _diagnostics.EnsureCapacity(capacity);
        }
        catch
        {
            _governor.ReleaseReserved(reservedBytes);
            throw;
        }

        _governor.CommitReserved(reservedBytes);
        if (grow)
        {
            _governor.ReleaseCommitted(_diagnosticBufferBytes);
            _diagnosticBufferBytes = bufferBytes;
        }
        _diagnosticMessageBytes = retainedMessages;
        _diagnostics.Add(diagnostic);
    }

    public async Task FlushAsync(AwkIo io)
    {
        if (_standardOutput.Length != 0)
        {
            await io.WriteStandardOutputAsync(_standardOutput.AsMemory()).ConfigureAwait(false);
            _standardOutput.Clear();
        }

        await FlushDiagnosticsAsync(io).ConfigureAwait(false);
    }

    public async Task FlushDiagnosticsAsync(AwkIo io)
    {
        try
        {
            for (var index = 0; index < _diagnostics.Count; index++)
            {
                await io.WriteStandardErrorAsync(AwkDiagnostics.Warning(_diagnostics[index])).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not AwkRuntimeException and not OperationCanceledException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorUnexpectedFailure, AwkDiagnosticId.HostIoFailure);
        }
        finally
        {
            ClearDiagnostics();
        }
    }

    public void Dispose()
    {
        ClearDiagnostics();
        _diagnostics.Capacity = 0;
        _governor.ReleaseCommitted(_diagnosticBufferBytes);
        _diagnosticBufferBytes = 0;
        _standardOutput.Dispose();
    }

    private void ClearDiagnostics()
    {
        _diagnostics.Clear();
        _governor.ReleaseCommitted(_diagnosticMessageBytes);
        _diagnosticMessageBytes = 0;
    }

    public override string ToString() => AwkUtf8Codec.Decode(_standardOutput.AsSpan());
}
