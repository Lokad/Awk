using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkOutputSink : IDisposable
{
    private readonly AwkUtf8Builder _standardOutput;
    private readonly List<AwkIoEvent> _ioEvents = [];
    private readonly List<IDisposable> _ioEventLeases = [];
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkMemoryGovernor _governor;

    public AwkOutputSink()
        : this(
            AwkRuntimeLimits.CreateDefault(),
            AwkMemoryGovernor.Disabled,
            AwkRuntimeCounters.Disabled)
    {
    }

    public AwkOutputSink(
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor,
        AwkRuntimeCounters counters)
    {
        _standardOutput = new AwkUtf8Builder(limits, governor);
        _counters = counters;
        _governor = governor;
    }

    public IReadOnlyList<AwkIoEvent> IoEvents => _ioEvents;

    public void Append(string value)
    {
        if (value.Length == 0)
            return;

        Append(AwkUtf8Codec.Encode(value, _counters));
    }

    public void Append(AwkText value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ByteLength == 0)
            return;

        _standardOutput.Append(value);
    }

    public void Append(ReadOnlyMemory<byte> content)
    {
        if (content.IsEmpty)
            return;

        _standardOutput.Append(content);
    }

    public void AppendRedirected(string path, AwkOutputRedirectionKind kind, string content)
    {
        AppendRedirected(new AwkRedirectionPath(path), kind, content);
    }

    public void AppendRedirected(AwkRedirectionPath path, AwkOutputRedirectionKind kind, string content)
    {
        if (content.Length == 0)
            return;

        AppendRedirected(path, kind, AwkUtf8Codec.Encode(content, _counters));
    }

    public void AppendRedirected(string path, AwkOutputRedirectionKind kind, ReadOnlyMemory<byte> content)
    {
        AppendRedirected(new AwkRedirectionPath(path), kind, content);
    }

    public void AppendRedirected(AwkRedirectionPath path, AwkOutputRedirectionKind kind, ReadOnlyMemory<byte> content)
    {
        if (content.IsEmpty)
            return;

        var lease = _governor.ReserveTransient(AwkRuntimeMemory.EstimateByteArrayBytes(content.Length));
        try
        {
            AddIoEvent(AwkIoEvent.Write(new AwkRedirectedOutput(path, kind, content)), lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public void Close(string path) =>
        AddIoEvent(AwkIoEvent.Close(path), _governor.ReserveTransient(0));

    public void ReportDiagnostic(AwkDiagnostic diagnostic) =>
        AddIoEvent(AwkIoEvent.DiagnosticWarning(diagnostic), _governor.ReserveTransient(0));

    public async Task FlushAsync(AwkIo io)
    {
        if (_standardOutput.Length != 0)
        {
            await io.WriteStandardOutputAsync(_standardOutput.AsMemory()).ConfigureAwait(false);
            _standardOutput.Clear();
        }

        await FlushIoEventsAsync(io).ConfigureAwait(false);
    }

    public async Task FlushIoEventsAsync(AwkIo io)
    {
        try
        {
            for (var index = 0; index < _ioEvents.Count; index++)
            {
                var ioEvent = _ioEvents[index];
                if (ioEvent.Kind == AwkIoEventKind.Write)
                {
                    await io.WriteAsync(ioEvent.Output).ConfigureAwait(false);
                }
                else if (ioEvent.Kind == AwkIoEventKind.Close)
                {
                    await io.CloseAsync(ioEvent.Path).ConfigureAwait(false);
                }
                else if (ioEvent.Kind == AwkIoEventKind.Diagnostic)
                {
                    await io.WriteStandardErrorAsync(AwkDiagnostics.Warning(ioEvent.Diagnostic)).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            ClearIoEvents();
        }
    }

    private void AddIoEvent(AwkIoEvent ioEvent, IDisposable lease)
    {
        _ioEvents.Add(ioEvent);
        try
        {
            _ioEventLeases.Add(lease);
        }
        catch
        {
            _ioEvents.RemoveAt(_ioEvents.Count - 1);
            throw;
        }
    }

    private void ClearIoEvents()
    {
        for (var index = 0; index < _ioEventLeases.Count; index++)
        {
            _ioEventLeases[index].Dispose();
        }

        _ioEventLeases.Clear();
        _ioEvents.Clear();
    }

    public void Dispose()
    {
        ClearIoEvents();
        _standardOutput.Dispose();
    }

    public override string ToString() => AwkUtf8Codec.Decode(_standardOutput.ToArray());
}
