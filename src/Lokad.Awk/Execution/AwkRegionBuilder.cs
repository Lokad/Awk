namespace Lokad.Awk;

// A key keeps its separator only through assembly; bytes survive until consumption.
// Output can reuse an idle buffer after the preceding operation has retired it.
internal readonly struct AwkRegionBuilder(AwkUtf8Builder buffer, AwkText separator)
{
    private readonly AwkUtf8Builder? _buffer = buffer;
    private readonly AwkText? _separator = separator;

    internal AwkUtf8Builder Buffer => _buffer ??
        throw new InvalidOperationException("Uninitialized region builder.");
    internal AwkText Separator => _separator ?? AwkText.Empty;
}
