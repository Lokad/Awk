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

            compiler.EmitArrayOperation(AwkOpcode.DeleteArrayElement, arrayName);
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
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            _ = await expression
                .EvaluateAsync(context)
                .ConfigureAwait(false);
            return AwkStatementFlow.Continue;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler) =>
            compiler.TryEmitExpressionStatement(expression);
    }
}
