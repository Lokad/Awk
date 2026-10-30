
namespace Lokad.Awk;

internal abstract record AwkSourceReference
{
    private AwkSourceReference()
    {
    }

    public static AwkSourceReference Inline(string script) =>
        new InlineSource(script);

    public static AwkSourceReference File(AwkFileOperand path) =>
        new FileSource(path);

    public static AwkSourceReference Include(AwkFileOperand path) =>
        new IncludeSource(path);

    internal sealed record InlineSource(string InlineScript) : AwkSourceReference;

    internal sealed record FileSource(AwkFileOperand ScriptPath) : AwkSourceReference;

    internal sealed record IncludeSource(AwkFileOperand ScriptPath) : AwkSourceReference;
}
