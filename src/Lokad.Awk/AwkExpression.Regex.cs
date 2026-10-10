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
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.CreateRegex(regexPattern);
            var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
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
            var replacementText = await replacement.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.CreateRegex(regexPattern);
            var resolvedTarget = await target.ResolveAsync(context).ConfigureAwait(false);
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
            var regex = context.State.CreateRegex(
                await patternExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false));
            if (!captureArrayName.HasValue)
            {
                var position = context.State.MatchRegexPosition(regex, input);
                AssignMatchVariables(context.State, position.Success, position.Index, position.Length);
                return AwkValue.FromNumber(position.Success ? position.Index + 1 : 0);
            }

            using var leasedMatch = context.State.MatchRegexCaptureValue(regex, input);
            var match = leasedMatch.Match;
            var arrayName = new AwkArrayName(captureArrayName.Value);
            context.State.ReplaceMatchCaptureArray(arrayName, leasedMatch);
            AssignMatchVariables(context.State, match.Success, match.Index, match.Length);

            return AwkValue.FromNumber(match.Success ? match.Index + 1 : 0);
        }
    }
}
