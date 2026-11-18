namespace Lokad.Awk.Examples;

// Conventions for the link-free in-memory example/test hosts, not runtime policy.
// Hosts with directories or links must traverse their namespace before interpreting
// dot segments and enforce the accessible root against that resolved identity.
internal static class MemoryHostPaths
{
    internal static AwkFileDescriptor NullDescriptor { get; } = new(int.MaxValue);
    internal static AwkPath NullPath { get; } = new("/dev/null");

    internal static AwkPath Resolve(AwkPathRequest request)
    {
        List<string> segments = request.Path.StartsWith('/') ? [] :
            [.. request.CurrentDirectory.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)];
        foreach (var segment in request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".") continue;
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }
        return new AwkPath('/' + string.Join('/', segments));
    }

    internal static bool IsWithin(AwkPath root, AwkPath path) =>
        path.Path == root.Path || path.Path.StartsWith(root.Path == "/" ? "/" : root.Path + '/', StringComparison.Ordinal);
}
