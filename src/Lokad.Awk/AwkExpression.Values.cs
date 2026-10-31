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
                ? ResolveField(context, AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value)
                : throw new InvalidOperationException("Synchronous field index is required.");

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
            indexExpression is AwkSynchronousExpression index
                ? context.State.GetFieldNumber(AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value)
                : throw new InvalidOperationException("Synchronous field index is required.");

        internal override bool TryResolveSynchronousByteText(AwkEvaluationContext context,
            out AwkByteTextOperand operand)
        {
            if (indexExpression is AwkSynchronousExpression { SynchronousDepth: > 0 and < 16 } index)
            {
                context.State.Checkpoint();
                var fieldIndex = AwkFieldIndex.FromNumber(index.ResolveNumberSynchronous(context)).Value;
                operand = context.State.ResolveFieldByteOperand(fieldIndex);
                return true; // The index has been evaluated; never evaluate it again for fallback.
            }
            operand = default;
            return false;
        }

        internal override async ValueTask AppendByteTextAsync(AwkEvaluationContext context, AwkUtf8Builder destination)
        {
            context.State.Checkpoint();
            var index = AwkFieldIndex.FromNumber(await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false));
            context.State.AppendFieldByteText(destination, index.Value);
        }

        public ValueTask<bool> CompareLeftAsync(AwkEvaluationContext context, AwkValue left, AwkComparison comparison)
        {
            context.State.Checkpoint();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(context.State.CompareValueField(left, AwkFieldIndex.FromNumber(pending.Result).Value, comparison))
                : AwaitComparisonIndexAsync(context, left, comparison, pending);
        }

        private static async ValueTask<bool> AwaitComparisonIndexAsync(
            AwkEvaluationContext context, AwkValue left, AwkComparison comparison, ValueTask<double> pending) =>
            context.State.CompareValueField(left, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value, comparison);

        public override ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(context.State.GetFieldNumber(AwkFieldIndex.FromNumber(pending.Result).Value))
                : AwaitNumericIndexAsync(context, pending);
        }

        private static async ValueTask<double> AwaitNumericIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            context.State.GetFieldNumber(AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var pending = indexExpression.ResolveNumberAsync(context);
            return pending.IsCompletedSuccessfully
                ? ValueTask.FromResult(ResolveField(context, AwkFieldIndex.FromNumber(pending.Result).Value))
                : AwaitIndexAsync(context, pending);
        }

        private static async ValueTask<AwkValue> AwaitIndexAsync(
            AwkEvaluationContext context, ValueTask<double> pending) =>
            ResolveField(context, AwkFieldIndex.FromNumber(await pending.ConfigureAwait(false)).Value);

        private static AwkValue ResolveField(AwkEvaluationContext context, int index)
        {
            if (index == 0)
                return context.State.GetCurrentRecordValue();

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

        internal override bool TryResolveSynchronousByteText(AwkEvaluationContext context,
            out AwkByteTextOperand operand)
        {
            context.State.Checkpoint();
            operand = context.State.ResolveFieldByteOperand(0);
            return true;
        }

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

    private sealed class ArrayElementExpression(AwkArrayName name, AwkArrayIndex indexExpression,
        int globalIndex, Action? checkpoint = null) : AwkSynchronousExpression
    {
        private readonly (int Depth, int Calls) _shape = indexExpression.GetSynchronousShape(checkpoint);
        internal override int SynchronousDepth => _shape.Depth > 0 ? _shape.Depth + 1 : 0;
        internal override int SynchronousCallDepth => _shape.Calls;

        internal override bool TryPrepareFunctionCalls(
            IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression, Action? preparationCheckpoint = null)
        {
            expression = this;
            if (remainingDepth <= 0) return false;
            if (SynchronousDepth > 0) return true;
            if (remainingDepth <= 1 || !indexExpression.TryPrepareFunctionCalls(functions,
                remainingDepth - 1, out var index, preparationCheckpoint)) return false;
            expression = new ArrayElementExpression(name, index, globalIndex, preparationCheckpoint);
            return expression.SynchronousDepth is > 0 and <= 16;
        }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            indexExpression.ReadSynchronous(context, name, globalIndex);

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
            indexExpression.ReadNumberSynchronous(context, name, globalIndex);

        public override ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
        {
            context.State.Checkpoint();
            return indexExpression.ReadNumberAsync(context, name, globalIndex);
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            indexExpression.ReadAsync(context, name, globalIndex);
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
