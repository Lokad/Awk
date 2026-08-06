using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AsyncLineReader
{
    private readonly IAwkHost _host;
    private readonly AwkFileDescriptor _descriptor;
    private readonly CancellationToken _cancellationToken;
    private readonly int _maxLineCount;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeResourceMonitor _resources;
    private ReadOnlyMemory<byte> _content;
    private ReadOnlyMemory<int> _lineStartOffsets;
    private int _lineIndex;
    private int _lineStart;
    private bool _reachedEof;

    public AsyncLineReader(
        IAwkHost host,
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken,
        int maxLineCount = int.MaxValue)
        : this(host, descriptor, cancellationToken, maxLineCount, AwkRuntimeCounters.Disabled)
    {
    }

    internal AsyncLineReader(
        IAwkHost host,
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken,
        int maxLineCount,
        AwkRuntimeCounters counters)
        : this(
            host,
            descriptor,
            cancellationToken,
            maxLineCount,
            counters,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters))
    {
    }

    internal AsyncLineReader(
        IAwkHost host,
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken,
        int maxLineCount,
        AwkRuntimeCounters counters,
        AwkRuntimeResourceMonitor resources)
    {
        if (maxLineCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLineCount));
        }

        _host = host ?? throw new ArgumentNullException(nameof(host));
        _descriptor = descriptor;
        _cancellationToken = cancellationToken;
        _maxLineCount = maxLineCount;
        _counters = counters;
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _content = ReadOnlyMemory<byte>.Empty;
        _lineStartOffsets = ReadOnlyMemory<int>.Empty;
    }

    public async ValueTask<bool> ReachedEof()
    {
        while (_lineIndex >= BufferedLineCount)
        {
            if (_reachedEof)
            {
                return true;
            }

            _resources.RecordHostCall();
            var result = await _host.ReadLinesAsync(_descriptor, _maxLineCount, _cancellationToken).ConfigureAwait(false);
            if (result.Failed)
                throw new AwkHostReadException(_descriptor, result.Error);

            _resources.RecordBytesRead(result.Content.Length);
            _content = result.Content;
            _lineStartOffsets = result.LineStartOffsets;
            _lineIndex = 0;
            _lineStart = 0;
            _reachedEof = result.ReachedEof;
        }

        return false;
    }

    public ReadOnlyMemory<byte> ReadLine()
    {
        if (_lineIndex >= BufferedLineCount)
        {
            throw new InvalidOperationException("No line is buffered. Call ReachedEof() before ReadLine().");
        }

        var nextStart = _lineIndex < _lineStartOffsets.Length
            ? _lineStartOffsets.Span[_lineIndex]
            : _content.Length;
        var line = _content.Slice(_lineStart, nextStart - _lineStart);
        _lineStart = nextStart;
        _lineIndex++;
        return line;
    }

    private int BufferedLineCount => _content.IsEmpty ? 0 : _lineStartOffsets.Length + 1;
}
