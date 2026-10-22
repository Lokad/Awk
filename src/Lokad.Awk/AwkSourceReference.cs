
namespace Lokad.Awk;

internal abstract record AwkSourceReference
{
    private AwkSourceReference()
    {
    }

    public static AwkSourceReference Inline(string script) =>
        new InlineSource(script);

    public static AwkSourceReference File(AwkResolvedPath path) =>
        new FileSource(path);

    public static AwkSourceReference Include(AwkResolvedPath path) =>
        new IncludeSource(path);

    internal sealed record InlineSource(string InlineScript) : AwkSourceReference;

    internal sealed record FileSource(AwkResolvedPath ScriptPath) : AwkSourceReference;

    internal sealed record IncludeSource(AwkResolvedPath ScriptPath) : AwkSourceReference;
}
