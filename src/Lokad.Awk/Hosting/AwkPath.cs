namespace Lokad.Awk;

/// <summary>
/// Canonical absolute path used by Lokad.Awk hosts.
/// </summary>
public sealed class AwkPath
{
    /// <summary>Initializes a canonical absolute path.</summary>
    public AwkPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!IsValid(path))
        {
            throw new ArgumentException($"'{path}' is not a valid absolute awk path.", nameof(path));
        }

        Path = path;
    }

    /// <summary>Gets the canonical absolute path text.</summary>
    public string Path { get; }

    /// <summary>Gets the canonical root path.</summary>
    public static AwkPath Root { get; } = new("/");

    /// <summary>Returns whether <paramref name="path"/> is canonical absolute awk path text.</summary>
    public static bool IsValid(string? path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/')
        {
            return false;
        }

        if (path == "/")
        {
            return true;
        }

        if (path[^1] == '/')
        {
            return false;
        }

        var segmentStart = 1;
        for (var index = 1; index <= path.Length; index++)
        {
            if (index < path.Length && path[index] != '/')
            {
                if (IsForbiddenChar(path[index]))
                    return false;

                continue;
            }

            var segmentLength = index - segmentStart;
            if (segmentLength == 0 ||
                segmentLength == 1 && path[segmentStart] == '.' ||
                segmentLength == 2 && path[segmentStart] == '.' && path[segmentStart + 1] == '.')
            {
                return false;
            }

            segmentStart = index + 1;
        }

        return true;
    }

    internal static bool IsForbiddenChar(char c) => c is <= '\x1f' or '\x7f';

    /// <inheritdoc />
    public override string ToString() => Path;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AwkPath other && Path == other.Path;

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);
}
