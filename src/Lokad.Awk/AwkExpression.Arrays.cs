using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class SplitExpression(
        AwkExpression valueExpression,
        AwkArrayOperand arrayOperand,
        AwkExpression separatorExpression,
        AwkArrayOperand? separatorArrayOperand,
        bool isRegexLiteral,
        bool usesDefaultSeparator) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var inputValue = (await valueExpression.EvaluateAsync(context).ConfigureAwait(false)).AsScalarCopy();
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(inputValue.RetainedTextMemoryBytes);
            var array = await arrayOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldArray = context.State.MemoryAccounting.ReserveTransient(array.RetainedBytes);
            var csv = usesDefaultSeparator && context.State.CsvInput;
            var separator = csv ? default : await separatorExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldSeparator = context.State.MemoryAccounting.ReserveTransient(separator.Text.EstimatedRetainedMemoryBytes);
            AwkArrayTarget? separators = separatorArrayOperand is null ? null :
                await separatorArrayOperand.CaptureAsync(context).ConfigureAwait(false);
            var input = context.State.FormatStringByteText(inputValue, heldInput);
            return AwkValue.FromNumber(context.State.SplitIntoArray(
                input,
                array,
                csv ? default : AwkFieldSeparatorSpec.FromRegexPattern(separator, isRegexLiteral),
                separators, csv));
        }
    }

    private sealed class PatsplitExpression(
        AwkExpression valueExpression,
        AwkArrayOperand arrayOperand,
        AwkExpression patternExpression,
        AwkArrayOperand? separatorArrayOperand) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.Patsplit)));

            var inputValue = (await valueExpression.EvaluateAsync(context).ConfigureAwait(false)).AsScalarCopy();
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(inputValue.RetainedTextMemoryBytes);
            var array = await arrayOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldArray = context.State.MemoryAccounting.ReserveTransient(array.RetainedBytes);
            var pattern = await patternExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            using var heldPattern = context.State.MemoryAccounting.ReserveTransient(pattern.Text.EstimatedRetainedMemoryBytes);
            AwkArrayTarget? separators = separatorArrayOperand is null ? null :
                await separatorArrayOperand.CaptureAsync(context).ConfigureAwait(false);
            var input = context.State.FormatStringByteText(inputValue, heldInput);
            return AwkValue.FromNumber(context.State.PatsplitIntoArray(
                input,
                array,
                pattern,
                separators));
        }
    }

}
