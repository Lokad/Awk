using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class GsubExpression(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var regexPattern = await pattern.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldPattern = context.State.MemoryAccounting.ReserveTransient(
                AwkRuntimeMemory.EstimateTextBytes(regexPattern.Text));
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            using var heldReplacement = context.State.MemoryAccounting.ReserveTransient(replacementText.EstimatedRetainedMemoryBytes);
            var regex = context.State.CreateRegex(regexPattern);
            using var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
            var replaced = AwkRegexSubstitution.ReplaceAll(
                context.State,
                regex,
                resolvedTarget.Input,
                replacementText,
                AwkReplacementMode.Substitution,
                maxReplacements: 0,
                out var count);

            if (count > 0)
                resolvedTarget.AssignText(context.State, replaced);
            return AwkValue.FromNumber(count);
        }
    }

    private sealed class SubExpression(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var regexPattern = await pattern.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldPattern = context.State.MemoryAccounting.ReserveTransient(
                AwkRuntimeMemory.EstimateTextBytes(regexPattern.Text));
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            using var heldReplacement = context.State.MemoryAccounting.ReserveTransient(replacementText.EstimatedRetainedMemoryBytes);
            var regex = context.State.CreateRegex(regexPattern);
            using var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
            var replaced = AwkRegexSubstitution.ReplaceAll(
                context.State,
                regex,
                resolvedTarget.Input,
                replacementText,
                AwkReplacementMode.Substitution,
                maxReplacements: 1,
                out var count);

            if (count > 0)
                resolvedTarget.AssignText(context.State, replaced);
            return AwkValue.FromNumber(count > 0 ? 1 : 0);
        }
    }

    private sealed class MatchExpression(
        AwkExpression valueExpression,
        AwkExpression patternExpression,
        AwkArrayOperand? captureArray) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var inputValue = (await valueExpression.EvaluateAsync(context).ConfigureAwait(false)).AsScalarCopy();
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(inputValue.RetainedTextMemoryBytes);
            var pattern = await patternExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldPattern = context.State.MemoryAccounting.ReserveTransient(
                pattern.Text.EstimatedRetainedMemoryBytes);
            AwkArrayTarget? captureTarget = captureArray is null ? null :
                await captureArray.CaptureAsync(context).ConfigureAwait(false);
            using var heldTarget = context.State.MemoryAccounting.ReserveTransient(captureTarget?.RetainedBytes ?? 0);
            var input = context.State.FormatStringByteText(inputValue, heldInput);
            return AwkValue.FromNumber(AwkMatchRuntime.Execute(context.State, input, pattern, captureTarget));
        }
    }
}
