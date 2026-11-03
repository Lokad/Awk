namespace Lokad.Awk;

internal static class AwkSubstitutionRuntime
{
    internal static AwkValue Gensub(AwkEvaluationContext context, AwkRegexPattern pattern,
        AwkValue replacement, AwkValue how, AwkValue input)
    {
        var state = context.State;
        state.ThrowIfCancellationRequested();
        var regex = state.CreateRegex(pattern);
        using var formatting = state.MemoryAccounting.CreateTransientLease();
        var inputText = state.FormatStringByteText(input, formatting);
        var replacementText = state.FormatStringByteText(replacement, formatting);
        var selector = how.IsNumberValue ? AwkText.Empty : state.FormatStringByteText(how, formatting);
        var bytes = selector.AsSpan(state.CancellationCheckpoint);
        AwkText result;
        if (!bytes.IsEmpty && bytes[0] is (byte)'g' or (byte)'G')
            result = AwkRegexSubstitution.ReplaceAll(state, regex, inputText, replacementText,
                AwkReplacementMode.Gensub, maxReplacements: 0, out _);
        else
        {
            var number = how.AsNumber(state.CancellationCheckpoint);
            if (number <= 0 && context.TryGetExecutionContext(out var execution))
            {
                var text = state.FormatStringByteText(how, formatting).AsString(state.Counters, state.CancellationCheckpoint);
                execution.Output.ReportDiagnostic(new AwkDiagnostic(default,
                    AwkMessages.FormatGensubThirdArgumentWarningText(text), AwkDiagnosticId.WarningRuntime));
            }
            // A subject cannot contain int.MaxValue distinct scalar positions.
            // Keep an out-of-range selector beyond its matches instead of wrapping to 1.
            var occurrence = double.IsNaN(number) || number >= int.MaxValue ? int.MaxValue
                : number < 1 ? 1 : (int)number;
            result = AwkRegexSubstitution.ReplaceOccurrence(state, regex, inputText, replacementText,
                AwkReplacementMode.Gensub, occurrence, out _);
        }
        return state.CreateByteTextValue(result);
    }

    internal static int Execute(AwkRuntimeState state, AwkRegexPattern pattern, AwkValue replacement,
        AwkResolvedSubstitutionTarget target, bool global)
    {
        using var heldKey = state.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
        state.ThrowIfCancellationRequested();
        var regex = state.CreateRegex(pattern);
        var inputValue = target.ReadValue(state);
        using var heldInput = state.MemoryAccounting.ReserveTransient(inputValue.RetainedTextMemoryBytes);
        var input = state.FormatStringByteText(inputValue, heldInput);
        using var heldReplacement = state.MemoryAccounting.CreateTransientLease();
        var replacementText = state.FormatStringByteText(replacement, heldReplacement);
        var replaced = AwkRegexSubstitution.ReplaceAll(state, regex, input, replacementText,
            AwkReplacementMode.Substitution, global ? 0 : 1, out var count);
        if (count > 0) target.AssignText(state, replaced);
        return count;
    }
}
