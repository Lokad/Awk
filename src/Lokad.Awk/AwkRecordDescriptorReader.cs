namespace Lokad.Awk;

internal sealed class AwkRecordDescriptorReader(
    IAwkHost host, CancellationToken cancellationToken, AwkRuntimeResourceMonitor resources,
    bool exposeHostErrorDetails = true, int maxHostErrorBytes = 4 << 10)
{
    public AwkByteReader CreateByteReader(AwkFileDescriptor descriptor) =>
        new(host, descriptor, cancellationToken, resources, exposeHostErrorDetails, maxHostErrorBytes);
}
