using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static AwkValue TypeOf(AwkRuntimeValueKind kind) =>
        AwkValue.FromText(AwkRuntimeValueKindNames.Format(kind));

    private static AwkExpression CreateStringBuiltin(AwkBuiltinDefinition definition, IReadOnlyList<AwkExpression> arguments)
    {
        if (definition.Id is AwkBuiltinId.Index or AwkBuiltinId.Substr or AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper &&
            definition.Arity.Accepts(arguments.Count))
        {
            var depth = 0; var calls = 0;
            foreach (var argument in arguments)
            {
                if (argument is not AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } value)
                    return new BuiltinCallExpression(definition, arguments);
                depth = Math.Max(depth, value.SynchronousDepth);
                calls = Math.Max(calls, value.SynchronousCallDepth);
            }
            return new SynchronousStringBuiltinExpression(definition.Id, arguments, depth + 1, calls);
        }
        return new BuiltinCallExpression(definition, arguments);
    }

    private sealed class SynchronousStringBuiltinExpression(AwkBuiltinId id, IReadOnlyList<AwkExpression> arguments,
        int depth, int calls) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => depth;
        internal override int SynchronousCallDepth => calls;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var first = ((AwkSynchronousExpression)arguments[0]).EvaluateSynchronous(context);
            if (id is AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper)
                return AwkBuiltins.EvaluateCaseValue(context.State, first, id == AwkBuiltinId.ToUpper);
            var second = ((AwkSynchronousExpression)arguments[1]).EvaluateSynchronous(context);
            if (id == AwkBuiltinId.Index) return AwkBuiltins.EvaluateIndexValues(context.State, first, second);
            var third = arguments.Count == 3 ? ((AwkSynchronousExpression)arguments[2]).EvaluateSynchronous(context) : default;
            return AwkBuiltins.EvaluateSubstrValues(context.State, first, second, third, arguments.Count);
        }
    }

    private sealed class BuiltinCallExpression(AwkBuiltinDefinition definition, IReadOnlyList<AwkExpression> arguments) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkBuiltins.EvaluateAsync(definition, arguments, context);
    }

    private sealed class FunctionCallExpression(AwkFunctionName name, IReadOnlyList<AwkExpression> arguments) : AwkExpression
    {
        internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression)
        {
            if (remainingDepth > 0 && arguments.Count == 1 && functions.TryGetValue(name, out var function) &&
                function.Layout.SlotCount == 1 && arguments[0].TryPrepareFunctionCalls(functions, remainingDepth - 1, out var operand))
            {
                var call = new SynchronousFunctionCall(function, operand);
                if (call.SynchronousDepth <= 16 && call.SynchronousCallDepth < 4)
                {
                    expression = call;
                    return true;
                }
            }
            expression = AwkSynchronousExpression.Unavailable;
            return false;
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.TryGetUserFunction(name, out var function))
                return function.InvokeAsync(arguments, context);

            throw new AwkRuntimeException(
                context.State.LintMode switch
                {
                    AwkLintMode.Off => AwkMessages.FormatUndefinedFunction(name.Text),
                    AwkLintMode.Fatal => AwkMessages.FormatUndefinedFunctionWithFatalLint(name.Text),
                    _ => AwkMessages.FormatUndefinedFunctionWithLintWarning(name.Text)
                });
        }
    }

    private sealed class FieldLengthExpression(AwkExpression indexExpression) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => indexExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and <= 14 } index
            ? index.SynchronousDepth + 2 : 0;
        internal override int SynchronousCallDepth => indexExpression is AwkSynchronousExpression index ? index.SynchronousCallDepth : 0;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            if (indexExpression is not AwkSynchronousExpression index)
                throw new InvalidOperationException("Synchronous field index is required.");
            context.State.RecordExecutionStep(); // The original field expression, before its index.
            var field = (int)index.ResolveNumberSynchronous(context);
            return AwkValue.FromNumber(context.State.CountFieldStringUnits(field));
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            // Retain the field-expression checkpoint before evaluating its index.
            context.State.RecordExecutionStep();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(AwkValue.FromNumber(context.State.CountFieldStringUnits((int)pending.Result)))
                : AwaitIndexAsync(context, pending);
        }

        private static async ValueTask<AwkValue> AwaitIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            AwkValue.FromNumber(context.State.CountFieldStringUnits((int)await pending.ConfigureAwait(false)));
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
            if (arrayName.HasValue && context.State.TryGetExistingArrayCount(new AwkArrayName(arrayName.Value), out var count))
            {
                result = AwkValue.FromNumber(count);
                return true;
            }
            result = default;
            return false;
        }

        private static AwkValue EvaluateLengthValue(AwkEvaluationContext context, AwkValue value) =>
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
