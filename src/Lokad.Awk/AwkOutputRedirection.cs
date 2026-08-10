using System;

namespace Lokad.Awk;

internal enum AwkOutputRedirectionKind
{
    Write,
    Append,
    Pipe,
    Coprocess
}

internal readonly struct AwkOutputRedirection(AwkExpression target, AwkOutputRedirectionKind kind)
{
    public AwkExpression Target { get; } = target;

    public AwkOutputRedirectionKind Kind { get; } = kind;
}

internal readonly record struct AwkRedirectionPath
{
    private readonly string _text;

    public AwkRedirectionPath(string text)
    {
        _text = text;
    }

    public string Text => _text ?? string.Empty;

    public override string ToString() => Text;
}

internal abstract record AwkOutputTarget
{
    private AwkOutputTarget()
    {
    }

    public static AwkOutputTarget Standard { get; } = new StandardOutputTarget();

    public static AwkOutputTarget Redirected(AwkOutputRedirection redirection) => new RedirectedOutputTarget(redirection);

    public bool IsRedirected => this is RedirectedOutputTarget;

    public AwkOutputRedirection Redirection =>
        this is RedirectedOutputTarget redirected
            ? redirected.OutputRedirection
            : throw new InvalidOperationException("No output redirection is present.");

    public bool TryGetRedirection(out AwkOutputRedirection redirection)
    {
        if (this is RedirectedOutputTarget redirected)
        {
            redirection = redirected.OutputRedirection;
            return true;
        }

        redirection = default;
        return false;
    }

    private sealed record StandardOutputTarget : AwkOutputTarget;

    private sealed record RedirectedOutputTarget(AwkOutputRedirection OutputRedirection) : AwkOutputTarget;
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
