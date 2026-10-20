using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class DynamicFieldIndexExpression(AwkExpression indexExpression) : AwkSynchronousExpression
    {
        public AwkExpression IndexExpression => indexExpression;

        internal override int SynchronousDepth => indexExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } index
            ? index.SynchronousDepth + 1 : 0;
        internal override int SynchronousCallDepth => indexExpression is AwkSynchronousExpression index ? index.SynchronousCallDepth : 0;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            indexExpression is AwkSynchronousExpression index
                ? ResolveField(context, (int)index.ResolveNumberSynchronous(context))
                : throw new InvalidOperationException("Synchronous field index is required.");

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
            indexExpression is AwkSynchronousExpression index
                ? context.State.GetFieldNumber((int)index.ResolveNumberSynchronous(context))
                : throw new InvalidOperationException("Synchronous field index is required.");

        public ValueTask<bool> CompareLeftAsync(AwkEvaluationContext context, AwkValue left, AwkComparison comparison)
        {
            context.State.Checkpoint();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(context.State.CompareValueField(left, (int)pending.Result, comparison))
                : AwaitComparisonIndexAsync(context, left, comparison, pending);
        }

        private static async ValueTask<bool> AwaitComparisonIndexAsync(
            AwkEvaluationContext context, AwkValue left, AwkComparison comparison, ValueTask<double> pending) =>
            context.State.CompareValueField(left, (int)await pending.ConfigureAwait(false), comparison);

        public override ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(context.State.GetFieldNumber((int)pending.Result))
                : AwaitNumericIndexAsync(context, pending);
        }

        private static async ValueTask<double> AwaitNumericIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            context.State.GetFieldNumber((int)await pending.ConfigureAwait(false));

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(ResolveField(context, (int)pending.Result))
                : AwaitIndexAsync(context, pending);
        }

        private static async ValueTask<AwkValue> AwaitIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            ResolveField(context, (int)await pending.ConfigureAwait(false));

        private static AwkValue ResolveField(AwkEvaluationContext context, int index)
        {
            if (index == 0)
                return context.State.GetCurrentRecordValue();

            if (index < 0)
                throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

            return context.State.GetFieldValue(new AwkFieldIndex(index));
        }
    }

    private sealed class FieldCountExpression : AwkSynchronousExpression
    {
        public static readonly FieldCountExpression Instance = new();

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount));
    }

    private sealed class FieldNfExpression : AwkExpression
    {
        public static readonly FieldNfExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.FieldCount == 0
                ? context.State.GetCurrentRecordValue()
                : context.State.GetFieldValue(context.State.FieldCount));
    }

    private sealed class FieldZeroExpression : AwkExpression
    {
        public static readonly FieldZeroExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.GetCurrentRecordValue());
    }

    private sealed class RecordNumberExpression : AwkExpression
    {
        public static readonly RecordNumberExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordNumber));
    }

    private sealed class FileRecordNumberExpression : AwkExpression
    {
        public static readonly FileRecordNumberExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber));
    }

    private sealed class FileNameExpression : AwkExpression
    {
        public static readonly FileNameExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FileName));
    }

    private sealed class LiteralExpression(string literal,
        AwkRegexPatternOrigin patternOrigin = AwkRegexPatternOrigin.Runtime) : AwkSynchronousExpression
    {
        private readonly AwkText _literalText = AwkText.FromSourceLiteralString(literal);

        public AwkText LiteralText => _literalText;

        internal override AwkRegexPatternOrigin RegexPatternOrigin => patternOrigin;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.CreateByteTextValue(_literalText);
    }

    private sealed class VariableExpression(AwkVariableName name) : AwkSynchronousExpression
    {
        internal override bool TryResolveArrayArgument(AwkRuntimeState state, out AwkFunctionArgument argument) =>
            state.TryResolveArrayArgument(name, out argument);

        internal override int SynchronousDepth => AwkSpecialVariables.TryGetScalar(name, out _) ? 0 : 1;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveVariableValue(name);

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
            return true;
        }
    }

    private sealed class PreparedGlobalVariableExpression(AwkVariableName name, int index) : AwkSynchronousExpression
    {
        internal bool IsBoundScalarFor(AwkRuntimeState state, AwkIdentifier identifier) =>
            name.Identifier == identifier && state.HasBoundGlobalScalar(index);

        internal override bool TryResolveArrayArgument(AwkRuntimeState state, out AwkFunctionArgument argument)
        {
            if (!state.HasBoundGlobalScalar(index))
                return state.TryResolveArrayArgument(name, out argument);
            state.Counters.RecordBoundScalarArgument();
            argument = default;
            return false;
        }

        internal override int SynchronousDepth => AwkSpecialVariables.TryGetScalar(name, out _) ? 0 : 1;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolvePreparedGlobalValue(name, index);

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
            context.State.TryResolvePreparedGlobalNumber(index, out var number)
                ? number
                : EvaluateSynchronousCore(context).AsNumber(context.State.CancellationCheckpoint);

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
            return true;
        }
    }

    private sealed class LocalVariableExpression(
        AwkVariableName name,
        AwkFunctionLocalSlotId slotId) : AwkSynchronousExpression
    {
        internal override bool TryResolveArrayArgument(AwkRuntimeState state, out AwkFunctionArgument argument)
        {
            if (state.HasPreparedLocalScalarArgument(slotId))
            {
                argument = default;
                return false;
            }
            return state.TryResolveArrayArgument(name, out argument);
        }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveFunctionLocalValue(slotId, name);

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
            return true;
        }
    }

    private sealed class FieldArrayElementExpression(AwkArrayName name, DynamicFieldIndexExpression fieldOperand, int globalIndex) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => fieldOperand.SynchronousDepth is > 0 and < 16
            ? fieldOperand.SynchronousDepth + 1 : 0;
        internal override int SynchronousCallDepth => fieldOperand.SynchronousCallDepth;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            context.State.Checkpoint(); // Field expression, before its index.
            return fieldOperand.IndexExpression is AwkSynchronousExpression index
                ? context.State.GetArrayFieldValue(name, (int)index.ResolveNumberSynchronous(context), globalIndex)
                : throw new InvalidOperationException("Synchronous field index is required.");
        }

        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            var index = (int)await fieldOperand.IndexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return context.State.GetArrayFieldValue(name, index, globalIndex);
        }
    }

    private sealed class ArrayElementExpression(AwkArrayName name, AwkArrayIndex indexExpression, int globalIndex) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return context.State.GetArrayAwkValue(name, key, globalIndex);
        }
    }

    private sealed class NumberExpression(double number) : AwkSynchronousExpression
    {
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            AwkValue.FromNumber(number);

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) => number;

        internal override bool TryGetIntegerConstant(out int value)
        {
            if (Math.Abs(number % 1) < double.Epsilon &&
                number >= int.MinValue &&
                number <= int.MaxValue)
            {
                value = (int)number;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
