using System;

namespace Lokad.Awk;

internal sealed class AwkSpecialPathPolicy(
    AwkFileDescriptor stdIn,
    AwkFileDescriptor stdOut,
    AwkFileDescriptor stdErr)
{
    public bool TryResolveInput(string path, out AwkFileDescriptor descriptor)
    {
        descriptor = stdIn;
        switch (path)
        {
            case "/dev/stdin":
            case "/dev/fd/0":
                return true;
            default:
                return false;
        }
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
