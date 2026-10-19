using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordDescriptorReader
{
    private readonly IAwkHost _host;
    private readonly CancellationToken _cancellationToken;
    private readonly AwkRuntimeResourceMonitor _resources;
    private readonly bool _exposeHostErrorDetails;
    private readonly int _maxHostErrorBytes;

    public AwkRecordDescriptorReader(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeResourceMonitor resources,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10)
    {
        _host = host;
        _cancellationToken = cancellationToken;
        _resources = resources;

        _exposeHostErrorDetails = exposeHostErrorDetails;
        _maxHostErrorBytes = maxHostErrorBytes;
    }

    public AsyncLineReader CreateLineReader(AwkFileDescriptor descriptor) =>
        new(
            _host,
            descriptor,
            _cancellationToken,
            AwkIo.InputReadBatchLineCount,
            _resources,
            countAsRuntimeInput: true,
            exposeHostErrorDetails: _exposeHostErrorDetails,
            maxHostErrorBytes: _maxHostErrorBytes);

    public async Task<AwkRecordReadBatch> ReadNextBatchAsync(AwkFileDescriptor descriptor)
    {
        _resources.RecordHostCall();
        var result = await _host.ReadLinesAsync(descriptor, AwkIo.InputReadBatchLineCount, _cancellationToken)
            .ConfigureAwait(false);
        if (result.Failed)
        {
            throw new AwkHostReadException(
                descriptor,
                new AwkOperationError(AwkHostBoundaryPolicy.ResolveError(
                    result.Error,
                    _exposeHostErrorDetails,
                    _maxHostErrorBytes)));
        }

        _resources.RecordInputBytes(result.Content.Length);
        return AwkRecordReadBatch.Success(result.Content, result.Completion);
    }

}

internal readonly struct AwkRecordReadBatch(
    ReadOnlyMemory<byte> content,
    AwkReadCompletion completion)
{
    public static AwkRecordReadBatch Success(
        ReadOnlyMemory<byte> content,
        AwkReadCompletion completion) =>
        new(content, completion);

    public ReadOnlyMemory<byte> Content { get; } = content;

    public bool ReachedEof => completion == AwkReadCompletion.EndOfFile;

}
