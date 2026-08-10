using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    private sealed class IfStatement(
        AwkCondition condition,
        AwkStatement[] body,
        AwkStatement[] elseBody) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var selectedBody = await condition.EvaluateAsync(context).ConfigureAwait(false) ? body : elseBody;
            return await ExecuteAllAsync(selectedBody, context).ConfigureAwait(false);
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (!condition.TryEmitExecutable(compiler))
                return false;

            var jumpIfFalseIndex = compiler.EmitJump(AwkOpcode.JumpIfFalse);
            compiler.Pop();
            for (var index = 0; index < body.Length; index++)
            {
                compiler.Emit(AwkOpcode.CheckBudget);
                compiler.EmitStatementOrFallback(body[index]);
            }

            if (elseBody.Length == 0)
            {
                compiler.PatchJumpTarget(jumpIfFalseIndex, compiler.CurrentOffset);
                return true;
            }

            var jumpOverElseIndex = compiler.EmitJump(AwkOpcode.Jump);
            compiler.PatchJumpTarget(jumpIfFalseIndex, compiler.CurrentOffset);
            for (var index = 0; index < elseBody.Length; index++)
            {
                compiler.Emit(AwkOpcode.CheckBudget);
                compiler.EmitStatementOrFallback(elseBody[index]);
            }

            compiler.PatchJumpTarget(jumpOverElseIndex, compiler.CurrentOffset);
            return true;
        }
    }

    private sealed class ForInStatement(
        AwkVariableName loopVariable,
        AwkArrayName arrayVariable,
        AwkStatement[] body) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            using var keys = await state.SnapshotArrayKeyValuesAsync(arrayVariable, context).ConfigureAwait(false);
            for (var index = 0; index < keys.Count; index++)
            {
                state.RecordExecutionStep();
                state.ThrowIfCancellationRequested();
                state.AssignVariableValue(loopVariable, state.CreateArrayKeyValue(keys[index]));
                var flow = await ExecuteAllAsync(body, context).ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow == AwkStatementFlow.ContinueLoop)
                    continue;

                if (flow != AwkStatementFlow.Continue)
                    return flow;
            }

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class ForStatement(
        AwkStatement initializer,
        AwkCondition condition,
        AwkStatement increment,
        AwkStatement[] body) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var flow = await initializer.ExecuteAsync(context).ConfigureAwait(false);
            if (flow != AwkStatementFlow.Continue)
                return flow;

            while (await condition
                .EvaluateAsync(context)
                .ConfigureAwait(false))
            {
                state.RecordExecutionStep();
                state.ThrowIfCancellationRequested();
                flow = await ExecuteAllAsync(body, context).ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow == AwkStatementFlow.ContinueLoop)
                {
                    flow = await increment.ExecuteAsync(context).ConfigureAwait(false);
                    if (flow == AwkStatementFlow.BreakLoop)
                        return AwkStatementFlow.Continue;

                    if (flow != AwkStatementFlow.Continue && flow != AwkStatementFlow.ContinueLoop)
                        return flow;

                    continue;
                }

                if (flow != AwkStatementFlow.Continue)
                    return flow;

                flow = await increment.ExecuteAsync(context).ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow != AwkStatementFlow.Continue)
                    return flow;
            }

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class WhileStatement(AwkCondition condition, AwkStatement[] body) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            while (await condition
                .EvaluateAsync(context)
                .ConfigureAwait(false))
            {
                state.RecordExecutionStep();
                state.ThrowIfCancellationRequested();
                var flow = await ExecuteAllAsync(body, context).ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow == AwkStatementFlow.ContinueLoop)
                {
                    continue;
                }

                if (flow != AwkStatementFlow.Continue)
                    return flow;
            }

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class DoWhileStatement(AwkStatement[] body, AwkCondition condition) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            do
            {
                state.RecordExecutionStep();
                state.ThrowIfCancellationRequested();
                var flow = await ExecuteAllAsync(body, context).ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow != AwkStatementFlow.Continue && flow != AwkStatementFlow.ContinueLoop)
                    return flow;
            }
            while (await condition
                .EvaluateAsync(context)
                .ConfigureAwait(false));

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class SwitchStatement(
        AwkExpression valueExpression,
        AwkSwitchCase[] cases,
        AwkStatement[] defaultBody) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            if (state.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.ErrorSwitchIsGnuExtension, AwkDiagnosticId.UnsupportedPolicy);

            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            for (var index = 0; index < cases.Length; index++)
            {
                var candidate = cases[index];
                if (!await MatchesAsync(candidate, value, context).ConfigureAwait(false))
                    continue;

                return await ExecuteSelectedAsync(candidate.Statements, context)
                    .ConfigureAwait(false);
            }

            return await ExecuteSelectedAsync(defaultBody, context).ConfigureAwait(false);
        }

        private static ValueTask<bool> MatchesAsync(
            AwkSwitchCase candidate,
            AwkValue value,
            AwkEvaluationContext context) =>
            candidate.Pattern.MatchesAsync(value, context);

        private static async ValueTask<AwkStatementFlow> ExecuteSelectedAsync(
            AwkStatement[] body,
            AwkEvaluationContext context)
        {
            var flow = await ExecuteAllAsync(body, context).ConfigureAwait(false);
            return flow == AwkStatementFlow.BreakLoop ? AwkStatementFlow.Continue : flow;
        }
    }

    private sealed class BreakStatement : AwkStatement
    {
        public static readonly BreakStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.BreakLoop);

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.Break);
            return true;
        }
    }

    private sealed class ContinueStatement : AwkStatement
    {
        public static readonly ContinueStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.ContinueLoop);

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.Continue);
            return true;
        }
    }

    private sealed class NextStatement : AwkStatement
    {
        public static readonly NextStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.NextRecord);

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.Next);
            return true;
        }
    }

    private sealed class NextFileStatement : AwkStatement
    {
        public static readonly NextFileStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.NextFile);

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.NextFile);
            return true;
        }
    }

    private sealed class ReturnStatement(AwkOptionalExpression valueExpression) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            if (!state.IsInsideFunction)
                throw new AwkRuntimeException(AwkMessages.ErrorReturnOutsideFunction);

            var value = valueExpression.HasValue
                ? await valueExpression.Value
                    .EvaluateAsync(context)
                    .ConfigureAwait(false)
                : AwkValue.FromText(string.Empty);
            state.SetFunctionReturnValue(value);
            return AwkStatementFlow.ReturnFunction;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (valueExpression.HasValue && !compiler.TryEmitExpression(valueExpression.Value))
                return false;

            compiler.EmitOptionalValue(AwkOpcode.Return, valueExpression.HasValue);
            if (valueExpression.HasValue)
                compiler.Pop();

            return true;
        }
    }

    private sealed class ExitStatement(AwkOptionalExpression statusExpression) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            var exitCode = AwkOptionalExitCode.None;
            if (statusExpression.HasValue)
            {
                exitCode = AwkOptionalExitCode.Specified(new AwkExitCode(
                    (int)Math.Truncate((await statusExpression.Value.EvaluateAsync(context).ConfigureAwait(false)).AsNumber())));
            }

            state.RequestExit(exitCode);
            return AwkStatementFlow.ExitProgram;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (statusExpression.HasValue && !compiler.TryEmitExpression(statusExpression.Value))
                return false;

            compiler.EmitOptionalValue(AwkOpcode.Exit, statusExpression.HasValue);
            if (statusExpression.HasValue)
                compiler.Pop();

            return true;
        }
    }
}
