using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    private sealed class FieldIndexExpression(int index) : AwkExpression
    {
        private readonly AwkFieldIndex _index = new(index);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            if (!_index.IsNamedField)
                return ValueTask.FromResult(AwkValue.FromText(string.Empty));

            return ValueTask.FromResult(context.State.GetFieldValue(_index));
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!_index.IsNamedField)
                return false;

            compiler.EmitLoadField(_index);
            compiler.Push();
            return true;
        }
    }

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
            context.State.RecordExecutionStep();
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
            context.State.RecordExecutionStep();
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

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!indexExpression.TryGetExecutableIntegerConstant(out var index) || index < 0)
                return false;

            // Constant folding must retain admission of the index expression.
            compiler.Emit(AwkOpcode.CheckBudget);
            if (index == 0)
                compiler.Emit(AwkOpcode.LoadCurrentRecord);
            else
                compiler.EmitLoadField(new AwkFieldIndex(index));

            compiler.Push();
            return true;
        }
    }

    private sealed class FieldCountExpression : AwkSynchronousExpression
    {
        public static readonly FieldCountExpression Instance = new();

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FieldCount));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadFieldCount);
            compiler.Push();
            return true;
        }
    }

    private sealed class FieldNfExpression : AwkExpression
    {
        public static readonly FieldNfExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.FieldCount == 0
                ? context.State.GetCurrentRecordValue()
                : context.State.GetFieldValue(context.State.FieldCount));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadLastField);
            compiler.Push();
            return true;
        }
    }

    private sealed class FieldZeroExpression : AwkExpression
    {
        public static readonly FieldZeroExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.GetCurrentRecordValue());

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadCurrentRecord);
            compiler.Push();
            return true;
        }
    }

    private sealed class RecordNumberExpression : AwkExpression
    {
        public static readonly RecordNumberExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordNumber));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadRecordNumber);
            compiler.Push();
            return true;
        }
    }

    private sealed class FileRecordNumberExpression : AwkExpression
    {
        public static readonly FileRecordNumberExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadFileRecordNumber);
            compiler.Push();
            return true;
        }
    }

    private sealed class FileNameExpression : AwkExpression
    {
        public static readonly FileNameExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveSpecialVariableValue(AwkSpecialVariableId.FileName));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadFileName);
            compiler.Push();
            return true;
        }
    }

    private sealed class LiteralExpression(string literal,
        AwkRegexPatternOrigin patternOrigin = AwkRegexPatternOrigin.Runtime) : AwkSynchronousExpression
    {
        private readonly AwkText _literalText = AwkText.FromSourceLiteralString(literal);

        public AwkText LiteralText => _literalText;

        internal override AwkRegexPatternOrigin RegexPatternOrigin => patternOrigin;

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.CreateByteTextValue(_literalText);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.EmitLoadConstant(AwkValue.FromText(_literalText));
            compiler.Push();
            return true;
        }
    }

    private sealed class VariableExpression(AwkVariableName name) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => AwkSpecialVariables.TryGetScalar(name, out _) ? 0 : 1;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveVariableValue(name);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.EmitLoadScalar(name);
            compiler.Push();
            return true;
        }

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
            return true;
        }
    }

    private sealed class PreparedGlobalVariableExpression(AwkVariableName name, int index) : AwkSynchronousExpression
    {
        internal override int SynchronousDepth => AwkSpecialVariables.TryGetScalar(name, out _) ? 0 : 1;
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolvePreparedGlobalValue(name, index);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.EmitLoadScalar(name);
            compiler.Push();
            return true;
        }

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
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveFunctionLocalValue(slotId, name);

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
            return true;
        }
    }

    private sealed class FieldArrayElementExpression(AwkArrayName name, DynamicFieldIndexExpression field) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            context.State.RecordExecutionStep();
            var index = (int)await field.IndexExpression.ResolveNumberAsync(context).ConfigureAwait(false);
            return context.State.GetArrayFieldValue(name, index);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!compiler.TryEmitExpression(field))
                return false;
            compiler.EmitLoadArrayElement(name);
            return true;
        }
    }

    private sealed class ArrayElementExpression(AwkArrayName name, AwkArrayIndex indexExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var key = await indexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
            return context.State.GetArrayAwkValue(name, key);
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!indexExpression.TryEmitExecutableKey(compiler))
                return false;

            compiler.EmitLoadArrayElement(name);
            return true;
        }
    }

    private sealed class NumberExpression(string text, double gnuDefaultValue) : AwkSynchronousExpression
    {
        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context) =>
            AwkValue.FromNumber(context.State.ResolveSourceNumberLiteral(text, gnuDefaultValue));

        protected override double EvaluateNumberSynchronousCore(AwkEvaluationContext context) =>
            context.State.ResolveSourceNumberLiteral(text, gnuDefaultValue);

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!TryResolvePortableExecutableNumber(text, gnuDefaultValue, out var value))
                return false;

            compiler.EmitLoadConstant(AwkValue.FromNumber(value));
            compiler.Push();
            return true;
        }

        internal override bool TryGetExecutableIntegerConstant(out int value)
        {
            if (TryResolvePortableExecutableNumber(text, gnuDefaultValue, out var resolvedValue) &&
                Math.Abs(resolvedValue % 1) < double.Epsilon &&
                resolvedValue >= int.MinValue &&
                resolvedValue <= int.MaxValue)
            {
                value = (int)resolvedValue;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool TryResolvePortableExecutableNumber(
            string text,
            double gnuDefaultValue,
            out double value)
        {
            if (IsPortableDecimalLiteral(text) &&
                AwkValue.TryParseNumber(
                    text,
                    AwkNumberParsingMode.Decimal,
                    AwkNumberTextExtent.EntireText,
                    out var decimalValue) &&
                decimalValue == gnuDefaultValue)
            {
                value = gnuDefaultValue;
                return true;
            }

            value = 0;
            return false;

            static bool IsPortableDecimalLiteral(string text)
            {
                if (text.Length == 0)
                    return false;

                for (var index = 0; index < text.Length; index++)
                {
                    var ch = text[index];
                    if (ch is >= '0' and <= '9' or '.' or '+' or '-' or 'e' or 'E')
                        continue;

                    return false;
                }

                return true;
            }
        }
    }
}
