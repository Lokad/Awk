using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class DeleteArrayElementStatement(AwkArrayName arrayName, AwkArrayIndex indexExpression) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var index = await indexExpression
                .ResolveKeyAsync(context)
                .ConfigureAwait(false);
            context.State.DeleteArrayValue(arrayName, index);
            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (!indexExpression.TryEmitExecutableKey(compiler))
                return false;

            compiler.EmitDeleteArrayElement(arrayName);
            compiler.Pop();
            return true;
        }
    }

    private sealed class DeleteArrayStatement(AwkArrayName arrayName) : AwkStatement
    {
        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            context.State.ClearArray(arrayName);
            return ValueTask.FromResult(AwkStatementFlow.Continue);
        }
    }

    private sealed class ExpressionStatement(AwkExpression expression) : AwkStatement
    {
        internal override bool TryGetLocalAssignment(
            out (AwkVariableName Name, AwkFunctionLocalSlotId Slot) local, out AwkExpression value) =>
            expression.TryGetLocalAssignment(out local, out value);

        internal override AwkStatement PrepareSynchronousCalls(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions)
        {
            var prepared = expression.PrepareSynchronousCallAction(functions);
            return ReferenceEquals(prepared, expression) ? this : new ExpressionStatement(prepared);
        }

        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action) =>
            expression.TryPrepareSynchronousAction(out action);

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var pending = expression.EvaluateAsync(context);
            if (!pending.IsCompletedSuccessfully)
                return AwaitExpressionAsync(pending);

            _ = pending.Result;
            return ValueTask.FromResult(AwkStatementFlow.Continue);
        }

        private static async ValueTask<AwkStatementFlow> AwaitExpressionAsync(ValueTask<AwkValue> pending)
        {
            _ = await pending.ConfigureAwait(false);
            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler) =>
            compiler.TryEmitExpressionStatement(expression);
    }
}
