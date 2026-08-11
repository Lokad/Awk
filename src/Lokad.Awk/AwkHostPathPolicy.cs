namespace Lokad.Awk;

internal static class AwkHostPathPolicy
{
    public static void ThrowIfDenied(AwkPath path, AwkPath? accessibleRoot)
    {
        if (accessibleRoot is not null && !accessibleRoot.IsEqualToOrParentOf(path))
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorPathOutsideExecutionRoot,
                AwkDiagnosticId.UnsupportedPolicy);
        }
    }
}
