using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class RegexArgumentExpression(AwkExpression value) : AwkExpression
    {
        internal override AwkRegexPatternOrigin RegexPatternOrigin => value.RegexPatternOrigin;

        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            context.State.CreateDeferredTextValue(await value.ResolveByteTextAsync(context).ConfigureAwait(false));
    }

    private sealed class SubstitutionExpression(AwkExpression pattern, AwkExpression replacement,
        AwkSubstitutionTarget target, bool global) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var regexPattern = await pattern.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldPattern = context.State.MemoryAccounting.ReserveTransient(
                AwkRuntimeMemory.EstimateTextBytes(regexPattern.Text));
            var replacementValue = (await replacement.EvaluateAsync(context).ConfigureAwait(false)).AsScalarCopy();
            using var heldReplacement = context.State.MemoryAccounting.ReserveTransient(replacementValue.RetainedTextMemoryBytes);
            using var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(AwkSubstitutionRuntime.Execute(
                context.State, regexPattern, replacementValue, resolvedTarget, global));
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
