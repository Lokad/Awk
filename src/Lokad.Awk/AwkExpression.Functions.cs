using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static AwkValue TypeOf(AwkRuntimeValueKind kind) =>
        AwkRuntimeValueKindNames.Value(kind);

    private sealed class FieldLengthExpression(AwkExpression indexExpression) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => indexExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and <= 14 } index
            ? index.SynchronousDepth + 2 : 0;
        internal override int SynchronousCallDepth => indexExpression is AwkSynchronousExpression index ? index.SynchronousCallDepth : 0;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            if (indexExpression is not AwkSynchronousExpression index)
                throw new InvalidOperationException("Synchronous field index is required.");
            context.State.Checkpoint(); // The original field expression, before its index.
            var field = AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value;
            return AwkValue.FromNumber(context.State.CountFieldStringUnits(field));
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            // Retain the field-expression checkpoint before evaluating its index.
            context.State.Checkpoint();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(context.State.CountFieldStringUnits(AwkFieldIndex.FromNumber(pending.Result).Value)))
                : AwaitIndexAsync(context, pending);
        }

        private static async ValueTask<AwkValue> AwaitIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            AwkValue.FromNumber(context.State.CountFieldStringUnits(AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value));
    }

    private sealed class LengthExpression(AwkExpression valueExpression, AwkOptionalIdentifier arrayName) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => valueExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } value
            ? value.SynchronousDepth + 1 : 0;
        internal override int SynchronousCallDepth => valueExpression is AwkSynchronousExpression value ? value.SynchronousCallDepth : 0;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            if (TryGetNamedArrayLength(context, out var result)) return result;
            return valueExpression is AwkSynchronousExpression value
                ? EvaluateLengthValue(context, value.EvaluateSynchronous(context))
                : throw new InvalidOperationException("Synchronous length argument is required.");
        }

        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (TryGetNamedArrayLength(context, out var result)) return result;
            return EvaluateLengthValue(context, await valueExpression.EvaluateAsync(context).ConfigureAwait(false));
        }

        private bool TryGetNamedArrayLength(AwkEvaluationContext context, out AwkValue result)
        {
            result = default;
            if (!arrayName.HasValue) return false;
            if (valueExpression is PreparedGlobalVariableExpression scalar && scalar.IsBoundScalarFor(context.State, arrayName.Value))
            {
                context.State.Counters.RecordBoundScalarLength();
                return false;
            }
            if (context.State.TryGetExistingArrayCount(new AwkArrayName(arrayName.Value), out var count))
            {
                result = AwkValue.FromNumber(count);
                return true;
            }
            return false;
        }

        private static AwkValue EvaluateLengthValue(AwkEvaluationContext context, AwkValue value) =>
            value.TryGetArrayBinding(out var array) ? AwkValue.FromNumber(array.Slots.Count) :
            value.TryGetArrayName(out var name) && context.State.TryGetExistingArrayCount(name, out var count)
                ? AwkValue.FromNumber(count)
                : AwkValue.FromNumber(context.State.CountStringUnits(context.State.FormatStringByteText(value)));
    }

    private sealed class IsArrayExpression(AwkArrayName variableName) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.IsArray)));

            return ValueTask.FromResult(AwkValue.FromNumber(context.State.IsArray(variableName) ? 1 : 0));
        }
    }

    private sealed class IsArrayValueExpression(AwkExpression valueExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.IsArray)));

            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(value.IsArrayValue ? 1 : 0);
        }
    }

    private sealed class ScalarArgumentExpression(AwkExpression valueExpression) : AwkExpression
    {
        // The wrapper preserves the source's scalar context and owns its ordinary
        // expression checkpoint; evaluating the wrapped core adds no extra step.
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            valueExpression.EvaluateCoreAsync(context);
    }

    private sealed class IsArrayArrayElementExpression(AwkArrayName name, AwkArrayIndex indexExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.IsArray)));

            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.GetArrayElementRuntimeKind(name, key) == AwkRuntimeValueKind.Array ? 1 : 0);
        }
    }

    private sealed class TypeOfExpression(AwkExpression valueExpression, AwkOptionalIdentifier variableName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.TypeOf)));

            if (variableName.HasValue)
            {
                var arrayName = new AwkArrayName(variableName.Value);
                if (context.State.IsArray(arrayName))
                    return TypeOf(AwkRuntimeValueKind.Array);

                if (!context.State.HasScalar(new AwkVariableName(variableName.Value)))
                    return TypeOf(AwkRuntimeValueKind.Untyped);
            }

            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            return TypeOf(value.RuntimeKind);
        }
    }

    private sealed class TypeOfArrayElementExpression(AwkArrayName variableName, AwkArrayIndex indexExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(AwkBuiltins.Name(AwkBuiltinId.TypeOf)));

            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return TypeOf(context.State.GetArrayElementRuntimeKind(variableName, key));
        }
    }
}
