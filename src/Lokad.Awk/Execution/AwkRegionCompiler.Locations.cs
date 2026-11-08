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
                    return Complete(new(variable));
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
            var access = compiler._arrays[location.Access];
            var captured = compiler._updates.Count;
            var slot = compiler._activeUpdates++;
            compiler._updates.Add(new(access.Name, slot, Path: access.Path));
            compiler.Emit(new(AwkRegionInstructionKind.ArrayPrepare, slot, location.Access));
            // Nested keys must remain available to repair the logical owner if
            // RHS computation detaches or replaces the captured child.
            if (access.Path < 0) compiler.FinishKey(location.Access);
            return Complete(new(new(location.Operand, captured), -1));
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
                    compiler.PrepareImmediatePath(location.Access);
                    compiler.Emit(new(AwkRegionInstructionKind.ArrayAssign, location.Access, _value));
                    compiler.FinishKey(location.Access);
                }
                else if (operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField)
                    compiler.Emit(new(AwkRegionInstructionKind.AssignField, location.Operand, _value));
                else compiler.Assign(location.Operand, _value);
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
            // Complete array updates already own the old number. Publish its
            // observable postfix result directly, without another capture step.
            var oldNumber = syntax.Kind == AwkExpressionKind.Postfix && update.Target.Capture >= 0
                ? compiler._numbers++ : -1;
            var previous = oldNumber >= 0 ? compiler.Operand(AwkRegionOperandKind.NumberTemporary, oldNumber)
                : syntax.Kind == AwkExpressionKind.Postfix ? compiler.CaptureNumber(update.Prior) : -1;
            var result = compiler.Update(update, compiler.Constant(decrement ? -1 : 1),
                AwkCompoundAssignmentOperation.Add, oldNumber);
            return Complete(previous >= 0 ? previous : result);
        }
    }

    private int Update(PreparedUpdate update, int right, AwkCompoundAssignmentOperation operation, int oldNumber = -1)
    {
        var number = _numbers++;
        var result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        if (update.Target.Capture >= 0)
        {
            // Owned results keep immediate conversion (including funded call returns).
            // Other sources are already bound and belong to this complete operation.
            if (_operands[right].Kind == AwkRegionOperandKind.ValueTemporary) right = CaptureNumber(right);
            var site = _updates[update.Target.Capture] with
            {
                Right = AwkRegionNumberSource.Bind(right, _operands[right])
            };
            var captureIndex = _code.Count - 1;
            // Key retirement is part of the same prepared operation, not RHS work.
            if (captureIndex >= 0 && _code[captureIndex].Kind == AwkRegionInstructionKind.KeyEnd)
                captureIndex--;
            if (captureIndex >= 0 && _code[captureIndex] is { Kind: AwkRegionInstructionKind.ArrayPrepare } capture &&
                capture.Target == site.Slot)
            {
                var removed = _code.Count - captureIndex;
                _code.RemoveRange(captureIndex, removed);
                _straightLine -= removed;
                site = site with { PrepareAccess = capture.Left };
            }
            // Only captures crossing instructions need storage and rule retirement.
            if (site.PrepareAccess < 0) _updateSlots = Math.Max(_updateSlots, site.Slot + 1);
            _updates[update.Target.Capture] = site;
            if (site.PrepareAccess >= 0 && (site.Right.IsRegister ||
                _operands[site.Right.OperandIndex].Kind is AwkRegionOperandKind.LocalVariable or
                    AwkRegionOperandKind.GlobalVariable or AwkRegionOperandKind.Literal or
                    AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField))
                PrepareImmediatePath(site.PrepareAccess);
            Emit(new(AwkRegionInstructionKind.ArrayUpdate, number, update.Target.Capture, oldNumber,
                Detail: (int)operation));
            if (site.Path >= 0 && _paths[site.Path].ImmediateKeys is null)
                Emit(new(AwkRegionInstructionKind.PathEnd, site.Path));
            _activeUpdates--;
        }
        else if (_operands[update.Target.Operand].IsVariable)
        {
            // Captures crossing RHS instructions stay where they were evaluated.
            // An adjacent load can belong to this same complete operation; its
            // register still publishes the old number for postfix consumers.
            if (_operands[right].Kind == AwkRegionOperandKind.ValueTemporary) right = CaptureNumber(right);
            var previous = NumericRegister(update.Prior);
            var capturePrevious = _code.Count != 0 && _code[^1] is var capture &&
                capture.Target == previous && capture.Left == update.Target.Operand &&
                capture.Kind is AwkRegionInstructionKind.GlobalNumber or AwkRegionInstructionKind.LocalNumber or
                    AwkRegionInstructionKind.VariableNumber;
            if (capturePrevious)
            {
                _code.RemoveAt(_code.Count - 1);
                _straightLine--;
            }
            var site = _scalarUpdates.Count;
            _scalarUpdates.Add(new(update.Target.Operand, previous,
                AwkRegionNumberSource.Bind(right, _operands[right]), capturePrevious));
            Emit(new(AwkRegionInstructionKind.ScalarUpdate, number, site, Detail: (int)operation));
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
