namespace Lokad.Awk;

/// <summary>Supplies original path spelling and namespace context for a host file open.</summary>
/// <remarks>
/// The host traverses this path before interpreting dot segments, links or trailing slashes.
/// It enforces the optional accessible root against its resolved namespace before effects.
/// Lexical normalization is not proof of access authorization.
/// </remarks>
public sealed record AwkPathRequest
{
    /// <summary>Initializes an unresolved request in the host's namespace.</summary>
    public AwkPathRequest(string path, AwkPath currentDirectory, AwkPath? accessiblePathRoot = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(currentDirectory);
        if (!AwkRawPath.IsValid(path))
            throw new ArgumentException("Invalid awk path text.", nameof(path));
        Path = path;
        CurrentDirectory = currentDirectory;
        AccessiblePathRoot = accessiblePathRoot;
    }

    /// <summary>Gets the original path text, including traversal-significant spelling.</summary>
    public string Path { get; }

    /// <summary>Gets the explicit directory against which relative paths are traversed.</summary>
    public AwkPath CurrentDirectory { get; }

    /// <summary>Gets the host-enforced namespace boundary, or null when none was requested.</summary>
    public AwkPath? AccessiblePathRoot { get; }
}
