using System;
namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateGensub(AwkBuiltinContext context)
    {
        if (context.ArgumentCount is < 3 or > 4)
            return AwkValue.FromText(string.Empty);

        var pattern = context.GetString(0);
        var replacement = context.GetByteText(1);
        var how = context.GetValue(2);
        var input = context.ArgumentCount == 4 ? context.GetByteText(3) : context.State.CurrentRecordByteText;
        return context.State.CreateByteTextValue(ApplyGensub(
            context,
            context.State.CreateRegex(new AwkRegexPattern(pattern)),
            replacement,
            how,
            input));
    }

    private static AwkText ApplyGensub(
        AwkBuiltinContext context,
        AwkRegex regex,
        AwkText replacement,
        AwkValue how,
        AwkText input)
    {
        var selector = how.AsString();
        if (selector.StartsWith("g", StringComparison.OrdinalIgnoreCase))
        {
            return AwkRegexSubstitution.ReplaceAll(
                context.State,
                regex,
                input,
                replacement,
                AwkReplacementMode.Gensub,
                maxReplacements: 0,
                out _);
        }

        var selectorNumber = how.AsNumber();
        if (selectorNumber <= 0 &&
            context.TryGetExecutionContext(out var execution))
        {
            execution.Output.ReportDiagnostic(new AwkDiagnostic(
                new AwkTextSpan(0, 0),
                AwkMessages.FormatGensubThirdArgumentWarningText(selector),
                AwkDiagnosticId.WarningRuntime));
        }

        var occurrence = Math.Max(1, (int)selectorNumber);

        return AwkRegexSubstitution.ReplaceOccurrence(
            context.State,
            regex,
            input,
            replacement,
            AwkReplacementMode.Gensub,
            occurrence,
            out _);
    }
}
