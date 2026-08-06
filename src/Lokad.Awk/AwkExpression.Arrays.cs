using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class SplitExpression(
        AwkExpression valueExpression,
        AwkArrayName arrayName,
        AwkExpression separatorExpression,
        AwkOptionalIdentifier separatorArrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var separator = await separatorExpression.ResolveStringAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.SplitIntoArray(
                input,
                arrayName,
                separator,
                separatorArrayName));
        }
    }

    private sealed class PatsplitExpression(
        AwkExpression valueExpression,
        AwkArrayName arrayName,
        AwkExpression patternExpression,
        AwkOptionalIdentifier separatorArrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.Patsplit)));

            var input = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            var pattern = await patternExpression.ResolveStringAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.PatsplitIntoArray(
                input,
                arrayName,
                pattern,
                separatorArrayName));
        }
    }

    private sealed class AsortExpression(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression orderExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.Asort)));

            var order = await orderExpression.ResolveStringAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(await context.State
                .SortArrayValuesAsync(sourceArrayName, destinationArrayName, AwkArraySortOrder.Parse(order), context)
                .ConfigureAwait(false));
        }
    }

    private sealed class AsortiExpression(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression orderExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.Asorti)));

            var order = await orderExpression.ResolveStringAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(await context.State
                .SortArrayKeysAsync(sourceArrayName, destinationArrayName, AwkArraySortOrder.Parse(order), context)
                .ConfigureAwait(false));
        }
    }
}
