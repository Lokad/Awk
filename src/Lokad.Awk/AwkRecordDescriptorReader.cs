using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRecordDescriptorReader
{
    private readonly IAwkHost _host;
    private readonly CancellationToken _cancellationToken;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeResourceMonitor _resources;

    public AwkRecordDescriptorReader(
        IAwkHost host,
        CancellationToken cancellationToken,
        AwkRuntimeCounters counters,
        AwkRuntimeResourceMonitor resources)
    {
        _host = host;
        _cancellationToken = cancellationToken;
        _counters = counters;
        _resources = resources;
    }

    public AsyncLineReader CreateLineReader(AwkFileDescriptor descriptor) =>
        new(_host, descriptor, _cancellationToken, AwkIo.InputReadBatchLineCount, _counters, _resources);

    public async Task<AwkRecordReadBatch> ReadNextBatchAsync(AwkFileDescriptor descriptor)
    {
        _resources.RecordHostCall();
        var result = await _host.ReadLinesAsync(descriptor, AwkIo.InputReadBatchLineCount, _cancellationToken).ConfigureAwait(false);
        if (result.Failed)
            throw new AwkHostReadException(descriptor, result.Error);

        _resources.RecordBytesRead(result.Content.Length);
        return AwkRecordReadBatch.Success(result.Content, result.ReachedEof);
    }

    public async Task<AwkText> ReadToEndTextAsync(
        AwkFileDescriptor descriptor,
        AwkRuntimeLimits limits,
        AwkMemoryGovernor governor)
    {
        using var builder = new AwkUtf8Builder(limits, governor, AwkIo.InputReadBatchLineCount);
        await AppendToEndAsync(descriptor, builder).ConfigureAwait(false);
        return builder.CopyToAwkText(_counters);
    }

    public async Task AppendToEndAsync(
        AwkFileDescriptor descriptor,
        AwkUtf8Builder builder)
    {
        while (true)
        {
            _counters.RecordCancellationCheck();
            _cancellationToken.ThrowIfCancellationRequested();
            _resources.RecordHostCall();
            var result = await _host.ReadLinesAsync(descriptor, AwkIo.InputReadBatchLineCount, _cancellationToken)
                .ConfigureAwait(false);
            if (result.Failed)
                throw new AwkHostReadException(descriptor, result.Error);

            _resources.RecordBytesRead(result.Content.Length);
            builder.Append(result.Content);
            if (result.ReachedEof)
                return;
        }
    }
}

internal readonly struct AwkRecordReadBatch(ReadOnlyMemory<byte> content, bool reachedEof)
{
    public static AwkRecordReadBatch Success(ReadOnlyMemory<byte> content, bool reachedEof) =>
        new(content, reachedEof);

    public ReadOnlyMemory<byte> Content { get; } = content;

    public bool ReachedEof { get; } = reachedEof;

}
