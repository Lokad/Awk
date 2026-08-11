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
    private readonly AwkRuntimeResourceMonitor _resources;
    private readonly bool _countAsRuntimeInput;
    private ReadOnlyMemory<byte> _content;
    private ReadOnlyMemory<int> _lineStartOffsets;
    private int _lineIndex;
    private int _lineStart;
    private bool _reachedEof;

    internal AsyncLineReader(
        IAwkHost host,
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken,
        int maxLineCount,
        AwkRuntimeResourceMonitor resources,
        bool countAsRuntimeInput = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLineCount);

        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(resources);
        _host = host;
        _descriptor = descriptor;
        _cancellationToken = cancellationToken;
        _maxLineCount = maxLineCount;
        _resources = resources;
        _countAsRuntimeInput = countAsRuntimeInput;
        _content = ReadOnlyMemory<byte>.Empty;
        _lineStartOffsets = ReadOnlyMemory<int>.Empty;
    }

    public async ValueTask<bool> TryBufferLineAsync()
    {
        while (_lineIndex >= BufferedLineCount)
        {
            if (_reachedEof)
            {
                return false;
            }

            _resources.RecordHostCall();
            var result = await _host.ReadLinesAsync(_descriptor, _maxLineCount, _cancellationToken).ConfigureAwait(false);
            if (result.Failed)
                throw new AwkHostReadException(_descriptor, result.Error);

            if (_countAsRuntimeInput)
                _resources.RecordInputBytes(result.Content.Length);
            else
                _resources.RecordBytesRead(result.Content.Length);
            _content = result.Content;
            _lineStartOffsets = result.LineStartOffsets;
            _lineIndex = 0;
            _lineStart = 0;
            _reachedEof = result.ReachedEof;
        }

        return true;
    }

    public ReadOnlyMemory<byte> ReadLine()
    {
        if (_lineIndex >= BufferedLineCount)
        {
            throw new InvalidOperationException(
                "No line is buffered. Call TryBufferLineAsync() before ReadLine().");
        }

        var nextStart = _lineIndex < _lineStartOffsets.Length
            ? _lineStartOffsets.Span[_lineIndex]
            : _content.Length;
        var line = _content.Slice(_lineStart, nextStart - _lineStart);
        _lineStart = nextStart;
        _lineIndex++;
        return line;
    }

    public AwkBufferedInputTail TakeBufferedTail()
    {
        var content = _lineIndex < BufferedLineCount
            ? _content[_lineStart..]
            : ReadOnlyMemory<byte>.Empty;
        _lineStart = _content.Length;
        _lineIndex = BufferedLineCount;
        return new AwkBufferedInputTail(content, _reachedEof);
    }

    public AwkFileDescriptor Descriptor => _descriptor;

    private int BufferedLineCount => _content.IsEmpty ? 0 : _lineStartOffsets.Length + 1;
}

internal readonly record struct AwkBufferedInputTail(
    ReadOnlyMemory<byte> Content,
    bool DescriptorReachedEof);
