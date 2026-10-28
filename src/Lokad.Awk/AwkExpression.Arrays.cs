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
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(input.EstimatedRetainedMemoryBytes);
            var array = await arrayOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldArray = context.State.MemoryAccounting.ReserveTransient(array.RetainedBytes);
            var csv = usesDefaultSeparator && context.State.CsvInput;
            var separator = csv ? default : await separatorExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            AwkArrayTarget? separators = separatorArrayOperand is null ? null :
                await separatorArrayOperand.CaptureAsync(context).ConfigureAwait(false);
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

            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            using var heldInput = context.State.MemoryAccounting.ReserveTransient(input.EstimatedRetainedMemoryBytes);
            var array = await arrayOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldArray = context.State.MemoryAccounting.ReserveTransient(array.RetainedBytes);
            var pattern = await patternExpression.ResolveRegexPatternAsync(context).ConfigureAwait(false);
            AwkArrayTarget? separators = separatorArrayOperand is null ? null :
                await separatorArrayOperand.CaptureAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.PatsplitIntoArray(
                input,
                array,
                pattern,
                separators));
        }
    }

    private sealed class SortArrayExpression(AwkArrayOperand sourceOperand, AwkArrayOperand? destinationOperand,
        AwkExpression orderExpression, bool keys) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(
                    AwkBuiltins.Name(keys ? AwkBuiltinId.Asorti : AwkBuiltinId.Asort)));

            var source = await sourceOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldSource = context.State.MemoryAccounting.ReserveTransient(source.RetainedBytes);
            var destination = destinationOperand is null ? source : await destinationOperand.CaptureAsync(context).ConfigureAwait(false);
            using var heldDestination = context.State.MemoryAccounting.ReserveTransient(destinationOperand is null ? 0 : destination.RetainedBytes);
            var order = await orderExpression.ResolveStringAsync(context).ConfigureAwait(false);
            var parsed = AwkArraySortOrder.Parse(order);
            return AwkValue.FromNumber(keys
                ? await context.State.SortArrayKeysAsync(source, destination, parsed, context).ConfigureAwait(false)
                : await context.State.SortArrayValuesAsync(source, destination, parsed, context).ConfigureAwait(false));
        }
    }
}
