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
        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = default;
            if (remainingDepth <= 1 ||
                !condition.PrepareSynchronousCalls(functions, checkpoint).TryGetSynchronousExpression(out var test) ||
                !AwkStatementBlockExecutor.TryPrepareComputation(body, functions, remainingDepth - 1,
                    out var yes, out var yesDepth, out var yesCalls, checkpoint) ||
                !AwkStatementBlockExecutor.TryPrepareComputation(elseBody, functions, remainingDepth - 1,
                    out var no, out var noDepth, out var noCalls, checkpoint)) return false;
            var depth = 1 + Math.Max(test.SynchronousDepth, Math.Max(yesDepth, noDepth));
            if (depth > 16) return false;
            prepared = new(new IfStatement(new(test), yes, no), depth,
                Math.Max(test.SynchronousCallDepth, Math.Max(yesCalls, noCalls)));
            return true;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned)
        {
            context.State.Counters.RecordSynchronousConditional();
            var selected = condition.EvaluateComputation(context) ? body : elseBody;
            return AwkStatementBlockExecutor.ExecuteComputation(selected, context, ref returned);
        }

        internal override bool TryPrepareTopLevelSynchronousAction(int conditionalDepth,
            ref int remainingStatements, out Action<AwkEvaluationContext> action)
        {
            action = static _ => { };
            if (conditionalDepth <= 0 || !condition.TryGetSynchronousExpression(out var test) ||
                !AwkStatementBlockExecutor.TryPrepareActions(body, null, conditionalDepth - 1,
                    ref remainingStatements, out var yes) ||
                !AwkStatementBlockExecutor.TryPrepareActions(elseBody, null, conditionalDepth - 1,
                    ref remainingStatements, out var no)) return false;
            action = PrepareConditional(test, yes, no);
            return true;
        }

        private static Action<AwkEvaluationContext> PrepareConditional(AwkSynchronousExpression test,
            Action<AwkEvaluationContext> yes, Action<AwkEvaluationContext> no) => context =>
        {
            context.State.Counters.RecordSynchronousConditional();
            if (test.EvaluateSynchronous(context).IsTruthy()) yes(context);
            else no(context);
        };

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var selectedBody = await condition.EvaluateAsync(context).ConfigureAwait(false) ? body : elseBody;
            return await AwkStatementBlockExecutor.ExecuteAsync(selectedBody, context).ConfigureAwait(false);
        }
    }

    private sealed class ForInStatement(
        AwkVariableName loopVariable,
        AwkArrayName arrayVariable,
        AwkStatement[] body,
        AwkArrayOperand? operand = null) : AwkStatement
    {
        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            var state = context.State;
            using var keys = operand is null
                ? await state.SnapshotArrayKeyValuesAsync(arrayVariable, context).ConfigureAwait(false)
                : await state.SnapshotArrayKeyValuesAsync(await operand.ResolveAsync(context).ConfigureAwait(false), context).ConfigureAwait(false);
            for (var index = 0; index < keys.Count; index++)
            {
                state.Checkpoint();
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
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? synchronousFunctions, Action? preparationCheckpoint) : AwkStatement
    {
        private readonly AwkSynchronousFor? _synchronous = AwkSynchronousFor.TryPrepare(initializer, condition, increment, body, synchronousFunctions, preparationCheckpoint);

        internal override bool TryPrepareTopLevelSynchronousAction(int conditionalDepth,
            ref int remainingStatements, out Action<AwkEvaluationContext> action)
        {
            if (_synchronous is not { } prepared)
                return base.TryPrepareTopLevelSynchronousAction(conditionalDepth, ref remainingStatements, out action);
            action = context => prepared.Execute(context);
            return true;
        }

        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = default;
            if (remainingDepth <= 1 ||
                !initializer.TryPrepareComputation(functions, remainingDepth - 1, out var initialize, checkpoint) ||
                !condition.PrepareSynchronousCalls(functions, checkpoint).TryGetSynchronousExpression(out var test) ||
                !increment.TryPrepareComputation(functions, remainingDepth - 1, out var advance, checkpoint) ||
                !AwkStatementBlockExecutor.TryPrepareComputation(body, functions, remainingDepth - 1,
                    out var nodes, out var bodyDepth, out var bodyCalls, checkpoint)) return false;
            var depth = 1 + Math.Max(test.SynchronousDepth,
                Math.Max(bodyDepth, Math.Max(initialize.Depth, advance.Depth)));
            if (depth > 16) return false;
            prepared = new(new ForStatement(initialize.Statement, new(test), advance.Statement, nodes, null, null),
                depth, Math.Max(test.SynchronousCallDepth,
                    Math.Max(bodyCalls, Math.Max(initialize.Calls, advance.Calls))));
            return true;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned)
        {
            context.State.Counters.RecordSynchronousLoop();
            var flow = initializer.ExecuteComputation(context, ref returned);
            if (flow != AwkStatementFlow.Continue) return flow;
            while (condition.EvaluateComputation(context))
            {
                context.State.Checkpoint();
                flow = AwkStatementBlockExecutor.ExecuteComputation(body, context, ref returned);
                if (flow == AwkStatementFlow.BreakLoop) return AwkStatementFlow.Continue;
                if (flow is not (AwkStatementFlow.Continue or AwkStatementFlow.ContinueLoop)) return flow;
                var continued = flow == AwkStatementFlow.ContinueLoop;
                flow = increment.ExecuteComputation(context, ref returned);
                if (flow == AwkStatementFlow.BreakLoop) return AwkStatementFlow.Continue;
                if (flow != AwkStatementFlow.Continue && !(continued && flow == AwkStatementFlow.ContinueLoop))
                    return flow;
            }
            return AwkStatementFlow.Continue;
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
                state.Checkpoint();
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

        internal override bool TryPrepareTopLevelSynchronousAction(int conditionalDepth,
            ref int remainingStatements, out Action<AwkEvaluationContext> action)
        {
            if (_synchronous is not { } prepared)
                return base.TryPrepareTopLevelSynchronousAction(conditionalDepth, ref remainingStatements, out action);
            action = context => prepared.Execute(context);
            return true;
        }

        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = default;
            if (remainingDepth <= 1 ||
                !condition.PrepareSynchronousCalls(functions, checkpoint).TryGetSynchronousExpression(out var test) ||
                !AwkStatementBlockExecutor.TryPrepareComputation(body, functions, remainingDepth - 1,
                    out var nodes, out var bodyDepth, out var bodyCalls, checkpoint)) return false;
            var depth = 1 + Math.Max(test.SynchronousDepth, bodyDepth);
            if (depth > 16) return false;
            prepared = new(new WhileStatement(new(test), nodes), depth,
                Math.Max(test.SynchronousCallDepth, bodyCalls));
            return true;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned)
        {
            context.State.Counters.RecordSynchronousLoop();
            while (condition.EvaluateComputation(context))
            {
                context.State.Checkpoint();
                var flow = AwkStatementBlockExecutor.ExecuteComputation(body, context, ref returned);
                if (flow == AwkStatementFlow.BreakLoop) return AwkStatementFlow.Continue;
                if (flow is not (AwkStatementFlow.Continue or AwkStatementFlow.ContinueLoop)) return flow;
            }
            return AwkStatementFlow.Continue;
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
                state.Checkpoint();
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
        private readonly AwkSynchronousFor? _synchronous = AwkSynchronousFor.TryPrepare(
            NoOp(), condition, NoOp(), body);

        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = default;
            if (remainingDepth <= 1 ||
                !condition.PrepareSynchronousCalls(functions, checkpoint).TryGetSynchronousExpression(out var test) ||
                !AwkStatementBlockExecutor.TryPrepareComputation(body, functions, remainingDepth - 1,
                    out var nodes, out var bodyDepth, out var bodyCalls, checkpoint)) return false;
            var depth = 1 + Math.Max(test.SynchronousDepth, bodyDepth);
            if (depth > 16) return false;
            prepared = new(new DoWhileStatement(nodes, new(test)), depth,
                Math.Max(test.SynchronousCallDepth, bodyCalls));
            return true;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned)
        {
            context.State.Counters.RecordSynchronousLoop();
            context.State.Counters.RecordSynchronousDoWhileLoop();
            do
            {
                context.State.Checkpoint();
                var flow = AwkStatementBlockExecutor.ExecuteComputation(body, context, ref returned);
                if (flow == AwkStatementFlow.BreakLoop) return AwkStatementFlow.Continue;
                if (flow is not (AwkStatementFlow.Continue or AwkStatementFlow.ContinueLoop)) return flow;
            } while (condition.EvaluateComputation(context));
            return AwkStatementFlow.Continue;
        }

        public override async ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context)
        {
            if (_synchronous is { } prepared && prepared.CanExecute(context))
                return prepared.ExecuteDoWhile(context);

            var state = context.State;
            do
            {
                state.Checkpoint();
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
                state.Checkpoint();
                if (arms[index] is AwkDefaultSwitchArm)
                {
                    defaultArmIndex = index;
                    continue;
                }

                var candidate = (AwkSwitchCase)arms[index];
                if (!candidate.Pattern.Matches(value, state))
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

        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = new(this, 1, 0);
            return remainingDepth > 0;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned) =>
            AwkStatementFlow.BreakLoop;

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.BreakLoop);

    }

    private sealed class ContinueStatement : AwkStatement
    {
        public static readonly ContinueStatement Instance = new();

        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = new(this, 1, 0);
            return remainingDepth > 0;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned) =>
            AwkStatementFlow.ContinueLoop;

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.ContinueLoop);

    }

    private sealed class NextStatement : AwkStatement
    {
        public static readonly NextStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.NextRecord);
    }

    private sealed class NextFileStatement : AwkStatement
    {
        public static readonly NextFileStatement Instance = new();

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.NextFile);
    }

    private sealed class ReturnStatement(AwkOptionalExpression valueExpression) : AwkStatement
    {
        internal override bool TryPrepareComputation(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkStatementPreparation prepared, Action? checkpoint)
        {
            prepared = default;
            if (remainingDepth <= 0) return false;
            if (!valueExpression.HasValue)
            {
                prepared = new(this, 1, 0);
                return true;
            }
            if (!valueExpression.Value.TryPrepareFunctionCalls(functions, remainingDepth, out var value, checkpoint))
                return false;
            prepared = new(new ReturnStatement(AwkOptionalExpression.Some(value)),
                value.SynchronousDepth, value.SynchronousCallDepth);
            return true;
        }

        internal override AwkStatementFlow ExecuteComputation(AwkEvaluationContext context, ref AwkValue returned)
        {
            returned = valueExpression.HasValue
                ? ((AwkSynchronousExpression)valueExpression.Value).EvaluateSynchronous(context)
                : AwkValue.Unassigned();
            return AwkStatementFlow.ReturnFunction;
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
    }
}
