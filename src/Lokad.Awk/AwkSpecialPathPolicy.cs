using System;

namespace Lokad.Awk;

internal sealed class AwkSpecialPathPolicy(
    AwkFileDescriptor stdIn,
    AwkFileDescriptor stdOut,
    AwkFileDescriptor stdErr)
{
    private static readonly string[] InputAliases = ["-", "/dev/stdin", "/dev/fd/0"];

    public static bool IsStandardInput(AwkText path)
    {
        foreach (var alias in InputAliases)
            if (path.EqualsAscii(alias)) return true;
        return false;
    }

    public static bool IsStandardInput(string path)
    {
        foreach (var alias in InputAliases)
            if (string.Equals(path, alias, StringComparison.Ordinal)) return true;
        return false;
    }

    public bool TryResolveInput(string path, out AwkFileDescriptor descriptor)
    {
        descriptor = stdIn;
        return IsStandardInput(path);
    }

    public bool TryResolveOutput(string path, out AwkFileDescriptor descriptor)
    {
        descriptor = AwkFileDescriptor.StdOut;
        switch (path)
        {
            case "/dev/stdout":
            case "/dev/fd/1":
                descriptor = stdOut;
                return true;
            case "/dev/stderr":
            case "/dev/fd/2":
                descriptor = stdErr;
                return true;
            default:
                return false;
        }
    }

    public static void ThrowIfUnsupported(string path)
    {
        if (path.StartsWith("/inet/", StringComparison.Ordinal))
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedInetSpecialFile, AwkDiagnosticId.UnsupportedPolicy);

        if (path.StartsWith("/dev/fd/", StringComparison.Ordinal) &&
            path is not ("/dev/fd/0" or "/dev/fd/1" or "/dev/fd/2"))
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorUnsupportedAwkFileDescriptorSpecialFile,
                AwkDiagnosticId.UnsupportedPolicy);
        }
    }
}
