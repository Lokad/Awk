using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Stage B lowering starts with ordinary scalar computation. Unsupported constructs
// remain an explicit migration boundary; there are no arity/block-size/script guards.
internal sealed class AwkRegionCompiler
{
    private readonly AwkLoweringScope _scope;
    private readonly List<AwkRegionInstruction> _code = [];
    private readonly List<AwkRegionOperand> _operands = [];
    private readonly List<AwkRegionVariable> _variables = [];
    private readonly List<double> _constants = [];
    private readonly Dictionary<AwkVariableName, int> _variableIndices = [];
    private readonly Stack<Loop> _loops = [];
    private int _numbers;
    private int _values;
    private int _straightLine;

    private AwkRegionCompiler(AwkLoweringScope scope) => _scope = scope;

    internal static bool TryCompile(IReadOnlyList<AwkStatementSyntax> statements,
        AwkLoweringScope scope, [NotNullWhen(true)] out AwkRegion? region)
    {
        var compiler = new AwkRegionCompiler(scope);
        compiler.Label();
        if (!compiler.Statements(statements))
        {
            region = null;
            return false;
        }
        region = new(compiler._code.ToArray(), compiler._operands.ToArray(),
            compiler._variables.ToArray(), compiler._constants.ToArray(), compiler._numbers, compiler._values);
        return true;
    }

    private bool Statements(IReadOnlyList<AwkStatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            _scope.CheckCancellation();
            switch (statement.Kind)
            {
                case AwkStatementKind.Block:
                    if (!Statements(statement.BodyStatements)) return false;
                    break;
                case AwkStatementKind.Expression:
                    if (!statement.Expression.TryGetValue(out var expression) || !Expression(expression, out var value)) return false;
                    Discard(value);
                    break;
                case AwkStatementKind.If:
                    if (!statement.Expression.TryGetValue(out var condition) || !Expression(condition, out var test)) return false;
                    var otherwise = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: test));
                    if (!Statements(statement.BodyStatements)) return false;
                    var done = Emit(new(AwkRegionInstructionKind.Jump));
                    Patch(otherwise, Label());
                    if (!Statements(statement.ElseStatements)) return false;
                    Patch(done, Label());
                    break;
                case AwkStatementKind.While:
                case AwkStatementKind.DoWhile:
                case AwkStatementKind.For:
                    if (!CompileLoop(statement)) return false;
                    break;
                case AwkStatementKind.Break:
                case AwkStatementKind.Continue:
                    if (_loops.Count == 0) return false;
                    var jump = Emit(new(AwkRegionInstructionKind.Jump));
                    (statement.Kind == AwkStatementKind.Break ? _loops.Peek().Breaks : _loops.Peek().Continues).Add(jump);
                    break;
                case AwkStatementKind.Next:
                case AwkStatementKind.NextFile:
                    Emit(new(AwkRegionInstructionKind.StatementBoundary));
                    Emit(new(AwkRegionInstructionKind.Flow, Detail: (int)(statement.Kind == AwkStatementKind.Next
                        ? AwkStatementFlow.NextRecord : AwkStatementFlow.NextFile)));
                    break;
                default: return false;
            }
            Emit(new(AwkRegionInstructionKind.StatementBoundary));
        }
        return true;
    }

    private bool CompileLoop(AwkStatementSyntax statement)
    {
        AwkForHeaderSyntax? header = null;
        if (statement.Kind == AwkStatementKind.For)
        {
            if (statement.Payload is not AwkForStatementSyntaxPayload payload || payload.Header.IsForIn || payload.Header.Parts.Count != 3)
                return false;
            header = payload.Header;
            if (header.Parts[0].Expression.TryGetValue(out var initial))
            {
                if (!Expression(initial, out var value)) return false;
                Discard(value);
            }
        }

        var start = Label();
        var conditionJump = -1;
        if (statement.Kind != AwkStatementKind.DoWhile)
        {
            var condition = header is null ? statement.Expression : header.Parts[1].Expression;
            if (condition.TryGetValue(out var expression))
            {
                if (!Expression(expression, out var value)) return false;
                conditionJump = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: value));
            }
        }
        var loop = new Loop();
        _loops.Push(loop);
        if (!Statements(statement.BodyStatements)) return false;
        _loops.Pop();
        var continuation = Label();
        foreach (var position in loop.Continues) Patch(position, continuation);
        if (header is not null && header.Parts[2].Expression.TryGetValue(out var step))
        {
            if (!Expression(step, out var value)) return false;
            Discard(value);
        }
        if (statement.Kind == AwkStatementKind.DoWhile)
        {
            if (!statement.Expression.TryGetValue(out var condition) || !Expression(condition, out var value)) return false;
            conditionJump = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: value));
        }
        Emit(new(AwkRegionInstructionKind.Jump, start));
        var end = Label();
        if (conditionJump >= 0) Patch(conditionJump, end);
        foreach (var position in loop.Breaks) Patch(position, end);
        return true;
    }

    private bool Expression(AwkExpressionSyntax syntax, out int result)
    {
        _scope.CheckCancellation();
        result = 0;
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        switch (syntax.Kind)
        {
            case AwkExpressionKind.Group when syntax.Children.Count == 1:
                return Expression(syntax.Children[0], out result);
            case AwkExpressionKind.Number:
                if (!AwkExpressionLowerer.TryReadSwitchConstant(syntax, _scope, out var number)) return false;
                result = Constant(number.AsNumber());
                return true;
            case AwkExpressionKind.Variable:
                if (!Variable(syntax, out var variable)) return false;
                result = Operand(AwkRegionOperandKind.Variable, variable);
                return true;
            case AwkExpressionKind.Field when syntax.Children.Count == 1:
                if (syntax.Children[0].Kind != AwkExpressionKind.Number) return false;
                if (!AwkExpressionLowerer.TryReadSwitchConstant(syntax.Children[0], _scope, out var ordinal)) return false;
                var field = ordinal.AsNumber();
                if (!double.IsFinite(field) || field < 0 || field > int.MaxValue) return false;
                result = Operand(AwkRegionOperandKind.Field, (int)field);
                return true;
            case AwkExpressionKind.Assignment when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Assignment(syntax, out result);
            case AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Binary(syntax, out result);
            case AwkExpressionKind.Unary when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
            case AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
                return Unary(syntax, out result);
            case AwkExpressionKind.Conditional when syntax.Children.Count == 3:
                if (!Expression(syntax.Children[0], out var test)) return false;
                var alternate = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: test));
                var target = _values++;
                if (!Expression(syntax.Children[1], out var first)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, first));
                var end = Emit(new(AwkRegionInstructionKind.Jump));
                Patch(alternate, Label());
                if (!Expression(syntax.Children[2], out var second)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, second));
                Patch(end, Label());
                result = Operand(AwkRegionOperandKind.ValueTemporary, target);
                return true;
            default: return false;
        }
    }

    private bool Assignment(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        if (!Variable(syntax.Children[0], out var target)) return false;
        var token = syntax.OperatorTokens[0].Kind;
        if (token == AwkTokenKind.Equal)
        {
            if (!Expression(syntax.Children[1], out var right)) return false;
            result = CaptureValue(right);
        }
        else
        {
            if (!AwkAssignmentOperators.TryGetCompoundOperation(token, out var operation)) return false;
            var left = CaptureNumber(Operand(AwkRegionOperandKind.Variable, target));
            if (!Expression(syntax.Children[1], out var right)) return false;
            var number = _numbers++;
            Emit(new(AwkRegionInstructionKind.Compound, number, left, right, (int)operation));
            result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        }
        Emit(new(AwkRegionInstructionKind.Assign, target, result));
        return true;
    }

    private bool Unary(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var token = syntax.OperatorTokens[0].Kind;
        if (token is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus or AwkTokenKind.PostfixPlusPlus or AwkTokenKind.PostfixMinusMinus)
        {
            if (!Variable(syntax.Children[0], out var target)) return false;
            var prior = CaptureNumber(Operand(AwkRegionOperandKind.Variable, target));
            var number = _numbers++;
            var decrement = token is AwkTokenKind.MinusMinus or AwkTokenKind.PostfixMinusMinus;
            Emit(new(AwkRegionInstructionKind.Arithmetic, number, prior, Constant(decrement ? -1 : 1), (int)AwkNumericOperation.Add));
            var updated = Operand(AwkRegionOperandKind.NumberTemporary, number);
            Emit(new(AwkRegionInstructionKind.Assign, target, updated));
            result = syntax.Kind == AwkExpressionKind.Postfix ? prior : updated;
            return true;
        }
        if (!Expression(syntax.Children[0], out var operand)) return false;
        if (token == AwkTokenKind.Plus) { result = CaptureNumber(operand); return true; }
        var destination = _numbers++;
        if (token == AwkTokenKind.Minus)
            Emit(new(AwkRegionInstructionKind.Negate, destination, operand));
        else if (token == AwkTokenKind.Bang)
            Emit(new(AwkRegionInstructionKind.Not, destination, operand));
        else return false;
        result = Operand(AwkRegionOperandKind.NumberTemporary, destination);
        return true;
    }

    private bool Binary(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var token = syntax.OperatorTokens[0].Kind;
        if (!Expression(syntax.Children[0], out var left)) return false;
        if (token is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe)
        {
            var number = _numbers++;
            var alternative = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: left));
            if (token == AwkTokenKind.PipePipe)
            {
                Emit(new(AwkRegionInstructionKind.Number, number, Constant(1)));
                var done = Emit(new(AwkRegionInstructionKind.Jump));
                Patch(alternative, Label());
                if (!Expression(syntax.Children[1], out var right)) return false;
                Emit(new(AwkRegionInstructionKind.Truth, number, right));
                Patch(done, Label());
            }
            else
            {
                if (!Expression(syntax.Children[1], out var right)) return false;
                Emit(new(AwkRegionInstructionKind.Truth, number, right));
                var done = Emit(new(AwkRegionInstructionKind.Jump));
                Patch(alternative, Label());
                Emit(new(AwkRegionInstructionKind.Number, number, Constant(0)));
                Patch(done, Label());
            }
            result = Operand(AwkRegionOperandKind.NumberTemporary, number);
            return true;
        }

        var arithmetic = token switch
        {
            AwkTokenKind.Plus => AwkNumericOperation.Add, AwkTokenKind.Minus => AwkNumericOperation.Subtract,
            AwkTokenKind.Star => AwkNumericOperation.Multiply, AwkTokenKind.Slash => AwkNumericOperation.Divide,
            AwkTokenKind.Percent => AwkNumericOperation.Modulo, AwkTokenKind.Caret => AwkNumericOperation.Power,
            _ => (AwkNumericOperation)(-1)
        };
        var comparison = token switch
        {
            AwkTokenKind.EqualEqual => AwkComparison.Equal, AwkTokenKind.BangEqual => AwkComparison.NotEqual,
            AwkTokenKind.Less => AwkComparison.LessThan, AwkTokenKind.LessEqual => AwkComparison.LessOrEqual,
            AwkTokenKind.Greater => AwkComparison.GreaterThan, AwkTokenKind.GreaterEqual => AwkComparison.GreaterOrEqual,
            _ => (AwkComparison)(-1)
        };
        if ((int)arithmetic < 0 && (int)comparison < 0) return false;
        // Immediate operands are consumed in source order inside one operation.
        // A computed RHS needs an owned capture before its operations can run.
        left = (int)arithmetic >= 0 ? CaptureNumber(left) : IsImmediateRead(syntax.Children[1]) ? left : CaptureValue(left);
        if (!Expression(syntax.Children[1], out var operand)) return false;
        var target = _numbers++;
        Emit(new((int)arithmetic >= 0 ? AwkRegionInstructionKind.Arithmetic : AwkRegionInstructionKind.Compare,
            target, left, operand, (int)arithmetic >= 0 ? (int)arithmetic : (int)comparison));
        result = Operand(AwkRegionOperandKind.NumberTemporary, target);
        return true;
    }

    private static bool IsImmediateRead(AwkExpressionSyntax syntax) => syntax.Kind switch
    {
        AwkExpressionKind.Number or AwkExpressionKind.Variable => true,
        AwkExpressionKind.Field => syntax.Children.Count == 1 && syntax.Children[0].Kind == AwkExpressionKind.Number,
        AwkExpressionKind.Group => syntax.Children.Count == 1 && IsImmediateRead(syntax.Children[0]),
        _ => false
    };

    private bool Variable(AwkExpressionSyntax syntax, out int index)
    {
        index = 0;
        if (!AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var identifier) ||
            _scope.IsReservedVariable(identifier.Name) || AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length)) return false;
        var name = new AwkVariableName(identifier.Identifier);
        if (_scope.TryGetFunctionLocalSlot(name, out _)) return false; // Stage B call-frame migration follows.
        if (!_variableIndices.TryGetValue(name, out index))
        {
            index = _variables.Count;
            _variableIndices.Add(name, index);
            _variables.Add(new(name, _scope.TryGetGlobalReadIndex(name, out var global) ? global : -1));
        }
        return true;
    }

    private int CaptureNumber(int operand)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.Number or AwkRegionOperandKind.NumberTemporary) return operand;
        var target = _numbers++;
        Emit(new(AwkRegionInstructionKind.Number, target, operand));
        return Operand(AwkRegionOperandKind.NumberTemporary, target);
    }

    private int CaptureValue(int operand)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.Number or AwkRegionOperandKind.NumberTemporary or AwkRegionOperandKind.ValueTemporary)
            return operand;
        var target = _values++;
        Emit(new(AwkRegionInstructionKind.Value, target, operand));
        return Operand(AwkRegionOperandKind.ValueTemporary, target);
    }

    private void Discard(int operand)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.Variable or AwkRegionOperandKind.Field)
            Emit(new(AwkRegionInstructionKind.Discard, Left: operand));
    }

    private int Constant(double value)
    {
        var index = _constants.Count;
        _constants.Add(value);
        return Operand(AwkRegionOperandKind.Number, index);
    }

    private int Operand(AwkRegionOperandKind kind, int index)
    {
        _operands.Add(new(kind, index));
        return _operands.Count - 1;
    }

    private int Emit(AwkRegionInstruction instruction)
    {
        if (_straightLine == 128)
        {
            _code.Add(new(AwkRegionInstructionKind.Poll));
            _straightLine = 0;
        }
        _straightLine++;
        _code.Add(instruction);
        return _code.Count - 1;
    }

    private int Label()
    {
        var index = _code.Count;
        _code.Add(new(AwkRegionInstructionKind.Poll));
        _straightLine = 0;
        return index;
    }

    private void Patch(int position, int target) => _code[position] = _code[position] with { Target = target };

    private sealed class Loop
    {
        internal List<int> Breaks { get; } = [];
        internal List<int> Continues { get; } = [];
    }
}
