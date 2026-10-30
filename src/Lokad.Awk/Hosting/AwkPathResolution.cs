using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal readonly struct AwkFileOperand(ReadOnlyMemory<byte> display, AwkPathRequest? request)
{
    public ReadOnlyMemory<byte> Display { get; } = display;

    public AwkPathRequest Request => request ??
        throw new InvalidOperationException("The default path has no host request.");

    public override string ToString() => Request.Path;
}

/// <summary>Provides invocation directory selection; file traversal belongs to the host.</summary>
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

    internal static AwkFileOperand PrepareArgument(string raw, AwkPath currentDirectory,
        ReadOnlyMemory<byte> display = default, AwkPath? accessiblePathRoot = null)
    {
        if (!AwkRawPath.IsValid(raw)) throw new FormatException("Invalid awk path text.");
        var request = new AwkPathRequest(raw, currentDirectory, accessiblePathRoot);
        return new AwkFileOperand(display.IsEmpty ? AwkUtf8Codec.Encode(raw) : display, request);
    }

    internal static AwkFileOperand PrepareRuntimeArgument(string raw, AwkPath currentDirectory,
        ReadOnlyMemory<byte> display = default, AwkPath? accessiblePathRoot = null)
    {
        try
        {
            return PrepareArgument(raw, currentDirectory, display, accessiblePathRoot);
        }
        catch (Exception exception) when (exception is FormatException or AwkPathException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidPath, AwkDiagnosticId.HostIoFailure);
        }
    }
}
