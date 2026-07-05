namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateSystem(AwkBuiltinContext context)
    {
        throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedSystem, AwkDiagnosticId.UnsupportedPolicy);
    }

    private static AwkValue EvaluateInternationalization(AwkBuiltinContext context)
    {
        throw new AwkRuntimeException(
            AwkMessages.ErrorUnsupportedInternationalization,
            AwkDiagnosticId.UnsupportedPolicy);
    }
}
