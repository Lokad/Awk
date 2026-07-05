using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static readonly AwkText MatchStartCaptureKeyPart = AwkText.FromSourceLiteralString("start");
    private static readonly AwkText MatchLengthCaptureKeyPart = AwkText.FromSourceLiteralString("length");

    private sealed class GsubExpression(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var patternText = await pattern.ResolveStringAsync(context).ConfigureAwait(false);
            var replacementText = await replacement.ResolveStringAsync(context).ConfigureAwait(false);
            var regex = context.State.Regex.Create(new AwkRegexPattern(patternText));
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
            var replacementText = await replacement.ResolveStringAsync(context).ConfigureAwait(false);
            var regex = context.State.Regex.Create(new AwkRegexPattern(patternText));
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
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var regex = context.State.Regex.Create(new AwkRegexPattern(
                await patternExpression.ResolveStringAsync(context).ConfigureAwait(false)));
            var match = context.State.Regex.MatchValue(regex, input);
            context.State.Variables.AssignValue(
                new AwkSpecialVariableName(AwkSpecialVariableId.RegexStart),
                AwkValue.FromNumber(match.Success ? match.Index + 1 : 0));
            context.State.Variables.AssignValue(
                new AwkSpecialVariableName(AwkSpecialVariableId.RegexLength),
                AwkValue.FromNumber(match.Success ? match.Length : -1));

            if (captureArrayName.HasValue)
            {
                var arrayName = new AwkArrayName(captureArrayName.Value);
                if (match.Success)
                    context.State.ThrowIfRegexCaptureBytesExceeded(EstimateCaptureBytes(match));

                context.State.Arrays.Clear(arrayName);
                if (match.Success)
                {
                    for (var index = 0; index < match.Groups.Count; index++)
                    {
                        var ordinalKey = AwkArrayKey.FromOrdinal(index);
                        var group = match.Groups[index];
                        context.State.Arrays.SetValue(
                            arrayName,
                            ordinalKey,
                            context.State.CreateByteTextValue(group.Value));

                        if (group.Success)
                        {
                            context.State.Arrays.SetValue(
                                arrayName,
                                context.State.ComposeArrayKey(ordinalKey, MatchStartCaptureKeyPart),
                                AwkValue.FromNumber(group.Index + 1));
                            context.State.Arrays.SetValue(
                                arrayName,
                                context.State.ComposeArrayKey(ordinalKey, MatchLengthCaptureKeyPart),
                                AwkValue.FromNumber(group.Length));
                        }
                    }
                }
            }

            return AwkValue.FromNumber(match.Success ? match.Index + 1 : 0);
        }

        private static long EstimateCaptureBytes(AwkRegexMatch match)
        {
            var bytes = 0L;
            for (var index = 0; index < match.Groups.Count; index++)
            {
                var group = match.Groups[index];
                if (group.Success)
                    bytes += group.Value.ByteLength;
            }

            return bytes;
        }
    }
}
