using System;

namespace Lokad.Awk;

/// <summary>
/// Canonical absolute path used by Lokad.Awk hosts.
/// </summary>
public sealed class AwkPath
{
    public AwkPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!IsValid(path))
        {
            throw new ArgumentException($"'{path}' is not a valid absolute awk path.", nameof(path));
        }

        Path = path;
    }

    private AwkPath()
    {
        Path = "/";
    }

    public string Path { get; }

    public bool IsRoot => Path == "/";

    public string FileName => Path[(Path.LastIndexOf('/') + 1)..];

    public AwkPath? Parent
    {
        get
        {
            if (IsRoot)
            {
                return null;
            }

            var slash = Path.LastIndexOf('/');
            return slash == 0 ? Root : new AwkPath(Path[..slash]);
        }
    }

    public static AwkPath Root { get; } = new();

    public static AwkPath Tmp { get; } = new("/tmp");

    public bool HasExtension(string ext)
    {
        ArgumentNullException.ThrowIfNull(ext);
        return Path.EndsWith(ext, StringComparison.Ordinal);
    }

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

        foreach (var segment in path[1..].Split('/'))
        {
            if (segment.Length == 0 || segment is "." or "..")
            {
                return false;
            }

            foreach (var c in segment)
            {
                if (IsForbiddenChar(c))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool IsForbiddenChar(char c) => c is <= '\x1f' or '\x7f';

    public bool IsParentOf(AwkPath other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Path.Length == 1
            ? other.Path.Length > 1
            : other.Path.Length > Path.Length &&
              other.Path[Path.Length] == '/' &&
              other.Path.StartsWith(Path, StringComparison.Ordinal);
    }

    public bool IsEqualToOrParentOf(AwkPath other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Path == other.Path || IsParentOf(other);
    }

    public AwkPath? TryChild(AwkPathSegment segment)
    {
        if (!AwkPathSegment.IsValid(segment.Name))
            return null;

        var combined = IsRoot ? "/" + segment.Name : Path + "/" + segment.Name;
        return IsValid(combined) ? new AwkPath(combined) : null;
    }

    public AwkPath? TryChild(string? name) =>
        AwkPathSegment.TryCreate(name, out var segment) ? TryChild(segment) : null;

    public AwkPath Child(AwkPathSegment segment) =>
        TryChild(segment) ?? throw new ArgumentException($"'{segment.Name}' is not a valid child path segment.", nameof(segment));

    public AwkPath Child(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return TryChild(name) ?? throw new ArgumentException($"'{name}' is not a valid child path segment.", nameof(name));
    }

    public override string ToString() => Path;

    public override bool Equals(object? obj) => obj is AwkPath other && Path == other.Path;

    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);
}

public readonly record struct AwkPathSegment
{
    private readonly string? _name;

    public AwkPathSegment(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!IsValid(name))
            throw new ArgumentException($"'{name}' is not a valid awk path segment.", nameof(name));

        _name = name;
    }

    public string Name => _name ?? string.Empty;

    public static bool TryCreate(string? name, out AwkPathSegment segment)
    {
        if (name is not null && IsValid(name))
        {
            segment = new AwkPathSegment(name);
            return true;
        }

        segment = default;
        return false;
    }

    public static bool IsValid(string? name)
    {
        if (string.IsNullOrEmpty(name) || name is "." or "..")
            return false;

        for (var index = 0; index < name.Length; index++)
        {
            if (name[index] == '/' || AwkPath.IsForbiddenChar(name[index]))
                return false;
        }

        return true;
    }

    public override string ToString() => Name;
}

public sealed class AwkPathException(string message) :
    InvalidOperationException(message ?? throw new ArgumentNullException(nameof(message)));
