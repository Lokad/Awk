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
            return await AwkStatementBlockExecutor.ExecuteAsync(selectedBody, context).ConfigureAwait(false);
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (!condition.TryEmitExecutable(compiler))
                return false;

            var jumpIfFalseIndex = compiler.EmitJumpIfFalse();
            compiler.Pop();
            for (var index = 0; index < body.Length; index++)
            {
                compiler.EmitStatementOrFallback(body[index]);
            }

            if (elseBody.Length == 0)
            {
                compiler.PatchJumpTarget(jumpIfFalseIndex, compiler.CurrentOffset);
                return true;
            }

            var jumpOverElseIndex = compiler.EmitJump();
            compiler.PatchJumpTarget(jumpIfFalseIndex, compiler.CurrentOffset);
            for (var index = 0; index < elseBody.Length; index++)
            {
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
                state.AssignVariableValue(loopVariable, state.CreateArrayKeyValue(keys[index]));
                var flow = await AwkStatementBlockExecutor.ExecuteAsync(body, context).ConfigureAwait(false);
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
        AwkStatement[] body,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? synchronousFunctions) : AwkStatement
    {
        private readonly AwkSynchronousFor? _synchronous = AwkSynchronousFor.TryPrepare(initializer, condition, increment, body, synchronousFunctions);

        internal override bool TryPrepareTopLevelSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            if (_synchronous is not { } prepared) return base.TryPrepareTopLevelSynchronousAction(out action);
            action = context => prepared.Execute(context);
            return true;
        }

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            if (_synchronous is { } prepared && prepared.CanExecute(context))
                return prepared.Execute(context);

            var flow = await initializer.ExecuteAsync(context).ConfigureAwait(false);
            if (flow != AwkStatementFlow.Continue)
                return flow;

            while (await condition
                .EvaluateAsync(context)
                .ConfigureAwait(false))
            {
                state.RecordExecutionStep();
                flow = await AwkStatementBlockExecutor.ExecuteAsync(body, context).ConfigureAwait(false);
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
        private readonly AwkSynchronousFor? _synchronous = AwkSynchronousFor.TryPrepare(
            NoOp(), condition, NoOp(), body);

        internal override bool TryPrepareTopLevelSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            if (_synchronous is not { } prepared) return base.TryPrepareTopLevelSynchronousAction(out action);
            action = context => prepared.Execute(context);
            return true;
        }

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            if (_synchronous is { } prepared && prepared.CanExecute(context))
                return prepared.Execute(context);

            var state = context.State;
            while (await condition
                .EvaluateAsync(context)
                .ConfigureAwait(false))
            {
                state.RecordExecutionStep();
                var flow = await AwkStatementBlockExecutor.ExecuteAsync(body, context).ConfigureAwait(false);
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
                var flow = await AwkStatementBlockExecutor.ExecuteAsync(body, context).ConfigureAwait(false);
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
        AwkSwitchArm[] arms) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            if (state.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
                throw new AwkRuntimeException(AwkMessages.ErrorSwitchIsGnuExtension, AwkDiagnosticId.UnsupportedPolicy);

            var value = await valueExpression.EvaluateAsync(context).ConfigureAwait(false);
            var selectedArmIndex = -1;
            var defaultArmIndex = -1;
            for (var index = 0; index < arms.Length; index++)
            {
                if (arms[index] is AwkDefaultSwitchArm)
                {
                    defaultArmIndex = index;
                    continue;
                }

                var candidate = (AwkSwitchCase)arms[index];
                if (!await candidate.Pattern.MatchesAsync(value, context).ConfigureAwait(false))
                    continue;

                selectedArmIndex = index;
                break;
            }

            if (selectedArmIndex < 0)
                selectedArmIndex = defaultArmIndex;

            for (var index = selectedArmIndex; index >= 0 && index < arms.Length; index++)
            {
                var flow = await AwkStatementBlockExecutor
                    .ExecuteAsync(arms[index].Statements, context)
                    .ConfigureAwait(false);
                if (flow == AwkStatementFlow.BreakLoop)
                    return AwkStatementFlow.Continue;

                if (flow != AwkStatementFlow.Continue)
                    return flow;
            }

            return AwkStatementFlow.Continue;
        }
    }

    private sealed class BreakStatement : AwkStatement
    {
        public static readonly BreakStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.BreakLoop);

    }

    private sealed class ContinueStatement : AwkStatement
    {
        public static readonly ContinueStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.ContinueLoop);

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
        internal override bool TryGetReturnExpression(out AwkExpression expression)
        {
            expression = valueExpression.HasValue ? valueExpression.Value : AwkSynchronousExpression.Unavailable;
            return valueExpression.HasValue;
        }

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            if (!state.IsInsideFunction)
                throw new AwkRuntimeException(AwkMessages.ErrorReturnOutsideFunction);

            var value = valueExpression.HasValue
                ? await valueExpression.Value
                    .EvaluateAsync(context)
                    .ConfigureAwait(false)
                : AwkValue.Unassigned();
            state.SetFunctionReturnValue(value);
            return AwkStatementFlow.ReturnFunction;
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
                    (int)Math.Truncate((await statusExpression.Value.EvaluateAsync(context).ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint))));
            }

            state.RequestExit(exitCode);
            return AwkStatementFlow.ExitProgram;
        }

        internal override bool TryEmitExecutable(AwkExecutableCompiler compiler)
        {
            if (statusExpression.HasValue && !compiler.TryEmitExpression(statusExpression.Value))
                return false;

            compiler.EmitExit(statusExpression.HasValue
                ? AwkExitStatusPresence.Specified
                : AwkExitStatusPresence.None);
            if (statusExpression.HasValue)
                compiler.Pop();

            return true;
        }
    }
}
