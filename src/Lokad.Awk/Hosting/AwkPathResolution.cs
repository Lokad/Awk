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
            ? AwkPath.Root
            : new AwkPath(currentDirectory);

        if (string.IsNullOrEmpty(path))
            return baseDirectory.Path;

        return AwkRawPath.Parse(path).ToAbsolute(baseDirectory).Path;
    }
}
