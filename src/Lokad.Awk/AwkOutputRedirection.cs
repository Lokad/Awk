namespace Lokad.Awk;

internal enum AwkOutputRedirectionKind
{
    Write,
    Append,
    Pipe,
    Coprocess
}

internal readonly struct AwkRedirectedOutput(
    string target, AwkOutputRedirectionKind kind, ReadOnlyMemory<byte> content)
{
    public string Target => target ?? string.Empty;
    public AwkOutputRedirectionKind Kind { get; } = kind;
    public ReadOnlyMemory<byte> Content { get; } = content;
}
