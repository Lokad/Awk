using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkResolvedPath(ReadOnlyMemory<byte> display, AwkPath absolute)
{
    public ReadOnlyMemory<byte> Display { get; } = display;

    public AwkPath Absolute { get; } = absolute;

    public override string ToString() => Absolute.Path;
}

public static class AwkPathResolution
{
    public static AwkPath ResolveCurrentDirectory(IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        foreach (var entry in environment)
        {
            if (!string.Equals(entry.Name, "PWD", StringComparison.Ordinal))
            {
                continue;
            }

            if (AwkPath.IsValid(entry.Value))
            {
                return new AwkPath(entry.Value);
            }

            break;
        }

        return AwkPath.Root;
    }

    internal static AwkResolvedPath ResolveArgument(string raw, AwkPath currentDirectory)
    {
        var absolute = AwkRawPath.Parse(raw).ToAbsolute(currentDirectory);
        return new AwkResolvedPath(AwkUtf8Codec.Encode(raw), absolute);
    }

    internal static AwkResolvedPath FromAbsolute(AwkPath absolute) =>
        new(AwkUtf8Codec.Encode(absolute.Path), absolute);

    public static string ResolveAbsolute(string path, string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(currentDirectory);
        var baseDirectory = string.IsNullOrEmpty(currentDirectory) || currentDirectory[0] != '/'
            ? "/"
            : currentDirectory;

        if (string.IsNullOrEmpty(path))
        {
            return baseDirectory;
        }

        var segments = new List<string>();
        if (path[0] != '/')
        {
            AppendSegments(baseDirectory.AsSpan(), segments);
        }

        AppendSegments(path.AsSpan(), segments);
        return BuildAbsolutePath(segments);
    }

    private static void AppendSegments(ReadOnlySpan<char> path, List<string> segments)
    {
        var start = 0;
        for (var index = 0; index <= path.Length; index++)
        {
            if (index < path.Length && path[index] != '/')
            {
                continue;
            }

            if (index > start)
            {
                var segment = path.Slice(start, index - start);
                if (segment.Length == 1 && segment[0] == '.')
                {
                }
                else if (segment.Length == 2 && segment[0] == '.' && segment[1] == '.')
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                }
                else
                {
                    segments.Add(segment.ToString());
                }
            }

            start = index + 1;
        }
    }

    private static string BuildAbsolutePath(IReadOnlyList<string> segments) =>
        segments.Count == 0 ? "/" : "/" + string.Join("/", segments);
}
