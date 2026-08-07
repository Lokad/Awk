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
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            await AwkBuiltins.EvaluateAsync(definition, arguments, context).ConfigureAwait(false);
    }

    private sealed class FunctionCallExpression(AwkFunctionName name, IReadOnlyList<AwkExpression> arguments) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (context.State.TryGetUserFunction(name, out var function))
                return await function.InvokeAsync(arguments, context).ConfigureAwait(false);

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
                if (context.State.CompatibilityMode == AwkCompatibilityMode.Posix)
                    throw new AwkRuntimeException(AwkMessages.ErrorLengthArrayArgument);

                return AwkValue.FromNumber(count);
            }

            var value = await valueExpression.ResolveByteTextAsync(context).ConfigureAwait(false);
            return AwkValue.FromNumber(context.State.CountStringUnits(value));
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
            return context.State.TryPeekArrayAwkValue(variableName, key, out var value)
                ? TypeOf(value.RuntimeKind)
                : TypeOf(AwkRuntimeValueKind.Untyped);
        }
    }
}
