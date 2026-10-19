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
    private readonly bool _exposeHostErrorDetails;
    private readonly int _maxHostErrorBytes;
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
        bool countAsRuntimeInput = false,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10)
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

        _exposeHostErrorDetails = exposeHostErrorDetails;
        _maxHostErrorBytes = maxHostErrorBytes;
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
            var result = await _host.ReadLinesAsync(_descriptor, _maxLineCount, _cancellationToken)
                .ConfigureAwait(false);
            if (result.Failed)
            {
                throw new AwkHostReadException(
                    _descriptor,
                    new AwkOperationError(AwkHostBoundaryPolicy.ResolveError(
                        result.Error,
                        _exposeHostErrorDetails,
                        _maxHostErrorBytes)));
            }

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
        var line = PeekLine();
        _lineStart += line.Length;
        _lineIndex++;
        return line;
    }

    public ReadOnlyMemory<byte> PeekLine()
    {
        if (_lineIndex >= BufferedLineCount)
        {
            throw new InvalidOperationException(
                "No line is buffered. Call TryBufferLineAsync() before ReadLine().");
        }

        var nextStart = _lineIndex < _lineStartOffsets.Length
            ? _lineStartOffsets.Span[_lineIndex]
            : _content.Length;
        return _content.Slice(_lineStart, nextStart - _lineStart);
    }

    public AwkBufferedInputTail TakeBufferedTail()
    {
        var tail = PeekBufferedTail();
        _lineStart = _content.Length;
        _lineIndex = BufferedLineCount;
        return tail;
    }

    public AwkBufferedInputTail PeekBufferedTail() =>
        new(_lineIndex < BufferedLineCount ? _content[_lineStart..] : ReadOnlyMemory<byte>.Empty,
            _reachedEof);

    public AwkFileDescriptor Descriptor => _descriptor;

    private int BufferedLineCount => _content.IsEmpty ? 0 : _lineStartOffsets.Length + 1;
}

internal readonly record struct AwkBufferedInputTail(
    ReadOnlyMemory<byte> Content,
    bool DescriptorReachedEof);
