using System;

namespace Lokad.Awk;

internal enum AwkOutputRedirectionKind
{
    Write,
    Append,
    Pipe,
    Coprocess
}

internal readonly struct AwkOutputRedirection(AwkExpression? target, AwkOutputRedirectionKind kind)
{
    public AwkExpression Target => target ??
        throw new InvalidOperationException("The default output redirection has no target.");

    public AwkOutputRedirectionKind Kind { get; } = kind;
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

internal abstract record AwkOutputTarget
{
    private AwkOutputTarget()
    {
    }

    public static AwkOutputTarget Standard { get; } = new StandardOutputTarget();

    public static AwkOutputTarget Redirected(AwkOutputRedirection redirection) => new RedirectedOutputTarget(redirection);

    public bool IsRedirected => this is RedirectedOutputTarget;

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

internal readonly record struct AwkResolvedOutputRedirection(
    AwkRedirectionPath Path,
    AwkOutputRedirectionKind Kind);

internal readonly record struct AwkOptionalResolvedOutputRedirection
{
    private readonly AwkResolvedOutputRedirection _value;

    private AwkOptionalResolvedOutputRedirection(AwkResolvedOutputRedirection value)
    {
        _value = value;
        HasValue = true;
    }

    public static AwkOptionalResolvedOutputRedirection None => default;

    public bool HasValue { get; }

    public static AwkOptionalResolvedOutputRedirection Specified(AwkResolvedOutputRedirection value) => new(value);

    public bool TryGetValue(out AwkResolvedOutputRedirection value)
    {
        value = _value;
        return HasValue;
    }
}
