using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkResolvedPath(ReadOnlyMemory<byte> display, AwkPath? absolute)
{
    public ReadOnlyMemory<byte> Display { get; } = display;

    public AwkPath Absolute => absolute ??
        throw new InvalidOperationException("The default resolved path has no absolute path.");

    public override string ToString() => Absolute.Path;
}

/// <summary>Provides canonical path resolution at the awk host boundary.</summary>
public static class AwkPathResolution
{
    /// <summary>Resolves the current directory from the first exported <c>PWD</c> if canonical, or returns root.</summary>
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

    internal static AwkResolvedPath ResolveArgument(string raw, AwkPath currentDirectory,
        ReadOnlyMemory<byte> display = default)
    {
        var absolute = AwkRawPath.Parse(raw, CultureInfo.InvariantCulture).ToAbsolute(currentDirectory);
        return new AwkResolvedPath(display.IsEmpty ? AwkUtf8Codec.Encode(raw) : display, absolute);
    }

    internal static AwkResolvedPath ResolveRuntimeArgument(string raw, AwkPath currentDirectory,
        ReadOnlyMemory<byte> display = default)
    {
        try
        {
            return ResolveArgument(raw, currentDirectory, display);
        }
        catch (Exception exception) when (exception is FormatException or AwkPathException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidPath, AwkDiagnosticId.HostIoFailure);
        }
    }
}
