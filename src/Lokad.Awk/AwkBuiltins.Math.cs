using System.Globalization;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static void ReportMathWarning(
        AwkBuiltinContext context,
        string name,
        double argument,
        bool outOfRange)
    {
        if (!context.TryGetExecutionContext(out var execution))
            return;

        var number = double.IsNegativeInfinity(argument)
            ? "-inf"
            : argument.ToString("G6", CultureInfo.InvariantCulture).Replace('E', 'e');
        var message = outOfRange
            ? AwkMessages.FormatMathRangeWarning(name, number)
            : AwkMessages.FormatMathNegativeArgumentWarning(name, number);
        execution.Output.ReportDiagnostic(new AwkDiagnostic(
            new AwkTextSpan(0, 0), message, AwkDiagnosticId.WarningRuntime));
    }
}
