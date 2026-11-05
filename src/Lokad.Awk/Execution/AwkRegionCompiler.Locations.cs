using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private readonly record struct PreparedLocation(int Operand, int Access = -1);
    private readonly record struct PreparedUpdate(PreparedTarget Target, int Prior);

    private sealed class FieldWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
                {
                    compiler._scope.CheckCancellation();
                    syntax = syntax.Children[0];
                }
                if (syntax.Kind != AwkExpressionKind.Field || syntax.Children.Count != 1) return false;
                if (compiler.TryFixedField(syntax, out var field)) return Complete(compiler.Operand(AwkRegionOperandKind.Field, field));
                _waiting = true;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            var validated = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.FieldIndex, validated, compiler.NumericRegister(CompletedResult<int>(completed))));
            return Complete(compiler.Operand(AwkRegionOperandKind.DynamicField, validated));
        }
    }

    private sealed class LocationWork(AwkExpressionSyntax syntax, bool strict = false) : PreparationWork<PreparedLocation>
    {
        private bool _waiting;
        private bool _field;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                if (strict && syntax.Kind is not (AwkExpressionKind.Variable or AwkExpressionKind.Field or AwkExpressionKind.ArrayElement))
                    return false;
                if (compiler.Variable(syntax, out var variable))
                    return Complete(new(compiler.Operand(AwkRegionOperandKind.Variable, variable)));
                var unwrapped = syntax;
                while (unwrapped.Kind == AwkExpressionKind.Group && unwrapped.Children.Count == 1)
                {
                    compiler._scope.CheckCancellation();
                    unwrapped = unwrapped.Children[0];
                }
                _field = unwrapped.Kind == AwkExpressionKind.Field;
                _waiting = true;
                next = _field ? new FieldWork(syntax) : new ArrayAccessWork(syntax);
                return true;
            }
            var result = CompletedResult<int>(completed);
            return Complete(_field ? new(result) : new(compiler.Operand(AwkRegionOperandKind.ArrayElement, result), result));
        }
    }

    private sealed class TargetWork(AwkExpressionSyntax syntax) : PreparationWork<PreparedUpdate>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                _waiting = true;
                next = new LocationWork(syntax);
                return true;
            }
            var location = CompletedResult<PreparedLocation>(completed);
            if (location.Access < 0)
                return Complete(new(new(location.Operand, -1), compiler.CaptureNumber(location.Operand)));
            if (compiler._arrays[location.Access].Path >= 0)
            {
                var number = compiler._numbers++;
                var prior = compiler.Operand(AwkRegionOperandKind.NumberTemporary, number);
                compiler.Emit(new(AwkRegionInstructionKind.PathNumber, number, compiler._arrays[location.Access].Path));
                return Complete(new(new(location.Operand, -1), prior));
            }
            var captured = compiler._targetNames.Count;
            compiler._targetNames.Add(compiler._arrays[location.Access].Name);
            compiler.Emit(new(AwkRegionInstructionKind.ArrayPrepare, captured, location.Access));
            compiler.FinishKey(location.Access);
            return Complete(new(new(location.Operand, captured), compiler.Operand(AwkRegionOperandKind.ArrayPrevious, captured)));
        }
    }

    private sealed class AssignmentWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private byte _stage;
        private int _value;
        private PreparedUpdate _update;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (syntax.OperatorTokens[0].Kind == AwkTokenKind.Equal)
            {
                if (_stage == 0)
                {
                    _stage = 1;
                    next = compiler.CreateOperandWork(syntax.Children[1]);
                    return true;
                }
                if (_stage == 1)
                {
                    // Ordinary assignment owns its RHS before destination effects.
                    _value = compiler.CaptureValue(CompletedResult<int>(completed));
                    _stage = 2;
                    next = new LocationWork(syntax.Children[0]);
                    return true;
                }
                var location = CompletedResult<PreparedLocation>(completed);
                var operand = compiler._operands[location.Operand];
                if (location.Access >= 0)
                {
                    compiler.Emit(new(AwkRegionInstructionKind.ArrayAssign, location.Access, _value));
                    compiler.FinishKey(location.Access);
                }
                else if (operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField)
                    compiler.Emit(new(AwkRegionInstructionKind.AssignField, location.Operand, _value));
                else compiler.Assign(operand.Index, _value);
                return Complete(_value);
            }
            if (!AwkAssignmentOperators.TryGetCompoundOperation(syntax.OperatorTokens[0].Kind, out var operation)) return false;
            if (_stage == 0)
            {
                // Compound updates capture both location and old number first.
                _stage = 1;
                next = new TargetWork(syntax.Children[0]);
                return true;
            }
            if (_stage == 1)
            {
                _update = CompletedResult<PreparedUpdate>(completed);
                _stage = 2;
                next = compiler.CreateOperandWork(syntax.Children[1]);
                return true;
            }
            return Complete(compiler.Update(_update, CompletedResult<int>(completed), operation));
        }
    }

    private sealed class IncrementWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                _waiting = true;
                next = new TargetWork(syntax.Children[0]);
                return true;
            }
            var update = CompletedResult<PreparedUpdate>(completed);
            var decrement = syntax.OperatorTokens[0].Kind is AwkTokenKind.MinusMinus or AwkTokenKind.PostfixMinusMinus;
            // Postfix owns the old number beyond release of the captured target.
            var previous = syntax.Kind == AwkExpressionKind.Postfix ? compiler.CaptureNumber(update.Prior) : -1;
            var result = compiler.Update(update, compiler.Constant(decrement ? -1 : 1), AwkCompoundAssignmentOperation.Add);
            return Complete(previous >= 0 ? previous : result);
        }
    }

    private int Update(PreparedUpdate update, int right, AwkCompoundAssignmentOperation operation)
    {
        var number = _numbers++;
        var result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        if (update.Target.Capture >= 0)
        {
            Emit(new(AwkRegionInstructionKind.ArrayUpdate, number, update.Target.Capture,
                NumericRegister(right), (int)operation));
        }
        else
        {
            Emit(new(AwkRegionInstructionKind.Compound, number, NumericRegister(update.Prior),
                NumericRegister(right), (int)operation));
            Store(update.Target.Operand, result);
        }
        return result;
    }
}
