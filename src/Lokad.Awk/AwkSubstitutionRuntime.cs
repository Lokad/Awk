namespace Lokad.Awk;

internal static class AwkSubstitutionRuntime
{
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
