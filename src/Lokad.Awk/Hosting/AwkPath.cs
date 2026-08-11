using System;
using System.Diagnostics.CodeAnalysis;

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

    private AwkPath()
    {
        Path = "/";
    }

    /// <summary>Gets the canonical absolute path text.</summary>
    public string Path { get; }

    /// <summary>Gets whether this path is the filesystem root.</summary>
    public bool IsRoot => Path == "/";

    /// <summary>Gets the final path segment, or an empty string for root.</summary>
    public string FileName => Path[(Path.LastIndexOf('/') + 1)..];

    /// <summary>Gets the parent path, or <see langword="null"/> for root.</summary>
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

    /// <summary>Gets the canonical root path.</summary>
    public static AwkPath Root { get; } = new();

    /// <summary>Gets the canonical temporary-directory path.</summary>
    public static AwkPath Tmp { get; } = new("/tmp");

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

    /// <summary>Returns whether this path is a strict parent of <paramref name="other"/>.</summary>
    public bool IsParentOf(AwkPath other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Path.Length == 1
            ? other.Path.Length > 1
            : other.Path.Length > Path.Length &&
              other.Path[Path.Length] == '/' &&
              other.Path.StartsWith(Path, StringComparison.Ordinal);
    }

    /// <summary>Returns whether this path equals or is a parent of <paramref name="other"/>.</summary>
    public bool IsEqualToOrParentOf(AwkPath other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Path == other.Path || IsParentOf(other);
    }

    /// <summary>Attempts to form the canonical child path.</summary>
    /// <param name="segment">The validated child segment.</param>
    /// <param name="child">The child path when the operation succeeds; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the child path was formed.</returns>
    public bool TryChild(
        AwkPathSegment segment,
        [NotNullWhen(true)] out AwkPath? child)
    {
        if (!AwkPathSegment.IsValid(segment.Name))
        {
            child = null;
            return false;
        }

        var combined = IsRoot ? "/" + segment.Name : Path + "/" + segment.Name;
        child = new AwkPath(combined);
        return true;
    }

    /// <summary>Attempts to form a canonical child path from segment text.</summary>
    /// <param name="name">The prospective child-segment text.</param>
    /// <param name="child">The child path when the operation succeeds; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the segment is valid and the child path was formed.</returns>
    public bool TryChild(
        string? name,
        [NotNullWhen(true)] out AwkPath? child)
    {
        if (!AwkPathSegment.TryCreate(name, out var segment))
        {
            child = null;
            return false;
        }

        return TryChild(segment, out child);
    }

    /// <inheritdoc />
    public override string ToString() => Path;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AwkPath other && Path == other.Path;

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);
}

/// <summary>Represents one validated component of an <see cref="AwkPath"/>.</summary>
public readonly record struct AwkPathSegment
{
    private readonly string? _name;

    /// <summary>Initializes a validated path segment.</summary>
    public AwkPathSegment(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!IsValid(name))
            throw new ArgumentException($"'{name}' is not a valid awk path segment.", nameof(name));

        _name = name;
    }

    /// <summary>Gets the segment text.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>Attempts to create a validated path segment.</summary>
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

    /// <summary>Returns whether <paramref name="name"/> is valid path-segment text.</summary>
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

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>Represents a failure to resolve a well-formed awk path.</summary>
/// <param name="message">The awk-shaped resolution error.</param>
public sealed class AwkPathException(string message) :
    InvalidOperationException(message ?? throw new ArgumentNullException(nameof(message)));
