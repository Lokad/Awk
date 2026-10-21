namespace Lokad.Awk;

internal static class AwkRegexScalarBoundary
{
    public static void ThrowIfSplit(ReadOnlySpan<char> input, int start, int length) =>
        ThrowIfUnaligned(IsBoundary(input, start) && IsBoundary(input, start + length));

    public static void ThrowIfUnaligned(bool isAligned)
    {
        if (!isAligned)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorRegexScalarBoundary,
                AwkDiagnosticId.RuntimeInvalidRegex);
        }
    }

    private static bool IsBoundary(ReadOnlySpan<char> input, int index) =>
        index == input.Length || !char.IsLowSurrogate(input[index]);
}
