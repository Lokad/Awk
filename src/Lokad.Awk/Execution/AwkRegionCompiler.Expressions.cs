using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    // Preparation-only continuations. Emitted operations still execute in the one
    // common interpreter; this stack never survives program publication.
    private bool Expression(AwkExpressionSyntax syntax, out int result, bool preserveOrigin = false)
    {
        result = 0;
        // Temporary protection for recursive helper bridges. Remove after their
        // operands also use preparation frames, before deleting the AST fallback.
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        var current = CreateOperandWork(syntax, preserveOrigin);
        Stack<OperandWork>? callers = null;
        var completed = current;
        while (true)
        {
            _scope.CheckCancellation();
            if (current.TryAdvance(this, completed, out var next))
            {
                callers ??= new();
                callers.Push(current);
                current = next;
                completed = next;
                continue;
            }
            if (!current.Succeeded) return false;
            completed = current;
            if (callers is null || !callers.TryPop(out var caller))
            {
                result = current.Operand;
                return true;
            }
            current = caller;
        }
    }

    private OperandWork CreateOperandWork(AwkExpressionSyntax syntax, bool preserveOrigin = false)
    {
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        return syntax.Kind switch
        {
            AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 => new BinaryWork(syntax),
            AwkExpressionKind.Unary or AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1 => new UnaryWork(syntax),
            _ => new RecursiveOperandWork(syntax, preserveOrigin)
        };
    }

    private abstract class OperandWork
    {
        internal bool Succeeded { get; private set; }
        internal int Operand { get; private set; }
        protected bool Complete(int operand)
        {
            Succeeded = true;
            Operand = operand;
            return false;
        }
        internal abstract bool TryAdvance(AwkRegionCompiler compiler, OperandWork completed,
            [NotNullWhen(true)] out OperandWork? next);
    }

    // Explicit migration bridge for not-yet-converted preparation helpers.
    // It is not an execution path and must disappear with the recursive emitter.
    private sealed class RecursiveOperandWork(AwkExpressionSyntax syntax, bool preserveOrigin) : OperandWork
    {
        internal override bool TryAdvance(AwkRegionCompiler compiler, OperandWork completed,
            [NotNullWhen(true)] out OperandWork? next)
        {
            next = null;
            return compiler.ExpressionCore(syntax, out var operand, preserveOrigin) ? Complete(operand) : false;
        }
    }

    private sealed class UnaryWork(AwkExpressionSyntax syntax) : OperandWork
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, OperandWork completed,
            [NotNullWhen(true)] out OperandWork? next)
        {
            next = null;
            var token = syntax.OperatorTokens[0].Kind;
            if (!_waiting)
            {
                if (token is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus or AwkTokenKind.PostfixPlusPlus or AwkTokenKind.PostfixMinusMinus)
                    return compiler.PrepareIncrement(syntax, out var updated) ? Complete(updated) : false;
                _waiting = true;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            var operand = completed.Operand;
            if (token == AwkTokenKind.Plus) return Complete(compiler.CaptureNumber(operand));
            var destination = compiler._numbers++;
            if (token == AwkTokenKind.Minus)
                compiler.Emit(new(AwkRegionInstructionKind.Negate, destination, compiler.NumericRegister(operand)));
            else if (token == AwkTokenKind.Bang)
                compiler.Emit(new(AwkRegionInstructionKind.Not, destination, operand));
            else return false;
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, destination));
        }
    }

    private sealed class BinaryWork(AwkExpressionSyntax syntax) : OperandWork
    {
        private byte _stage;
        private int _left;
        private int _number;
        private int _alternative;
        private int _done;
        private bool _stringComparison;
        private AwkNumericOperation _arithmetic;
        private AwkComparison _comparison;

        internal override bool TryAdvance(AwkRegionCompiler compiler, OperandWork completed,
            [NotNullWhen(true)] out OperandWork? next)
        {
            next = null;
            var token = syntax.OperatorTokens[0].Kind;
            if (_stage == 0)
            {
                if (token == AwkTokenKind.In)
                    return compiler.Membership(syntax, out var member) ? Complete(member) : false;
                if (token is AwkTokenKind.Tilde or AwkTokenKind.BangTilde)
                    return compiler.RegexMatch(syntax, out var match) ? Complete(match) : false;
                _stage = 1;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            var logical = token is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe;
            if (_stage == 1)
            {
                _left = completed.Operand;
                if (logical)
                {
                    _number = compiler._numbers++;
                    _alternative = compiler.EmitBranch(_left, whenTrue: false);
                    if (token == AwkTokenKind.PipePipe)
                    {
                        compiler.Emit(new(AwkRegionInstructionKind.MoveNumber, _number, compiler.NumericRegister(compiler.Constant(1))));
                        _done = compiler.Emit(new(AwkRegionInstructionKind.Jump));
                        compiler.Patch(_alternative, compiler.Label());
                    }
                }
                else
                {
                    _arithmetic = token switch
                    {
                        AwkTokenKind.Plus => AwkNumericOperation.Add, AwkTokenKind.Minus => AwkNumericOperation.Subtract,
                        AwkTokenKind.Star => AwkNumericOperation.Multiply, AwkTokenKind.Slash => AwkNumericOperation.Divide,
                        AwkTokenKind.Percent => AwkNumericOperation.Modulo, AwkTokenKind.Caret => AwkNumericOperation.Power,
                        _ => (AwkNumericOperation)(-1)
                    };
                    _comparison = token switch
                    {
                        AwkTokenKind.EqualEqual => AwkComparison.Equal, AwkTokenKind.BangEqual => AwkComparison.NotEqual,
                        AwkTokenKind.Less => AwkComparison.LessThan, AwkTokenKind.LessEqual => AwkComparison.LessOrEqual,
                        AwkTokenKind.Greater => AwkComparison.GreaterThan, AwkTokenKind.GreaterEqual => AwkComparison.GreaterOrEqual,
                        _ => (AwkComparison)(-1)
                    };
                    if ((int)_arithmetic < 0 && (int)_comparison < 0) return false;
                    _stringComparison = compiler._operands[_left].Kind == AwkRegionOperandKind.Literal;
                    _left = (int)_arithmetic >= 0 ? compiler.CaptureNumber(_left)
                        : compiler.IsImmediateRead(syntax.Children[1]) ? _left : compiler.CaptureValue(_left);
                }
                _stage = 2;
                next = compiler.CreateOperandWork(syntax.Children[1]);
                return true;
            }
            var right = completed.Operand;
            if (logical)
            {
                compiler.Emit(new(AwkRegionInstructionKind.Truth, _number, right));
                if (token == AwkTokenKind.PipePipe) compiler.Patch(_done, compiler.Label());
                else
                {
                    var done = compiler.Emit(new(AwkRegionInstructionKind.Jump));
                    compiler.Patch(_alternative, compiler.Label());
                    compiler.Emit(new(AwkRegionInstructionKind.MoveNumber, _number, compiler.NumericRegister(compiler.Constant(0))));
                    compiler.Patch(done, compiler.Label());
                }
            }
            else
            {
                _number = compiler._numbers++;
                if ((int)_arithmetic >= 0)
                    compiler.Emit(new(AwkRegionInstructionKind.Arithmetic, _number, compiler.NumericRegister(_left), compiler.NumericRegister(right), (int)_arithmetic));
                else
                    compiler.Emit(new(_stringComparison || compiler._operands[right].Kind == AwkRegionOperandKind.Literal
                        ? AwkRegionInstructionKind.CompareText : AwkRegionInstructionKind.Compare,
                        _number, _left, right, (int)_comparison));
            }
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, _number));
        }
    }
}
