namespace Lokad.Awk;

internal static class AwkRegexScalarBoundary
{
    public static void ThrowIfUnaligned(bool isAligned)
    {
        if (!isAligned)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexScalarBoundary,
                AwkDiagnosticId.RuntimeInvalidRegex);
        }
    }

}
