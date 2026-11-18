namespace Lokad.Awk;

internal sealed class AwkRecordDescriptorReader(
    IAwkHost host, CancellationToken cancellationToken, AwkRuntimeOptions options)
{
    public AwkByteReader CreateByteReader(AwkFileDescriptor descriptor) =>
        new(host, descriptor, cancellationToken, options);
}
