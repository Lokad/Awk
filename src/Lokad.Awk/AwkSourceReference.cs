
namespace Lokad.Awk;

internal abstract record AwkSourceReference
{
    private AwkSourceReference()
    {
    }

    /// <summary>Gets how the source must be loaded and diagnosed.</summary>
    public abstract AwkSourceReferenceKind Kind { get; }

    public static AwkSourceReference Inline(string script) =>
        new InlineSource(script);

    public static AwkSourceReference File(AwkResolvedPath path) =>
        new FileSource(path);

    public static AwkSourceReference Include(AwkResolvedPath path) =>
        new IncludeSource(path);

    internal sealed record InlineSource(string InlineScript) : AwkSourceReference
    {
        public override AwkSourceReferenceKind Kind => AwkSourceReferenceKind.Inline;
    }

    internal sealed record FileSource(AwkResolvedPath ScriptPath) : AwkSourceReference
    {
        public override AwkSourceReferenceKind Kind => AwkSourceReferenceKind.File;
    }

    internal sealed record IncludeSource(AwkResolvedPath ScriptPath) : AwkSourceReference
    {
        public override AwkSourceReferenceKind Kind => AwkSourceReferenceKind.Include;
    }
}

internal enum AwkSourceReferenceKind
{
    Inline,
    File,
    Include
}
