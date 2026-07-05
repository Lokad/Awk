namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateSprintf(AwkBuiltinContext context)
    {
        if (context.ArgumentCount == 0)
            return AwkValue.FromText(string.Empty);

        var formatText = context.GetString(0);
        return context.State.CreateByteTextValue(AwkFormattingRuntime.FormatPrintfStrictByteText(
            formatText,
            new AwkBuiltinContextPrintfArguments(context, startIndex: 1),
            context.State.Counters,
            context.State.MemoryGovernor));
    }
}
