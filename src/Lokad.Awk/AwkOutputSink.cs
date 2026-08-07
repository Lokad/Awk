using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkOutputSink : IDisposable
{
    private readonly AwkUtf8Builder _standardOutput;
    private readonly List<AwkDiagnostic> _diagnostics = [];
    private readonly AwkRuntimeCounters _counters;

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
    }

    public void Append(string value)
    {
        if (value.Length == 0)
            return;

        Append(AwkUtf8Codec.Encode(value, _counters));
    }

    public void Append(AwkText value)
    {
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

    public void ReportDiagnostic(AwkDiagnostic diagnostic) =>
        _diagnostics.Add(diagnostic);

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
        finally
        {
            _diagnostics.Clear();
        }
    }

    public void Dispose()
    {
        _diagnostics.Clear();
        _standardOutput.Dispose();
    }

    public override string ToString() => AwkUtf8Codec.Decode(_standardOutput.ToArray());
}
