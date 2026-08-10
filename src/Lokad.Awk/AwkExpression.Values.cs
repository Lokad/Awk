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
            if (!_index.IsNamedField || _index.Value > context.State.FieldCount)
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

    private sealed class DynamicFieldIndexExpression(AwkExpression indexExpression) : AwkExpression
    {
        protected override async ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            var index = (int)(await indexExpression.ResolveNumberAsync(context).ConfigureAwait(false));
            if (index == 0)
                return context.State.GetCurrentRecordValue();

            if (index < 0)
                throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

            if (index > context.State.FieldCount)
                return AwkValue.FromText(string.Empty);

            return context.State.GetFieldValue(new AwkFieldIndex(index));
        }

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            if (!indexExpression.TryGetExecutableIntegerConstant(out var index) || index < 0)
                return false;

            if (index == 0)
                compiler.Emit(AwkOpcode.LoadCurrentRecord);
            else
                compiler.EmitLoadField(new AwkFieldIndex(index));

            compiler.Push();
            return true;
        }
    }

    private sealed class FieldCountExpression : AwkExpression
    {
        public static readonly FieldCountExpression Instance = new();

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkValue.FromNumber(context.State.FieldCount));

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
                ? AwkValue.FromText(string.Empty)
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
            ValueTask.FromResult(AwkValue.FromNumber(context.State.RecordNumber));

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
            ValueTask.FromResult(AwkValue.FromNumber(context.State.FileRecordNumber));

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
            ValueTask.FromResult(context.State.CreateStringValue(context.State.FileName));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.Emit(AwkOpcode.LoadFileName);
            compiler.Push();
            return true;
        }
    }

    private sealed class LiteralExpression(string literal) : AwkExpression
    {
        private readonly AwkText _literalText = AwkText.FromSourceLiteralString(literal);

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.CreateByteTextValue(_literalText));

        internal override bool TryEmitExecutableValue(AwkExecutableCompiler compiler)
        {
            compiler.EmitLoadConstant(AwkValue.FromText(_literalText));
            compiler.Push();
            return true;
        }
    }

    private sealed class VariableExpression(AwkVariableName name) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveVariableValue(name));

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
        AwkFunctionLocalSlotId slotId) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(context.State.ResolveFunctionLocalValue(slotId, name));

        public override bool TryGetVariableName(out AwkVariableName variableName)
        {
            variableName = name;
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

    private sealed class NumberExpression(string text, double gnuDefaultValue) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkValue.FromNumber(
                context.State.ResolveSourceNumberLiteral(text, gnuDefaultValue)));

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
                    requireFullText: true,
                    out var decimalValue) &&
                decimalValue == gnuDefaultValue)
            {
                value = gnuDefaultValue;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool IsPortableDecimalLiteral(string text)
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
