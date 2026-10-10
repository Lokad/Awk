using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static AwkValue TypeOf(AwkRuntimeValueKind kind) =>
        AwkValue.FromText(AwkRuntimeValueKindNames.Format(kind));

    private sealed class BuiltinCallExpression(AwkBuiltinDefinition definition, IReadOnlyList<AwkExpression> arguments) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            AwkBuiltins.EvaluateAsync(definition, arguments, context);
    }

    private sealed class FunctionCallExpression(AwkFunctionName name, IReadOnlyList<AwkExpression> arguments) : AwkExpression
    {
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

    private sealed class LengthExpression(AwkExpression valueExpression, AwkOptionalIdentifier arrayName) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (arrayName.HasValue &&
                context.State.TryGetExistingArrayCount(new AwkArrayName(arrayName.Value), out var count))
            {

                return AwkValue.FromNumber(count);
            }

            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            if (value.TryGetArrayName(out var name) && context.State.TryGetExistingArrayCount(name, out var arrayCount))
            {
                return AwkValue.FromNumber(arrayCount);
            }
            return AwkValue.FromNumber(context.State.CountStringUnits(context.State.FormatStringByteText(value)));
        }
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
