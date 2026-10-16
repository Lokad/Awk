namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private static AwkExpression CreateLogical(AwkExpression left, AwkExpression right, LogicalOperation operation) =>
        TryGetSynchronousPair(left, right, out var first, out var second)
            ? new SynchronousLogicalExpression(first, second, operation)
            : new LogicalExpression(left, right, operation);

    private sealed class SynchronousLogicalExpression(AwkSynchronousExpression left,
        AwkSynchronousExpression right, LogicalOperation operation) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth { get; } = 1 + Math.Max(left.SynchronousDepth, right.SynchronousDepth);
        internal override int SynchronousCallDepth { get; } = Math.Max(left.SynchronousCallDepth, right.SynchronousCallDepth);
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var truthy = left.EvaluateSynchronous(context).IsTruthy();
            if (operation == LogicalOperation.And && !truthy) return AwkValue.FromNumber(0);
            if (operation == LogicalOperation.Or && truthy) return AwkValue.FromNumber(1);
            return AwkValue.FromNumber(right.EvaluateSynchronous(context).IsTruthy() ? 1 : 0);
        }
        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler) =>
            EmitLogical(compiler, left, right, operation);
    }

    private static bool EmitLogical(AwkExecutableCompiler compiler, AwkExpression left,
        AwkExpression right, LogicalOperation operation)
    {
        if (operation == LogicalOperation.And)
            EmitAnd(compiler);
        else
            EmitOr(compiler);

        return true;

        void EmitAnd(AwkExecutableCompiler compiler)
        {
            var startDepth = compiler.StackDepth;
            compiler.EmitExpressionOrFallback(left);
            var falseJump = compiler.EmitJumpIfFalse();
            compiler.Pop();

            compiler.EmitExpressionOrFallback(right);
            compiler.Emit(AwkOpcode.Truthiness);
            var endJump = compiler.EmitJump();
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchJumpTarget(falseJump, compiler.CurrentOffset);
            EmitBooleanConstant(compiler, value: false);

            compiler.PatchJumpTarget(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
        }

        void EmitOr(AwkExecutableCompiler compiler)
        {
            var startDepth = compiler.StackDepth;
            compiler.EmitExpressionOrFallback(left);
            var evaluateRightJump = compiler.EmitJumpIfFalse();
            compiler.Pop();

            EmitBooleanConstant(compiler, value: true);
            var endJump = compiler.EmitJump();
            var resultDepth = compiler.StackDepth;

            compiler.SetStackDepth(startDepth);
            compiler.PatchJumpTarget(evaluateRightJump, compiler.CurrentOffset);
            compiler.EmitExpressionOrFallback(right);
            compiler.Emit(AwkOpcode.Truthiness);

            compiler.PatchJumpTarget(endJump, compiler.CurrentOffset);
            compiler.SetStackDepth(resultDepth);
        }
    }
}
