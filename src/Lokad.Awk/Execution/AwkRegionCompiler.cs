using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Lowering covers scalar/array computation, direct calls and synchronous builtins. Unmigrated constructs
// remain an explicit migration boundary; there are no arity/block-size/script guards.
internal sealed class AwkRegionCompiler
{
    private readonly AwkLoweringScope _scope;
    private readonly AwkRegionKind _kind;
    private readonly List<AwkRegionInstruction> _code = [];
    private readonly List<AwkRegionOperand> _operands = [];
    private readonly List<AwkRegionVariable> _variables = [];
    private readonly List<AwkRegionConstant> _constants = [];
    private readonly List<AwkText> _literals = [];
    private readonly List<AwkRegionArrayAccess> _arrays = [];
    private readonly List<AwkRegionPath> _paths = [];
    private readonly List<int> _builderSeparators = [];
    private readonly List<AwkRegionCall> _calls = [];
    private readonly List<AwkRegionBuiltin> _builtins = [];
    private readonly List<AwkRegionStringCall> _strings = [];
    private readonly List<AwkRegionPrint> _prints = [];
    private readonly List<AwkRegionPrintf> _printfs = [];
    private readonly List<AwkRegionGetline> _getlines = [];
    private readonly List<AwkStatement> _hostStatements = [];
    private int _activeCalls;
    private int _incomingCalls;
    private int _activeBuilders;
    private readonly Dictionary<long, int> _constantRegisters = [];
    private readonly Dictionary<AwkVariableName, int> _variableIndices = [];
    private readonly Stack<Loop> _loops = [];
    private int _numbers;
    private int _values;
    private int _texts;
    private int _printSegments;
    private int _targets;
    private int _iterators;
    private int _straightLine;
    private bool _usesOutputBuilder;

    private AwkRegionCompiler(AwkLoweringScope scope, AwkRegionKind kind) => (_scope, _kind) = (scope, kind);

    internal static bool TryCompile(IReadOnlyList<AwkStatementSyntax> statements,
        AwkLoweringScope scope, [NotNullWhen(true)] out AwkRegion? region) =>
        new AwkRegionCompiler(scope, scope.IsFunction ? AwkRegionKind.Function : AwkRegionKind.Record)
            .Compile(statements, null, defaultPrint: false, out region);

    internal static bool TryCompileRule(AwkRuleSyntax rule, AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkRegion? region)
    {
        AwkExpressionSyntax? condition = null;
        if (rule.Kind == AwkRuleKind.Main && rule.PatternTokens.Count != 0)
        {
            if (rule.PatternSyntax is not AwkSinglePatternSyntax { Expression: { } expression })
            {
                region = null;
                return false;
            }
            condition = expression;
        }
        return new AwkRegionCompiler(scope, rule.Kind == AwkRuleKind.Main ? AwkRegionKind.Record : AwkRegionKind.Phase)
            .Compile(rule.ActionStatements, condition, defaultPrint: !rule.HasExplicitAction, out region);
    }

    private bool Compile(IReadOnlyList<AwkStatementSyntax> statements, AwkExpressionSyntax? condition,
        bool defaultPrint, [NotNullWhen(true)] out AwkRegion? region)
    {
        Label();
        List<int>? skipped = null;
        if (condition is not null)
        {
            skipped = [];
            if (!Branch(condition, whenTrue: false, skipped))
            {
                region = null;
                return false;
            }
        }
        if (!Statements(statements))
        {
            region = null;
            return false;
        }
        if (defaultPrint)
        {
            Emit(new(AwkRegionInstructionKind.PrintField, Left: Operand(AwkRegionOperandKind.Field, 0)));
            Emit(new(AwkRegionInstructionKind.StatementBoundary));
        }
        if (skipped is not null)
        {
            PatchBranches(skipped, Label());
            // A false condition can still have queued diagnostics. Preserve the
            // action boundary even when none of its statements executes.
            Emit(new(AwkRegionInstructionKind.StatementBoundary));
        }
        region = new(_code.ToArray(), _operands.ToArray(),
            _variables.ToArray(), _constants.ToArray(), _literals.ToArray(), _arrays.ToArray(),
            new(_numbers, _values, _texts, _targets, Math.Max(_builderSeparators.Count, _usesOutputBuilder ? 1 : 0), _incomingCalls, _printSegments, _getlines.Count, _paths.Count, _iterators),
            _calls.ToArray(), _hostStatements.ToArray(),
            _builtins.ToArray(), _prints.ToArray(), _printfs.ToArray(), _getlines.ToArray(), _strings.ToArray(), _paths.ToArray(), _kind);
        return true;
    }

    private bool Statements(IReadOnlyList<AwkStatementSyntax> statements)
    {
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        foreach (var statement in statements)
        {
            var firstTemporary = _values;
            _scope.CheckCancellation();
            if (AwkStatementKindFacts.IsOptionalValue(statement.Kind) &&
                    (statement.Expressions.Count > 1 || statement.Expressions.Count == 0 &&
                        AwkTokenNavigation.FindNextSignificant(statement.Tokens, 1) is not null) ||
                AwkStatementValidationFacts.IsArgumentless(statement.Kind) && statement.Expressions.Count != 0 ||
                statement.Kind == AwkStatementKind.Expression && statement.Expressions.Count != 1 ||
                statement.Kind is AwkStatementKind.If or AwkStatementKind.While or AwkStatementKind.DoWhile && statement.Expressions.Count != 1)
                return false;
            switch (statement.Kind)
            {
                case AwkStatementKind.Block:
                    if (!Statements(statement.BodyStatements)) return false;
                    break;
                case AwkStatementKind.Expression:
                    if (!statement.Expression.TryGetValue(out var expression)) return false;
                    if (!CopyField(expression))
                    {
                        if (!Expression(expression, out var value)) return false;
                        Discard(value);
                    }
                    break;
                case AwkStatementKind.If:
                    var otherwise = new List<int>();
                    if (!statement.Expression.TryGetValue(out var condition) || !Branch(condition, whenTrue: false, otherwise)) return false;
                    if (!Statements(statement.BodyStatements)) return false;
                    var done = Emit(new(AwkRegionInstructionKind.Jump));
                    PatchBranches(otherwise, Label());
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
                    if (!AwkStatementLoweringShapes.Output(statement).TryGetValue(out var output)) return false;
                    if (output.Redirection.TryGetValue(out var printRedirection) && printRedirection.Kind == AwkOutputRedirectionKind.Coprocess)
                    {
                        if (!LegacyHostStatement(statement)) return false;
                        break;
                    }
                    if (!OutputTarget(output.Redirection, out var printTarget)) return false;
                    if (output.Output.IsEmpty)
                    {
                        if (printTarget.IsRedirected)
                        {
                            var recordPrint = AllocatePrint(1, [], printTarget);
                            Emit(new(AwkRegionInstructionKind.Value, recordPrint.Values, Operand(AwkRegionOperandKind.Field, 0), Detail: 1));
                            EmitPrint(recordPrint);
                        }
                        else Emit(new(AwkRegionInstructionKind.PrintField, Left: Operand(AwkRegionOperandKind.Field, 0)));
                        break;
                    }
                    if (!output.Output.Expression.TryGetValue(out var printed)) return false;
                    if (printed.Kind == AwkExpressionKind.Group && printed.FirstChild is { Kind: AwkExpressionKind.ExpressionList } list)
                        printed = list;
                    if (printed.Kind == AwkExpressionKind.ExpressionList)
                    {
                        if (!PrintList(printed.Children, printTarget)) return false;
                        break;
                    }
                    if (printTarget.IsRedirected)
                    {
                        if (!PrintList([printed], printTarget)) return false;
                        break;
                    }
                    if (!Expression(printed, out var printValue, preserveOrigin: true)) return false;
                    var printKind = _operands[printValue].Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField
                        ? AwkRegionInstructionKind.PrintField : AwkRegionInstructionKind.PrintValue;
                    Emit(new(printKind, Left: printValue));
                    break;
                case AwkStatementKind.Printf:
                    if (!AwkStatementLoweringShapes.Output(statement).TryGetValue(out var formatted)) return false;
                    if (formatted.Redirection.TryGetValue(out var printfRedirection) && printfRedirection.Kind == AwkOutputRedirectionKind.Coprocess)
                    {
                        if (!LegacyHostStatement(statement)) return false;
                        break;
                    }
                    if (!OutputTarget(formatted.Redirection, out var printfTarget) ||
                        !formatted.Output.Expression.TryGetValue(out var printfOutput) || !Printf(printfOutput, printfTarget)) return false;
                    break;
                case AwkStatementKind.Delete:
                    if (!AwkStatementLoweringShapes.SingleExpression(statement).TryGetValue(out var deleted) || !Delete(deleted)) return false;
                    break;
                case AwkStatementKind.Getline:
                    if (!AwkStatementLoweringShapes.Getline(statement).TryGetValue(out var getline) ||
                        !AwkGetlineLowerer.TryResolveStatementParts(getline, out var parts) ||
                        !Getline(parts, isExpression: false, out _)) return false;
                    break;
                default: return false;
            }
            // Values captured by a statement have no users after it completes.
            // Calls/returns and pending host users finish before reaching here;
            // nonlocal flow is unwound by its existing owner instead.
            if (_values > firstTemporary)
                Emit(new(AwkRegionInstructionKind.ReleaseValues, firstTemporary, Right: _values - firstTemporary));
            Emit(new(AwkRegionInstructionKind.StatementBoundary));
        }
        return true;
    }

    // Coprocess rejection remains a named migration boundary, not an alternate engine.
    private bool LegacyHostStatement(AwkStatementSyntax statement)
    {
        var host = AwkStatementLowerer.LowerList([statement], _scope);
        if (!host.Succeeded || host.Value.Count != 1) return false;
        var index = _hostStatements.Count;
        _hostStatements.Add(host.Value[0]);
        Emit(new(AwkRegionInstructionKind.HostStatement, Left: index));
        return true;
    }

    private bool CompileLoop(AwkStatementSyntax statement)
    {
        AwkForHeaderSyntax? header = null;
        if (statement.Kind == AwkStatementKind.For)
        {
            if (statement.Payload is not AwkForStatementSyntaxPayload payload) return false;
            if (payload.Header.IsForIn) return CompileIteration(payload.Header, statement.BodyStatements);
            if (payload.Header.Parts.Count != 3)
                return false;
            header = payload.Header;
            if (header.Parts[0].Expression.TryGetValue(out var initial))
            {
                if (!Expression(initial, out var value)) return false;
                Discard(value);
            }
        }

        var start = Label();
        var conditionJumps = new List<int>();
        if (statement.Kind != AwkStatementKind.DoWhile)
        {
            var condition = header is null ? statement.Expression : header.Parts[1].Expression;
            if (condition.TryGetValue(out var expression))
            {
                if (!Branch(expression, whenTrue: false, conditionJumps)) return false;
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
            if (!statement.Expression.TryGetValue(out var condition) || !Branch(condition, whenTrue: false, conditionJumps)) return false;
        }
        Emit(new(AwkRegionInstructionKind.Jump, start));
        var end = Label();
        PatchBranches(conditionJumps, end);
        foreach (var position in loop.Breaks) Patch(position, end);
        return true;
    }

    private bool CompileIteration(AwkForHeaderSyntax header, IReadOnlyList<AwkStatementSyntax> body)
    {
        if (header.Parts.Count != 2 || !header.Parts[0].Expression.TryGetValue(out var variableSyntax) ||
            !Variable(variableSyntax, out var variable) || !header.Parts[1].Expression.TryGetValue(out var arraySyntax))
            return false;
        int access;
        if (AwkExpressionSyntaxFacts.TryGetIdentifier(arraySyntax, out var name))
        {
            if (_scope.IsReservedVariable(name.Name)) return false;
            access = _arrays.Count;
            _arrays.Add(new(new(name.Identifier), _scope.GetArrayBinding(name.Identifier), -1));
        }
        else if (arraySyntax.Kind != AwkExpressionKind.ArrayElement || !NestedAccess(arraySyntax, out access)) return false;
        var slot = _iterators++;
        Emit(new(AwkRegionInstructionKind.IteratorBegin, slot, access));
        var start = Label();
        var next = Emit(new(AwkRegionInstructionKind.IteratorNext, Left: slot, Right: variable));
        var loop = new Loop();
        _loops.Push(loop);
        if (!Statements(body)) return false;
        _loops.Pop();
        foreach (var position in loop.Continues) Patch(position, start);
        Emit(new(AwkRegionInstructionKind.Jump, start));
        var end = Label();
        Patch(next, end);
        foreach (var position in loop.Breaks) Patch(position, end);
        Emit(new(AwkRegionInstructionKind.IteratorEnd, slot));
        return true;
    }

    private bool Expression(AwkExpressionSyntax syntax, out int result, bool preserveOrigin = false)
    {
        _scope.CheckCancellation();
        result = 0;
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        switch (syntax.Kind)
        {
            case AwkExpressionKind.Group when syntax.Children.Count == 1:
                return Expression(syntax.Children[0], out result, preserveOrigin);
            case AwkExpressionKind.Number:
                if (!AwkExpressionLowerer.TryReadSwitchConstant(syntax, _scope, out var number)) return false;
                result = Constant(number.AsNumber());
                return true;
            case AwkExpressionKind.String:
                if (!AwkExpressionLowerer.TryReadStringLiteral(syntax, _scope, out var literal)) return false;
                result = Operand(AwkRegionOperandKind.Literal, _literals.Count);
                _literals.Add(AwkText.FromSourceLiteralString(literal));
                return true;
            case AwkExpressionKind.Variable:
                if (AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var identifier) &&
                    AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length))
                    return Length(null, out result);
                if (!Variable(syntax, out var variable)) return false;
                result = Operand(AwkRegionOperandKind.Variable, variable);
                return true;
            case AwkExpressionKind.Field when syntax.Children.Count == 1:
                return Field(syntax, out result);
            case AwkExpressionKind.ArrayElement:
                if (!ArrayAccess(syntax, out var access)) return false;
                if (_arrays[access].Path >= 0 || _operands[_arrays[access].Key].Kind == AwkRegionOperandKind.KeyBuilder)
                {
                    // A reusable key buffer must be consumed before another access
                    // can reuse it. The captured scalar preserves its own spelling.
                    var captured = _values++;
                    Emit(new(AwkRegionInstructionKind.ArrayRead, captured, access, Detail: preserveOrigin ? 1 : 0));
                    FinishKey(access);
                    result = Operand(AwkRegionOperandKind.ValueTemporary, captured);
                }
                else result = Operand(AwkRegionOperandKind.ArrayElement, access);
                return true;
            case AwkExpressionKind.Assignment when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Assignment(syntax, out result);
            case AwkExpressionKind.FunctionCall:
                return Call(syntax, out result);
            case AwkExpressionKind.Concatenation when syntax.Children.Count == 2:
                return Concatenation(syntax, out result);
            case AwkExpressionKind.Binary when syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1:
                return Binary(syntax, out result);
            case AwkExpressionKind.Unary when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
            case AwkExpressionKind.Postfix when syntax.Children.Count == 1 && syntax.OperatorTokens.Count == 1:
                return Unary(syntax, out result);
            case AwkExpressionKind.Conditional when syntax.Children.Count == 3:
                var alternate = new List<int>();
                if (!Branch(syntax.Children[0], whenTrue: false, alternate)) return false;
                var target = _values++;
                if (!Expression(syntax.Children[1], out var first, preserveOrigin)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, first, Detail: preserveOrigin ? 1 : 0));
                var end = Emit(new(AwkRegionInstructionKind.Jump));
                PatchBranches(alternate, Label());
                if (!Expression(syntax.Children[2], out var second, preserveOrigin)) return false;
                Emit(new(AwkRegionInstructionKind.Value, target, second, Detail: preserveOrigin ? 1 : 0));
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
        if (_scope.TryGetFunctionLocalSlot(new(target.Identifier), out _)) return false;
        if (AwkBuiltins.IsName(target.Name, AwkBuiltinId.Getline))
        {
            return AwkGetlineLowerer.TryResolveExpressionCallParts(syntax, out var parts) &&
                Getline(parts, isExpression: true, out result);
        }
        if (AwkBuiltins.IsName(target.Name, AwkBuiltinId.Length) &&
            target.HasDelimitedCall && syntax.Children.Count is 1 or 2)
            return Length(syntax.Children.Count == 1 ? null : syntax.Children[1], out result);
        if (AwkBuiltins.TryGetCallable(target.Name, out var builtin) &&
            AwkCompatibilityNames.EnablesBuiltin(builtin, _scope.CompatibilityMode) &&
            target.HasDelimitedCall && AwkBuiltinCallShape.AcceptsArgumentCount(builtin, syntax.Children.Count - 1) &&
            builtin.TryGetScalarComputation(out var implementation))
            return builtin.Id is AwkBuiltinId.Index or AwkBuiltinId.Substr or AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper
                ? StringCall(syntax, builtin.Id, out result)
                : Builtin(syntax, builtin, implementation, out result);
        if (_scope.Functions is not { } functions ||
            !functions.TryGetValue(new AwkFunctionName(target.Identifier), out var function)) return false;
        var slot = _activeCalls++;
        _incomingCalls = Math.Max(_incomingCalls, _activeCalls);
        var call = _calls.Count;
        _calls.Add(new(function, slot, syntax.Children.Count - 1));
        Emit(new(AwkRegionInstructionKind.BeginCall, Left: call));
        for (var index = 1; index < syntax.Children.Count; index++)
        {
            if (!Expression(syntax.Children[index], out var value, preserveOrigin: true)) return false;
            Emit(new(AwkRegionInstructionKind.Argument, call, value, index - 1,
                syntax.Children[index].Kind == AwkExpressionKind.Group ? 1 : 0));
        }
        var returned = _values++;
        Emit(new(AwkRegionInstructionKind.Call, returned, call));
        _activeCalls--;
        result = Operand(AwkRegionOperandKind.ValueTemporary, returned);
        return true;
    }

    private bool Length(AwkExpressionSyntax? argument, out int result)
    {
        result = 0;
        int value;
        if (argument is null) value = Operand(AwkRegionOperandKind.Field, 0);
        else if (!Expression(argument, out value, preserveOrigin: true)) return false;
        var number = _numbers++;
        Emit(new(AwkRegionInstructionKind.Length, number, value,
            Detail: argument is not null && AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out _) ? 1 : 0));
        result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        return true;
    }

    private bool Getline(AwkGetlineLoweringParts parts, bool isExpression, out int result)
    {
        var site = _getlines.Count;
        var number = _numbers++;
        result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        var source = parts.HasRedirection ? _values++ : -1;
        _getlines.Add(new(site, source, parts.SourceKind, number, isExpression));
        var boundary = Emit(new(AwkRegionInstructionKind.GetlineBoundary, Left: site));
        if (parts.HasRedirection)
        {
            if (!parts.Source.TryGetValue(out var sourceSyntax) || !Expression(sourceSyntax, out var sourceValue)) return false;
            Emit(new(AwkRegionInstructionKind.Value, source, sourceValue));
            // Resolve the name before any destination effects, including numeric formatting.
            Emit(new(AwkRegionInstructionKind.PrepareString, source));
        }
        if (parts.Target.TryGetValue(out var target))
        {
            int operand;
            var access = -1;
            if (target.Kind == AwkExpressionKind.Variable &&
                AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(target, out _) && Variable(target, out var variable))
                operand = Operand(AwkRegionOperandKind.Variable, variable);
            else if (target.Kind == AwkExpressionKind.Field && Field(target, out var field)) operand = field;
            else if (target.Kind == AwkExpressionKind.ArrayElement &&
                ArrayAccess(target, out access))
            {
                operand = Operand(AwkRegionOperandKind.ArrayElement, access);
            }
            else return false;
            Emit(new(AwkRegionInstructionKind.GetlineTarget, site, operand));
            if (access >= 0) FinishKey(access);
        }
        Emit(new(AwkRegionInstructionKind.Getline, Left: site));
        Patch(boundary, Label());
        return true;
    }

    private bool Builtin(AwkExpressionSyntax syntax, AwkBuiltinDefinition definition,
        AwkBuiltinImplementation.SyncBuiltinImplementation implementation, out int result)
    {
        result = 0;
        var count = syntax.Children.Count - 1;
        var start = _values;
        _values = checked(_values + count);
        for (var index = 0; index < count; index++)
        {
            if (!Expression(syntax.Children[index + 1], out var operand)) return false;
            Emit(new(AwkRegionInstructionKind.BuiltinArgument, start + index, operand,
                Detail: definition.ConsumesArithmeticInputs ? 1 : 0));
        }
        // Fold the final uninterrupted captures into the invocation. Earlier
        // captures stay before any intervening computation, mutation or call.
        // This applies to every scalar builtin, independently of syntax shape.
        var firstPending = _code.Count;
        while (firstPending > 0 && _code[firstPending - 1] is { Kind: AwkRegionInstructionKind.BuiltinArgument } capture &&
            capture.Target >= start && capture.Target < start + count)
            firstPending--;
        var pendingArguments = firstPending == _code.Count ? [] : new int[_code.Count - firstPending];
        for (var index = 0; index < pendingArguments.Length; index++)
            pendingArguments[index] = _code[firstPending + index].Left;
        _code.RemoveRange(firstPending, pendingArguments.Length);
        var site = _builtins.Count;
        _builtins.Add(new(implementation, start, count, pendingArguments));
        var returned = _values++;
        Emit(new(AwkRegionInstructionKind.Builtin, returned, site));
        result = Operand(AwkRegionOperandKind.ValueTemporary, returned);
        return true;
    }

    private bool StringCall(AwkExpressionSyntax syntax, AwkBuiltinId id, out int result)
    {
        result = 0;
        var count = syntax.Children.Count - 1;
        Span<int> arguments = stackalloc int[3];
        arguments.Fill(-1);
        for (var index = 0; index < count; index++)
        {
            // Preserve evaluation before any later computation or possibly
            // invalidating lookup. Immediate scalar/field reads keep their
            // existing owners through this synchronous operation.
            if (!IsImmediateRead(syntax.Children[index + 1])) CaptureStringArguments(arguments, index);
            if (!Expression(syntax.Children[index + 1], out var operand)) return false;
            arguments[index] = operand;
            if (_operands[operand].Kind == AwkRegionOperandKind.ArrayElement)
                CaptureStringArguments(arguments, index + 1);
        }
        var site = _strings.Count;
        _strings.Add(new(id, arguments[0], arguments[1], arguments[2]));
        var target = id == AwkBuiltinId.Index ? _numbers++ : _values++;
        Emit(new(AwkRegionInstructionKind.String, target, site));
        result = Operand(id == AwkBuiltinId.Index ? AwkRegionOperandKind.NumberTemporary : AwkRegionOperandKind.ValueTemporary, target);
        return true;
    }

    private void CaptureStringArguments(Span<int> arguments, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (_operands[arguments[index]].Kind is AwkRegionOperandKind.NumberTemporary or AwkRegionOperandKind.ValueTemporary) continue;
            var captured = _values++;
            Emit(new(AwkRegionInstructionKind.Value, captured, arguments[index], Detail: 1));
            arguments[index] = Operand(AwkRegionOperandKind.ValueTemporary, captured);
        }
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
            else if (Field(syntax.Children[0], out var field))
                Emit(new(AwkRegionInstructionKind.AssignField, field, result));
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
        if (syntax.Children.Count < 2) return false;
        if (syntax.Children[0].Kind == AwkExpressionKind.ArrayElement) return NestedAccess(syntax, out index);
        if (!AwkExpressionSyntaxFacts.TryGetArrayElementTarget(syntax, out var array) ||
            _scope.IsReservedVariable(array.Name)) return false;
        if (!ArrayKey(syntax.Children, 1, out var key)) return false;
        // Nested subscript reads happen now, not recursively inside a later operand.
        if (_operands[key].Kind == AwkRegionOperandKind.ArrayElement) key = CaptureValue(key);
        index = _arrays.Count;
        _arrays.Add(new(new(array.Identifier), _scope.GetArrayBinding(array.Identifier), key));
        return true;
    }

    private bool NestedAccess(AwkExpressionSyntax syntax, out int index, bool deletion = false)
    {
        index = 0;
        var levels = new List<AwkExpressionSyntax>();
        var root = syntax;
        while (root.Kind == AwkExpressionKind.ArrayElement && root.Children.Count > 1)
        {
            _scope.CheckCancellation();
            levels.Add(root);
            root = root.Children[0];
        }
        if (!AwkExpressionSyntaxFacts.TryGetIdentifier(root, out var name) || _scope.IsReservedVariable(name.Name)) return false;
        var path = _paths.Count;
        var binding = _scope.GetArrayBinding(name.Identifier);
        _paths.Add(new(new(name.Identifier), binding, levels.Count));
        Emit(new(AwkRegionInstructionKind.PathBegin, path));
        for (var depth = 0; depth < levels.Count; depth++)
        {
            if (!ArrayKey(levels[^(depth + 1)].Children, 1, out var key)) return false;
            Emit(new(AwkRegionInstructionKind.PathKey, path, key, depth, deletion ? 1 : 0));
            if (_operands[key] is { Kind: AwkRegionOperandKind.KeyBuilder } builder)
            {
                Emit(new(AwkRegionInstructionKind.KeyEnd, builder.Index));
                _activeBuilders--;
            }
        }
        index = _arrays.Count;
        _arrays.Add(new(new(name.Identifier), binding, -1, path));
        return true;
    }

    private bool ArrayKey(IReadOnlyList<AwkExpressionSyntax> parts, int first, out int key)
    {
        key = 0;
        if (parts.Count <= first) return false;
        if (parts.Count - first > 1)
        {
            var builder = _activeBuilders++;
            if (builder == _builderSeparators.Count) _builderSeparators.Add(_values++);
            var separator = _builderSeparators[builder];
            Emit(new(AwkRegionInstructionKind.KeyBegin, builder, separator));
            for (var component = first; component < parts.Count; component++)
            {
                if (component > first) Emit(new(AwkRegionInstructionKind.KeySeparator, builder, separator));
                if (!Expression(parts[component], out var value)) return false;
                Emit(new(AwkRegionInstructionKind.KeyAppend, builder, value));
            }
            // Separator bytes are already copied. Its old owner need not survive
            // lookup/insertion after a component has changed SUBSEP.
            Emit(new(AwkRegionInstructionKind.ReleaseValue, separator));
            key = Operand(AwkRegionOperandKind.KeyBuilder, builder);
        }
        else if (!Expression(parts[first], out key)) return false;
        return true;
    }

    private bool Membership(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var named = AwkExpressionSyntaxFacts.TryGetIdentifier(syntax.Children[1], out var array);
        if (named ? _scope.IsReservedVariable(array.Name) : syntax.Children[1].Kind != AwkExpressionKind.ArrayElement) return false;
        var index = syntax.Children[0];
        if (index.Kind == AwkExpressionKind.Group && index.FirstChild is { Kind: AwkExpressionKind.ExpressionList } tuple)
            index = tuple;
        int key;
        if (index.Kind == AwkExpressionKind.ExpressionList)
        {
            if (!ArrayKey(index.Children, 0, out key)) return false;
        }
        else if (!Expression(index, out key)) return false;
        if (!named)
        {
            // Finish key conversion before the RHS can mutate fields, CONVFMT or
            // the key's owner. Keep bytes in existing funded scratch through calls.
            if (_operands[key].Kind != AwkRegionOperandKind.KeyBuilder)
            {
                var builder = _activeBuilders++;
                if (builder == _builderSeparators.Count) _builderSeparators.Add(_values++);
                Emit(new(AwkRegionInstructionKind.KeyEnd, builder));
                Emit(new(AwkRegionInstructionKind.KeyAppend, builder, key));
                key = Operand(AwkRegionOperandKind.KeyBuilder, builder);
            }
            if (!NestedAccess(syntax.Children[1], out var nested)) return false;
            var found = _numbers++;
            Emit(new(AwkRegionInstructionKind.NestedContains, found, _arrays[nested].Path, _operands[key].Index));
            FinishKey(nested);
            Emit(new(AwkRegionInstructionKind.KeyEnd, _operands[key].Index));
            _activeBuilders--;
            result = Operand(AwkRegionOperandKind.NumberTemporary, found);
            return true;
        }
        var access = _arrays.Count;
        _arrays.Add(new(new(array.Identifier), _scope.GetArrayBinding(array.Identifier), key));
        var number = _numbers++;
        Emit(new(AwkRegionInstructionKind.ArrayContains, number, access));
        FinishKey(access);
        result = Operand(AwkRegionOperandKind.NumberTemporary, number);
        return true;
    }

    private bool Delete(AwkExpressionSyntax syntax)
    {
        if (AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var name))
        {
            if (_scope.IsReservedVariable(name.Name)) return false;
            var site = _arrays.Count;
            _arrays.Add(new(new(name.Identifier), _scope.GetArrayBinding(name.Identifier), -1));
            Emit(new(AwkRegionInstructionKind.ArrayClear, site));
            return true;
        }
        if (syntax.Kind != AwkExpressionKind.ArrayElement || syntax.Children.Count < 2) return false;
        if (syntax.Children[0].Kind == AwkExpressionKind.ArrayElement)
        {
            if (!NestedAccess(syntax, out var access, deletion: true)) return false;
            Emit(new(AwkRegionInstructionKind.PathDelete, _arrays[access].Path));
            FinishKey(access);
            return true;
        }
        if (!ArrayAccess(syntax, out var flat)) return false;
        Emit(new(AwkRegionInstructionKind.ArrayDelete, flat));
        FinishKey(flat);
        return true;
    }

    private void FinishKey(int access)
    {
        if (_arrays[access].Path >= 0)
        {
            Emit(new(AwkRegionInstructionKind.PathEnd, _arrays[access].Path));
            return;
        }
        if (_operands[_arrays[access].Key] is not { Kind: AwkRegionOperandKind.KeyBuilder } key) return;
        Emit(new(AwkRegionInstructionKind.KeyEnd, key.Index));
        _activeBuilders--;
    }

    private bool PrepareTarget(AwkExpressionSyntax syntax, out PreparedTarget target, out int prior)
    {
        if (Variable(syntax, out var variable))
        {
            target = new(Operand(AwkRegionOperandKind.Variable, variable), -1);
            prior = CaptureNumber(Operand(AwkRegionOperandKind.Variable, variable));
            return true;
        }
        if (Field(syntax, out var field))
        {
            target = new(field, -1);
            prior = CaptureNumber(field);
            return true;
        }
        target = default;
        prior = 0;
        if (!ArrayAccess(syntax, out var access)) return false;
        var number = _numbers++;
        if (_arrays[access].Path >= 0)
        {
            Emit(new(AwkRegionInstructionKind.PathNumber, number, _arrays[access].Path));
            prior = Operand(AwkRegionOperandKind.NumberTemporary, number);
            target = new(Operand(AwkRegionOperandKind.ArrayElement, access), -1);
            return true;
        }
        var captured = _targets++;
        Emit(new(AwkRegionInstructionKind.ArrayPrepare, captured, access, number));
        FinishKey(access);
        prior = Operand(AwkRegionOperandKind.NumberTemporary, number);
        target = new(Operand(AwkRegionOperandKind.ArrayElement, access), captured);
        return true;
    }

    private void Store(PreparedTarget target, int value)
    {
        var operand = _operands[target.Operand];
        if (target.Capture >= 0)
            Emit(new(AwkRegionInstructionKind.ArrayCommit, target.Capture, NumericRegister(value), operand.Index));
        else if (operand.Kind == AwkRegionOperandKind.ArrayElement)
        {
            Emit(new(AwkRegionInstructionKind.ArrayAssign, operand.Index, value));
            FinishKey(operand.Index);
        }
        else if (operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField)
            Emit(new(AwkRegionInstructionKind.AssignField, target.Operand, value));
        else Assign(operand.Index, value);
    }

    private bool Field(AwkExpressionSyntax syntax, out int operand)
    {
        operand = 0;
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        if (syntax.Kind != AwkExpressionKind.Field || syntax.Children.Count != 1) return false;
        if (TryFixedField(syntax, out var field))
        {
            operand = Operand(AwkRegionOperandKind.Field, field);
            return true;
        }
        if (!Expression(syntax.Children[0], out var value)) return false;
        var validated = _numbers++;
        Emit(new(AwkRegionInstructionKind.FieldIndex, validated, NumericRegister(value)));
        operand = Operand(AwkRegionOperandKind.DynamicField, validated);
        return true;
    }

    private bool CopyField(AwkExpressionSyntax syntax)
    {
        // A discarded move to a fixed location can keep record-backed text. A
        // computed destination must first own the RHS before evaluating its index.
        if (syntax.Kind != AwkExpressionKind.Assignment || syntax.Children.Count != 2 ||
            syntax.OperatorTokens.Count != 1 || syntax.OperatorTokens[0].Kind != AwkTokenKind.Equal ||
            !TryFixedField(syntax.Children[0], out var destination) || destination == 0 ||
            !Field(syntax.Children[1], out var source)) return false;
        Emit(new(AwkRegionInstructionKind.CopyField, destination, source));
        return true;
    }

    private bool TryFixedField(AwkExpressionSyntax syntax, out int field)
    {
        field = 0;
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        if (syntax.Kind != AwkExpressionKind.Field || syntax.Children.Count != 1) return false;
        var index = syntax.Children[0];
        while (index.Kind == AwkExpressionKind.Group && index.Children.Count == 1)
        {
            _scope.CheckCancellation();
            index = index.Children[0];
        }
        if (index.Kind != AwkExpressionKind.Number || !AwkExpressionLowerer.TryReadSwitchConstant(index, _scope, out var constant)) return false;
        var number = constant.AsNumber();
        if (!double.IsFinite(number) || number < 0 || number > int.MaxValue) return false;
        field = (int)number;
        return true;
    }

    private bool OutputTarget(AwkSyntaxOptional<AwkOutputRedirectionSyntax> syntax, out AwkRegionOutput output)
    {
        output = default;
        if (!syntax.TryGetValue(out var redirection)) return true;
        if (!redirection.Target.Expression.TryGetValue(out var target) || !Expression(target, out var value)) return false;
        var captured = _values++;
        Emit(new(AwkRegionInstructionKind.Value, captured, value));
        output = new(captured, redirection.Kind);
        _usesOutputBuilder = true;
        return true;
    }

    private AwkRegionPrint AllocatePrint(int count, int[] fields, AwkRegionOutput output)
    {
        var print = new AwkRegionPrint(_values, _texts, _printSegments, count, fields, output);
        _values = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_values, count));
        _texts = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_texts, (long)count + 2));
        _printSegments = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_printSegments, count));
        return print;
    }

    private void EmitPrint(AwkRegionPrint print)
    {
        var site = _prints.Count;
        _prints.Add(print);
        Emit(new(AwkRegionInstructionKind.PrintList, Left: site));
    }

    private bool PrintList(IReadOnlyList<AwkExpressionSyntax> arguments, AwkRegionOutput output)
    {
        if (arguments.Count == 0) return false;
        var fields = new int[arguments.Count];
        for (var index = 0; index < fields.Length; index++)
        {
            // $0 can reconstruct and retire record slices. Such a list owns its
            // earlier values rather than carrying views across reconstruction.
            if (TryFixedField(arguments[index], out fields[index]) && fields[index] != 0) continue;
            fields = [];
            break;
        }
        var print = AllocatePrint(arguments.Count, fields, output);
        if (fields.Length == 0)
        {
            for (var index = 0; index < arguments.Count; index++)
            {
                if (!Expression(arguments[index], out var value, preserveOrigin: true)) return false;
                Emit(new(AwkRegionInstructionKind.Value, print.Values + index, value, Detail: 1));
            }
        }
        EmitPrint(print);
        return true;
    }

    private bool Printf(AwkExpressionSyntax syntax, AwkRegionOutput output)
    {
        if (syntax.Kind == AwkExpressionKind.Group && syntax.FirstChild is { Kind: AwkExpressionKind.ExpressionList } list)
            syntax = list;
        IReadOnlyList<AwkExpressionSyntax> arguments = syntax.Kind == AwkExpressionKind.ExpressionList ? syntax.Children : [syntax];
        if (arguments.Count == 0 || !Expression(arguments[0], out var format, preserveOrigin: true)) return false;
        var formatSlot = _values++;
        // Capture the value before arguments; numeric formatting uses CONVFMT
        // after their effects. The captured text survives record/variable writes.
        Emit(new(AwkRegionInstructionKind.Value, formatSlot, format, Detail: 1));
        var count = arguments.Count - 1;
        var fields = new int[count];
        for (var index = 0; index < count; index++)
        {
            if (TryFixedField(arguments[index + 1], out fields[index]) && fields[index] != 0) continue;
            fields = [];
            break;
        }
        var values = _values;
        if (fields.Length == 0)
        {
            _values = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_values, count));
            for (var index = 0; index < count; index++)
            {
                if (!Expression(arguments[index + 1], out var value, preserveOrigin: true)) return false;
                Emit(new(AwkRegionInstructionKind.Value, values + index, value, Detail: 1));
            }
        }
        var site = _printfs.Count;
        _printfs.Add(new(formatSlot, values, count, fields, output));
        Emit(new(AwkRegionInstructionKind.Printf, Left: site));
        return true;
    }

    private bool Concatenation(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        var parts = new List<AwkExpressionSyntax>();
        var pending = new Stack<AwkExpressionSyntax>();
        pending.Push(syntax);
        while (pending.TryPop(out var part))
        {
            _scope.CheckCancellation();
            if (part.Kind == AwkExpressionKind.Concatenation && part.Children.Count == 2)
            {
                pending.Push(part.Children[1]);
                pending.Push(part.Children[0]);
            }
            else parts.Add(part);
        }
        var start = _texts;
        _texts += parts.Count;
        for (var index = 0; index < parts.Count; index++)
        {
            if (!Expression(parts[index], out var value)) return false;
            // Formatting belongs to this operand, before later work can mutate
            // CONVFMT or its source. Nested concatenations use separate slots.
            Emit(new(AwkRegionInstructionKind.ConcatArgument, start + index, value));
        }
        var target = _values++;
        Emit(new(AwkRegionInstructionKind.ConcatValue, target, start, parts.Count));
        result = Operand(AwkRegionOperandKind.ValueTemporary, target);
        return true;
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
        if (token == AwkTokenKind.In) return Membership(syntax, out result);
        if (!Expression(syntax.Children[0], out var left)) return false;
        if (token is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe)
        {
            var number = _numbers++;
            var alternative = EmitBranch(left, whenTrue: false);
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
        var stringComparison = _operands[left].Kind == AwkRegionOperandKind.Literal;
        left = (int)arithmetic >= 0 ? CaptureNumber(left) : IsImmediateRead(syntax.Children[1]) ? left : CaptureValue(left);
        if (!Expression(syntax.Children[1], out var operand)) return false;
        var target = _numbers++;
        if ((int)arithmetic >= 0)
            Emit(new(AwkRegionInstructionKind.Arithmetic, target, NumericRegister(left), NumericRegister(operand), (int)arithmetic));
        else
            Emit(new(stringComparison || _operands[operand].Kind == AwkRegionOperandKind.Literal
                ? AwkRegionInstructionKind.CompareText : AwkRegionInstructionKind.Compare,
                target, left, operand, (int)comparison));
        result = Operand(AwkRegionOperandKind.NumberTemporary, target);
        return true;
    }

    private bool Branch(AwkExpressionSyntax syntax, bool whenTrue, List<int> destinations)
    {
        _scope.CheckCancellation();
        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack()) return false;
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        if (syntax.Kind == AwkExpressionKind.Unary && syntax.Children.Count == 1 &&
            syntax.OperatorTokens.Count == 1 && syntax.OperatorTokens[0].Kind == AwkTokenKind.Bang)
            return Branch(syntax.Children[0], !whenTrue, destinations);
        if (syntax.Kind == AwkExpressionKind.Binary && syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 &&
            syntax.OperatorTokens[0].Kind is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe)
        {
            var conjunction = syntax.OperatorTokens[0].Kind == AwkTokenKind.AmpersandAmpersand;
            if (conjunction != whenTrue)
                return Branch(syntax.Children[0], whenTrue, destinations) && Branch(syntax.Children[1], whenTrue, destinations);
            var bypass = new List<int>();
            if (!Branch(syntax.Children[0], !whenTrue, bypass) || !Branch(syntax.Children[1], whenTrue, destinations)) return false;
            PatchBranches(bypass, Label());
            return true;
        }
        if (!Expression(syntax, out var value)) return false;
        destinations.Add(EmitBranch(value, whenTrue));
        return true;
    }

    private int EmitBranch(int operand, bool whenTrue)
    {
        var source = _operands[operand];
        return source.Kind == AwkRegionOperandKind.NumberTemporary
            ? Emit(new(whenTrue ? AwkRegionInstructionKind.JumpIfNonzero : AwkRegionInstructionKind.JumpIfZero, Left: source.Index))
            : Emit(new(whenTrue ? AwkRegionInstructionKind.JumpIfTrue : AwkRegionInstructionKind.JumpIfFalse, Left: operand));
    }

    private void PatchBranches(List<int> positions, int target)
    {
        foreach (var position in positions) Patch(position, target);
    }

    private bool IsImmediateRead(AwkExpressionSyntax syntax)
    {
        while (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1)
        {
            _scope.CheckCancellation();
            syntax = syntax.Children[0];
        }
        return syntax.Kind is AwkExpressionKind.Number or AwkExpressionKind.String ||
            syntax.Kind == AwkExpressionKind.Variable &&
                AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var identifier) && !AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length) ||
            syntax.Kind == AwkExpressionKind.Field && TryFixedField(syntax, out _);
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
        if (source.Kind == AwkRegionOperandKind.ValueTemporary && _code.Count > 0 &&
            _code[^1] is { Kind: AwkRegionInstructionKind.Call, Detail: 0 } call && call.Target == source.Index)
        {
            // Conversion is the immediate consumer: no script work moves across
            // it. Keep the value with its funded callee until conversion finishes.
            _code[^1] = call with { Target = target, Detail = 1 };
            if (source.Index == _values - 1) _values--;
            _operands[operand] = new(AwkRegionOperandKind.NumberTemporary, target);
            return operand;
        }
        var kind = source.Kind switch
        {
            AwkRegionOperandKind.Field => AwkRegionInstructionKind.FieldNumber,
            AwkRegionOperandKind.DynamicField => AwkRegionInstructionKind.DynamicFieldNumber,
            AwkRegionOperandKind.Literal => AwkRegionInstructionKind.LiteralNumber,
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
        if (_operands[operand].Kind is AwkRegionOperandKind.Variable or AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField or AwkRegionOperandKind.ArrayElement)
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

    private readonly record struct PreparedTarget(int Operand, int Capture);
}
