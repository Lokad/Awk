using System;

namespace Lokad.Awk;

internal enum AwkOutputRedirectionKind
{
    Write,
    Append,
    Pipe,
    Coprocess
}

internal readonly record struct AwkRedirectionPath
{
    private readonly string? _text;

    public AwkRedirectionPath(string text)
    {
        _text = text.Length == 0 ? null : text;
    }

    public string Text => _text ?? string.Empty;

    public override string ToString() => Text;
}

internal readonly struct AwkRedirectedOutput
{
    public AwkRedirectedOutput(AwkRedirectionPath path, AwkOutputRedirectionKind kind, ReadOnlyMemory<byte> content)
    {
        RedirectionPath = path;
        Kind = kind;
        Content = content;
    }

    public AwkRedirectionPath RedirectionPath { get; }

    public AwkOutputRedirectionKind Kind { get; }

    public ReadOnlyMemory<byte> Content { get; }
}

internal readonly record struct AwkResolvedOutputRedirection(
    AwkRedirectionPath Path,
    AwkOutputRedirectionKind Kind);
