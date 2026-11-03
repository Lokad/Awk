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
        return PrepareOperandWork(CreateOperandWork(syntax, preserveOrigin), out result);
    }

    private bool PrepareOperandWork(PreparationWork<int> work, out int result)
    {
        result = 0;
        if (!PrepareWork(work)) return false;
        result = work.Result;
        return true;
    }

    private bool PrepareWork(PreparationWork current)
    {
        Stack<PreparationWork>? callers = null;
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
            if (callers is null || !callers.TryPop(out var caller)) return true;
            current = caller;
        }
    }

    private PreparationWork<int> CreateOperandWork(AwkExpressionSyntax syntax, bool preserveOrigin = false)
    {
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        return syntax.Kind switch
        {
            AwkExpressionKind.Field when syntax.Children.Count == 1 => new FieldWork(syntax),
            AwkExpressionKind.ArrayElement => new ArrayReadWork(syntax, preserveOrigin),
            AwkExpressionKind.Assignment when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 => new AssignmentWork(syntax),
            AwkExpressionKind.Concatenation when syntax.Children.Count == 2 => new ConcatenationWork(syntax),
            AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 &&
                syntax.OperatorTokens[0].Kind == AwkTokenKind.In => new MembershipWork(syntax),
            AwkExpressionKind.Unary or AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1 &&
                syntax.OperatorTokens[0].Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus or
                    AwkTokenKind.PostfixPlusPlus or AwkTokenKind.PostfixMinusMinus => new IncrementWork(syntax),
            AwkExpressionKind.Conditional when syntax.Children.Count == 3 => new ConditionalWork(syntax, preserveOrigin),
            AwkExpressionKind.FunctionCall => CreateCallWork(syntax),
            AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 => new BinaryWork(syntax),
            AwkExpressionKind.Unary or AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1 => new UnaryWork(syntax),
            _ => new RecursiveOperandWork(syntax, preserveOrigin)
        };
    }

    private abstract class PreparationWork
    {
        internal bool Succeeded { get; private set; }
        protected bool Complete() { Succeeded = true; return false; }
        internal abstract bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next);
    }

    private abstract class PreparationWork<T> : PreparationWork where T : struct
    {
        internal T Result { get; private set; }
        protected bool Complete(T result) { Result = result; return Complete(); }
    }

    private static T CompletedResult<T>(PreparationWork completed) where T : struct =>
        completed is PreparationWork<T> work ? work.Result :
            throw new InvalidOperationException("The completed preparation has a different result type.");

    // Explicit migration bridge for not-yet-converted preparation helpers.
    // It is not an execution path and must disappear with the recursive emitter.
    private sealed class RecursiveOperandWork(AwkExpressionSyntax syntax, bool preserveOrigin) : PreparationWork<int>
    {
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            return compiler.ExpressionCore(syntax, out var operand, preserveOrigin) ? Complete(operand) : false;
        }
    }

    private sealed class UnaryWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var token = syntax.OperatorTokens[0].Kind;
            if (!_waiting)
            {
                _waiting = true;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            var operand = CompletedResult<int>(completed);
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

    private sealed class BinaryWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private byte _stage;
        private int _left;
        private int _number;
        private int _alternative;
        private int _done;
        private bool _stringComparison;
        private AwkNumericOperation _arithmetic;
        private AwkComparison _comparison;

        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var token = syntax.OperatorTokens[0].Kind;
            if (_stage == 0)
            {
                if (token is AwkTokenKind.Tilde or AwkTokenKind.BangTilde)
                    return compiler.RegexMatch(syntax, out var match) ? Complete(match) : false;
                _stage = 1;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            var logical = token is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe;
            if (_stage == 1)
            {
                _left = CompletedResult<int>(completed);
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
            var right = CompletedResult<int>(completed);
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
