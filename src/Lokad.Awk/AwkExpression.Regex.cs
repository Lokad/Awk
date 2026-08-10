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
            var patternText = await pattern.ResolveStringAsync(context).ConfigureAwait(false);
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.CreateRegex(new AwkRegexPattern(patternText));
            var input = await target.ResolveInputTextAsync(context).ConfigureAwait(false);
            var replaced = AwkRegexSubstitution.ReplaceAll(
                context.State,
                regex,
                input,
                replacementText,
                AwkReplacementMode.Substitution,
                maxReplacements: 0,
                out var count);

            await target.AssignTextAsync(context, replaced).ConfigureAwait(false);
            return AwkValue.FromNumber(count);
        }
    }

    private sealed class SubExpression(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var patternText = await pattern.ResolveStringAsync(context).ConfigureAwait(false);
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.CreateRegex(new AwkRegexPattern(patternText));
            var input = await target.ResolveInputTextAsync(context).ConfigureAwait(false);
            var replaced = AwkRegexSubstitution.ReplaceAll(
                context.State,
                regex,
                input,
                replacementText,
                AwkReplacementMode.Substitution,
                maxReplacements: 1,
                out var count);

            await target.AssignTextAsync(context, replaced).ConfigureAwait(false);
            return AwkValue.FromNumber(count > 0 ? 1 : 0);
        }
    }

    private sealed class MatchExpression(
        AwkExpression valueExpression,
        AwkExpression patternExpression,
        AwkOptionalIdentifier captureArrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            static void AssignMatchVariables(
                AwkRuntimeState state,
                bool success,
                int index,
                int length)
            {
                state.AssignVariableValue(
                    new AwkSpecialVariableName(AwkSpecialVariableId.RegexStart),
                    AwkValue.FromNumber(success ? index + 1 : 0));
                state.AssignVariableValue(
                    new AwkSpecialVariableName(AwkSpecialVariableId.RegexLength),
                    AwkValue.FromNumber(success ? length : -1));
            }

            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.CreateRegex(new AwkRegexPattern(
                await patternExpression.ResolveStringAsync(context).ConfigureAwait(false)));
            if (!captureArrayName.HasValue)
            {
                var position = context.State.MatchRegexPosition(regex, input);
                AssignMatchVariables(context.State, position.Success, position.Index, position.Length);
                return AwkValue.FromNumber(position.Success ? position.Index + 1 : 0);
            }

            var match = context.State.MatchRegexCaptureValue(regex, input);
            var arrayName = new AwkArrayName(captureArrayName.Value);
            context.State.ReplaceMatchCaptureArray(arrayName, match.Groups);
            AssignMatchVariables(context.State, match.Success, match.Index, match.Length);

            return AwkValue.FromNumber(match.Success ? match.Index + 1 : 0);
        }
    }
}
