namespace Lokad.Awk;

internal static class AwkHostPathPolicy
{
    public static void ThrowIfDenied(AwkOpenedFile opened)
    {
        if (opened.Error.Kind == AwkOperationErrorKind.PolicyDenied)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorPathOutsideExecutionRoot,
                AwkDiagnosticId.UnsupportedPolicy);
        }
    }
}
