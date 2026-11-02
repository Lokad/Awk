using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Stage B lowering covers ordinary scalar/named-array computation. Unmigrated constructs
// remain an explicit migration boundary; there are no arity/block-size/script guards.
internal sealed class AwkRegionCompiler
{
    private readonly AwkLoweringScope _scope;
    private readonly List<AwkRegionInstruction> _code = [];
    private readonly List<AwkRegionOperand> _operands = [];
    private readonly List<AwkRegionVariable> _variables = [];
    private readonly List<AwkRegionConstant> _constants = [];
    private readonly List<AwkRegionArrayAccess> _arrays = [];
    private readonly List<int> _builderSeparators = [];
    private readonly List<AwkRegionCall> _calls = [];
    private readonly List<AwkStatement> _hostStatements = [];
    private readonly List<AwkExpression> _hostExpressions = [];
    private int _activeCalls;
    private int _incomingCalls;
    private int _activeBuilders;
    private readonly Dictionary<long, int> _constantRegisters = [];
    private readonly Dictionary<AwkVariableName, int> _variableIndices = [];
    private readonly Stack<Loop> _loops = [];
    private int _numbers;
    private int _values;
    private int _targets;
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
            compiler._variables.ToArray(), compiler._constants.ToArray(), compiler._arrays.ToArray(),
            compiler._numbers, compiler._values, compiler._targets, compiler._builderSeparators.Count,
            compiler._calls.ToArray(), compiler._incomingCalls, compiler._hostStatements.ToArray(), compiler._hostExpressions.ToArray(), scope.IsFunction);
        return true;
    }

    private bool Statements(IReadOnlyList<AwkStatementSyntax> statements)
    {
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
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
                    Emit(new(AwkRegionInstructionKind.StatementBoundary));
                    var jump = Emit(new(AwkRegionInstructionKind.Jump));
                    (statement.Kind == AwkStatementKind.Break ? _loops.Peek().Breaks : _loops.Peek().Continues).Add(jump);
                    break;
                case AwkStatementKind.Next:
                case AwkStatementKind.NextFile:
                    Emit(new(AwkRegionInstructionKind.StatementBoundary));
                    Emit(new(AwkRegionInstructionKind.Flow, Detail: (int)(statement.Kind == AwkStatementKind.Next
                        ? AwkStatementFlow.NextRecord : AwkStatementFlow.NextFile)));
                    break;
                case AwkStatementKind.Return when _scope.IsFunction:
                    var returned = -1;
                    if (statement.Expression.TryGetValue(out var returning))
                    {
                        if (!Expression(returning, out var operand)) return false;
                        returned = CaptureValue(operand);
                    }
                    Emit(new(AwkRegionInstructionKind.StatementBoundary));
                    Emit(new(AwkRegionInstructionKind.Return, Left: returned));
                    break;
                case AwkStatementKind.Print:
                case AwkStatementKind.Printf:
                case AwkStatementKind.Getline:
                    // Migration boundary: retain the existing host primitive and its
                    // operand semantics until I/O operand lowering moves in Stage C.
                    var host = AwkStatementLowerer.LowerList([statement], _scope);
                    if (!host.Succeeded || host.Value.Count != 1) return false;
                    var hostIndex = _hostStatements.Count;
                    _hostStatements.Add(host.Value[0]);
                    Emit(new(AwkRegionInstructionKind.HostStatement, Left: hostIndex));
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

    private bool Expression(AwkExpressionSyntax syntax, out int result, bool argument = false)
    {
        _scope.CheckCancellation();
        result = 0;
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        switch (syntax.Kind)
        {
            case AwkExpressionKind.Group when syntax.Children.Count == 1:
                return Expression(syntax.Children[0], out result, argument);
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
            case AwkExpressionKind.ArrayElement:
                if (!ArrayAccess(syntax, out var access)) return false;
                if (_operands[_arrays[access].Key].Kind == AwkRegionOperandKind.KeyBuilder)
                {
                    // A reusable key buffer must be consumed before another access
                    // can reuse it. The captured scalar preserves its own spelling.
                    var captured = _values++;
                    Emit(new(AwkRegionInstructionKind.ArrayRead, captured, access, Detail: argument ? 1 : 0));
                    FinishKey(access);
                    result = Operand(AwkRegionOperandKind.ValueTemporary, captured);
                }
                else result = Operand(AwkRegionOperandKind.ArrayElement, access);
                return true;
            case AwkExpressionKind.Assignment when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Assignment(syntax, out result);
            case AwkExpressionKind.FunctionCall:
                return Call(syntax, out result);
            case AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Binary(syntax, out result);
            case AwkExpressionKind.Unary when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
            case AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
                return Unary(syntax, out result);
            case AwkExpressionKind.Conditional when syntax.Children.Count == 3:
                if (!Expression(syntax.Children[0], out var test)) return false;
                var alternate = Emit(new(AwkRegionInstructionKind.JumpIfFalse, Left: test));
                var target = _values++;
                if (!Expression(syntax.Children[1], out var first, argument)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, first, Detail: argument ? 1 : 0));
                var end = Emit(new(AwkRegionInstructionKind.Jump));
                Patch(alternate, Label());
                if (!Expression(syntax.Children[2], out var second, argument)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, second, Detail: argument ? 1 : 0));
                Patch(end, Label());
                result = Operand(AwkRegionOperandKind.ValueTemporary, target);
                return true;
            default: return false;
        }
    }

    private bool Call(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        if (!AwkExpressionSyntaxFacts.TryGetCallableTarget(syntax, out var target)) return false;
        if (AwkBuiltins.IsName(target.Name, AwkBuiltinId.Getline))
        {
            if (!AwkExpressionLowerer.TryLower(syntax, _scope, out var expression)) return false;
            var hostIndex = _hostExpressions.Count;
            _hostExpressions.Add(expression);
            var value = _values++;
            Emit(new(AwkRegionInstructionKind.HostExpression, value, hostIndex));
            result = Operand(AwkRegionOperandKind.ValueTemporary, value);
            return true;
        }
        if (_scope.Functions is not { } functions ||
            !functions.TryGetValue(new AwkFunctionName(target.Identifier), out var function)) return false;
        var slot = _activeCalls++;
        _incomingCalls = Math.Max(_incomingCalls, _activeCalls);
        var call = _calls.Count;
        _calls.Add(new(function, slot, syntax.Children.Count - 1));
        Emit(new(AwkRegionInstructionKind.BeginCall, Left: call));
        for (var index = 1; index < syntax.Children.Count; index++)
        {
            if (!Expression(syntax.Children[index], out var value, argument: true)) return false;
            Emit(new(AwkRegionInstructionKind.Argument, call, value, index - 1,
                syntax.Children[index].Kind == AwkExpressionKind.Group ? 1 : 0));
        }
        var returned = _values++;
        Emit(new(AwkRegionInstructionKind.Call, returned, call));
        _activeCalls--;
        result = Operand(AwkRegionOperandKind.ValueTemporary, returned);
        return true;
    }

    private bool Assignment(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var token = syntax.OperatorTokens[0].Kind;
        if (token == AwkTokenKind.Equal)
        {
            if (!Expression(syntax.Children[1], out var right)) return false;
            result = CaptureValue(right);
            if (Variable(syntax.Children[0], out var variable))
                Assign(variable, result);
            else
            {
                // Ordinary assignment evaluates its RHS before the subscript.
                if (!ArrayAccess(syntax.Children[0], out var access)) return false;
                Emit(new(AwkRegionInstructionKind.ArrayAssign, access, result));
                FinishKey(access);
            }
        }
        else
        {
            if (!AwkAssignmentOperators.TryGetCompoundOperation(token, out var operation)) return false;
            if (!PrepareTarget(syntax.Children[0], out var target, out var left)) return false;
            if (!Expression(syntax.Children[1], out var right)) return false;
            var number = _numbers++;
            Emit(new(AwkRegionInstructionKind.Compound, number, NumericRegister(left), NumericRegister(right), (int)operation));
            result = Operand(AwkRegionOperandKind.NumberTemporary, number);
            Store(target, result);
        }
        return true;
    }

    private bool ArrayAccess(AwkExpressionSyntax syntax, out int index)
    {
        index = 0;
        if (syntax.Children.Count < 2 || !AwkExpressionSyntaxFacts.TryGetArrayElementTarget(syntax, out var array) ||
            _scope.IsReservedVariable(array.Name)) return false;
        int key;
        if (syntax.Children.Count > 2)
        {
            var builder = _activeBuilders++;
            if (builder == _builderSeparators.Count) _builderSeparators.Add(_values++);
            var separator = _builderSeparators[builder];
            Emit(new(AwkRegionInstructionKind.KeyBegin, builder, separator));
            for (var component = 1; component < syntax.Children.Count; component++)
            {
                if (component > 1) Emit(new(AwkRegionInstructionKind.KeySeparator, builder, separator));
                if (!Expression(syntax.Children[component], out var value)) return false;
                Emit(new(AwkRegionInstructionKind.KeyAppend, builder, value));
            }
            // Separator bytes are already copied. Its old owner need not survive
            // lookup/insertion after a component has changed SUBSEP.
            Emit(new(AwkRegionInstructionKind.ReleaseValue, separator));
            key = Operand(AwkRegionOperandKind.KeyBuilder, builder);
        }
        else if (!Expression(syntax.Children[1], out key)) return false;
        // Nested subscript reads happen now, not recursively inside a later operand.
        if (_operands[key].Kind == AwkRegionOperandKind.ArrayElement) key = CaptureValue(key);
        index = _arrays.Count;
        _arrays.Add(new(new(array.Identifier), _scope.GetArrayBinding(array.Identifier), key));
        return true;
    }

    private void FinishKey(int access)
    {
        if (_operands[_arrays[access].Key] is not { Kind: AwkRegionOperandKind.KeyBuilder } key) return;
        Emit(new(AwkRegionInstructionKind.KeyEnd, key.Index));
        _activeBuilders--;
    }

    private bool PrepareTarget(AwkExpressionSyntax syntax, out PreparedTarget target, out int prior)
    {
        if (Variable(syntax, out var variable))
        {
            target = new(variable, -1);
            prior = CaptureNumber(Operand(AwkRegionOperandKind.Variable, variable));
            return true;
        }
        target = default;
        prior = 0;
        if (!ArrayAccess(syntax, out var access)) return false;
        var captured = _targets++;
        var number = _numbers++;
        Emit(new(AwkRegionInstructionKind.ArrayPrepare, captured, access, number));
        FinishKey(access);
        prior = Operand(AwkRegionOperandKind.NumberTemporary, number);
        target = new(access, captured);
        return true;
    }

    private void Store(PreparedTarget target, int value)
    {
        if (target.Capture >= 0)
            Emit(new(AwkRegionInstructionKind.ArrayCommit, target.Capture, NumericRegister(value), target.Binding));
        else Assign(target.Binding, value);
    }

    private void Assign(int variable, int value)
    {
        if (_operands[value].Kind != AwkRegionOperandKind.NumberTemporary)
        {
            Emit(new(AwkRegionInstructionKind.Assign, variable, value));
            return;
        }
        var kind = _variables[variable] switch
        {
            { LocalIndex: >= 0 } => AwkRegionInstructionKind.AssignLocalNumber,
            { GlobalIndex: >= 0 } => AwkRegionInstructionKind.AssignGlobalNumber,
            _ => AwkRegionInstructionKind.AssignVariableNumber
        };
        Emit(new(kind, variable, NumericRegister(value)));
    }

    private bool Unary(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var token = syntax.OperatorTokens[0].Kind;
        if (token is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus or AwkTokenKind.PostfixPlusPlus or AwkTokenKind.PostfixMinusMinus)
        {
            if (!PrepareTarget(syntax.Children[0], out var target, out var prior)) return false;
            var number = _numbers++;
            var decrement = token is AwkTokenKind.MinusMinus or AwkTokenKind.PostfixMinusMinus;
            Emit(new(AwkRegionInstructionKind.Arithmetic, number, NumericRegister(prior), NumericRegister(Constant(decrement ? -1 : 1)), (int)AwkNumericOperation.Add));
            var updated = Operand(AwkRegionOperandKind.NumberTemporary, number);
            Store(target, updated);
            result = syntax.Kind == AwkExpressionKind.Postfix ? prior : updated;
            return true;
        }
        if (!Expression(syntax.Children[0], out var operand)) return false;
        if (token == AwkTokenKind.Plus) { result = CaptureNumber(operand); return true; }
        var destination = _numbers++;
        if (token == AwkTokenKind.Minus)
            Emit(new(AwkRegionInstructionKind.Negate, destination, NumericRegister(operand)));
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
                Emit(new(AwkRegionInstructionKind.MoveNumber, number, NumericRegister(Constant(1))));
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
                Emit(new(AwkRegionInstructionKind.MoveNumber, number, NumericRegister(Constant(0))));
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
        if ((int)arithmetic >= 0)
            Emit(new(AwkRegionInstructionKind.Arithmetic, target, NumericRegister(left), NumericRegister(operand), (int)arithmetic));
        else
            Emit(new(AwkRegionInstructionKind.Compare, target, left, operand, (int)comparison));
        result = Operand(AwkRegionOperandKind.NumberTemporary, target);
        return true;
    }

    private bool IsImmediateRead(AwkExpressionSyntax syntax)
    {
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        return syntax.Kind is AwkExpressionKind.Number or AwkExpressionKind.Variable ||
            syntax.Kind == AwkExpressionKind.Field && syntax.Children.Count == 1 && syntax.Children[0].Kind == AwkExpressionKind.Number;
    }

    private bool Variable(AwkExpressionSyntax syntax, out int index)
    {
        index = 0;
        if (!AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var identifier) ||
            _scope.IsReservedVariable(identifier.Name) || AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length)) return false;
        var name = new AwkVariableName(identifier.Identifier);
        if (!_variableIndices.TryGetValue(name, out index))
        {
            index = _variables.Count;
            _variableIndices.Add(name, index);
            var local = _scope.TryGetFunctionLocalSlot(name, out var slot) ? slot.Index : -1;
            _variables.Add(new(name, local < 0 && _scope.TryGetGlobalReadIndex(name, out var global) ? global : -1, local));
        }
        return true;
    }

    private int CaptureNumber(int operand)
    {
        var source = _operands[operand];
        if (source.Kind == AwkRegionOperandKind.NumberTemporary) return operand;
        var target = _numbers++;
        var kind = source.Kind switch
        {
            AwkRegionOperandKind.Field => AwkRegionInstructionKind.FieldNumber,
            AwkRegionOperandKind.ValueTemporary => AwkRegionInstructionKind.ValueNumber,
            AwkRegionOperandKind.ArrayElement => AwkRegionInstructionKind.ArrayNumber,
            AwkRegionOperandKind.Variable => _variables[source.Index] switch
            {
                { LocalIndex: >= 0 } => AwkRegionInstructionKind.LocalNumber,
                { GlobalIndex: >= 0 } => AwkRegionInstructionKind.GlobalNumber,
                _ => AwkRegionInstructionKind.VariableNumber
            },
            _ => throw new InvalidOperationException("A numeric operand requires a value location.")
        };
        Emit(new(kind, target, source.Index));
        return Operand(AwkRegionOperandKind.NumberTemporary, target);
    }

    private int CaptureValue(int operand)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.NumberTemporary or AwkRegionOperandKind.ValueTemporary)
            return operand;
        var target = _values++;
        Emit(new(AwkRegionInstructionKind.Value, target, operand));
        return Operand(AwkRegionOperandKind.ValueTemporary, target);
    }

    private void Discard(int operand)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.Variable or AwkRegionOperandKind.Field or AwkRegionOperandKind.ArrayElement)
            Emit(new(AwkRegionInstructionKind.Discard, Left: operand));
    }

    private int Constant(double value)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        if (!_constantRegisters.TryGetValue(bits, out var register))
        {
            register = _numbers++;
            _constantRegisters.Add(bits, register);
            _constants.Add(new(register, value));
        }
        return Operand(AwkRegionOperandKind.NumberTemporary, register);
    }

    private int NumericRegister(int operand) => _operands[CaptureNumber(operand)].Index;

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

    private readonly record struct PreparedTarget(int Binding, int Capture);
}
