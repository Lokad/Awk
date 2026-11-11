using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Prepare the interpreter's operands, effects and control flow without arity,
// block-size or script-shape eligibility guards.
internal sealed partial class AwkRegionCompiler
{
    private readonly AwkLoweringScope _scope;
    private readonly AwkRegionKind _kind;
    private readonly List<AwkRegionInstruction> _code = [];
    private readonly List<AwkRegionOperand> _operands = [];
    private readonly List<AwkRegionConstant> _constants = [];
    private readonly List<AwkRegionLiteral> _literals = [];
    private readonly List<AwkRegionArrayAccess> _arrays = [];
    private readonly List<AwkRegionArrayUpdate> _updates = [];
    private readonly List<AwkRegionScalarUpdate> _scalarUpdates = [];
    private int _activeUpdates;
    private int _updateSlots;
    private readonly List<AwkRegionPath> _paths = [];
    private int _capturedPaths;
    private int _builderCount;
    private readonly List<AwkRegionCall> _calls = [];
    private readonly List<AwkRegionBuiltin> _builtins = [];
    private readonly List<AwkRegionStringCall> _strings = [];
    private readonly List<AwkRegionPrint> _prints = [];
    private readonly List<AwkRegionPrintf> _printfs = [];
    private readonly List<AwkRegionGetline> _getlines = [];
    private readonly List<AwkSwitchCasePattern> _switchCases = [];
    private readonly List<AwkRegionRegexMatch> _regexMatches = [];
    private readonly List<AwkRegionSplit> _splits = [];
    private readonly List<AwkRegionMatch> _matches = [];
    private readonly List<AwkRegionSubstitution> _substitutions = [];
    private readonly List<AwkRegionGensub> _gensubs = [];
    private readonly List<AwkRegionSort> _sorts = [];
    private readonly List<AwkRegionIteration> _iterations = [];
    private readonly List<AwkRegionRuleScope> _ruleScopes = [];
    private int _sortSlots;
    private int _activeCalls;
    private int _incomingCalls;
    private int _activeBuilders;
    private readonly Dictionary<long, int> _constantRegisters = [];
    private readonly Dictionary<AwkVariableName, int> _variableIndices = [];
    private readonly Stack<ControlScope> _controls = [];
    private int _numbers;
    private int _lastNumberRead = -1;
    private int _lastNumberOperand;
    private int _values;
    private int _texts;
    private int _printSegments;
    private int _iterators;
    private int _arrayTargets;
    private int _straightLine;
    private bool _usesOutputBuilder;
    private AwkDiagnostic? _diagnostic;
    private int _mainBodyStart = -1;
    internal bool HasMainRules { get; private set; }

    private AwkRegionCompiler(AwkLoweringScope scope, AwkRegionKind kind) => (_scope, _kind) = (scope, kind);

    internal static AwkRegionCompiler CreateMain(AwkLoweringScope scope)
    {
        var compiler = new AwkRegionCompiler(scope, AwkRegionKind.Record);
        // Reading already polls at the input boundary; body labels poll computation.
        compiler.Emit(new(AwkRegionInstructionKind.MainRead));
        compiler._mainBodyStart = compiler._code.Count;
        return compiler;
    }

    internal AwkRegion BuildMain()
    {
        Emit(new(AwkRegionInstructionKind.Jump, 0));
        return CreateRegion();
    }

    internal bool AppendMainRule(AwkRuleSyntax rule, int rangeIndex, out AwkDiagnostic diagnostic)
    {
        var before = StorageLayout() with { Targets = 0 };
        var firstUpdate = _updates.Count;
        var firstInstruction = _code.Count;
        var success = AppendMainRuleBody(rule, rangeIndex);
        if (success)
        {
            PrepareArrayOwners(firstInstruction);
            var after = StorageLayout() with { Targets = 0 };
            for (var index = firstUpdate; index < _updates.Count; index++)
            {
                if ((index & 127) == 127) _scope.CheckCancellation();
                if (_updates[index].PrepareAccess < 0)
                    after = after with { Targets = Math.Max(after.Targets, _updates[index].Slot + 1) };
            }
            // Number registers and immutable program operands retain no execution-owned payload.
            // Emit cleanup only for scratch that can actually own a live user.
            if (after.Values != before.Values || after.Texts != before.Texts ||
                after.Targets != before.Targets || after.Paths != before.Paths ||
                after.GetlineTargets != before.GetlineTargets || after.Iterators != before.Iterators ||
                after.ArrayTargets != before.ArrayTargets || after.Sorts != before.Sorts ||
                after.PrintSegments != before.PrintSegments || after.Builders > 0 || after.Incoming > 0)
            {
                var scope = _ruleScopes.Count;
                _ruleScopes.Add(new(before, after));
                Emit(new(AwkRegionInstructionKind.RuleEnd, Left: scope));
            }
            HasMainRules = true;
        }
        diagnostic = _diagnostic ?? (success ? default : InvalidRule(rule));
        return success;
    }

    internal static bool TryCompile(IReadOnlyList<AwkStatementSyntax> statements,
        AwkLoweringScope scope, [NotNullWhen(true)] out AwkRegion? region) =>
        TryCompile(statements, scope, out region, out _);

    internal static bool TryCompile(IReadOnlyList<AwkStatementSyntax> statements,
        AwkLoweringScope scope, [NotNullWhen(true)] out AwkRegion? region, out AwkDiagnostic diagnostic)
    {
        var compiler = new AwkRegionCompiler(scope, scope.IsFunction ? AwkRegionKind.Function : AwkRegionKind.Record);
        var success = compiler.Compile(statements, null, defaultPrint: false, out region);
        diagnostic = compiler._diagnostic ?? default;
        return success;
    }

    internal static bool TryCompilePhase(AwkRuleSyntax rule, AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkRegion? region, out AwkDiagnostic diagnostic)
    {
        var compiler = new AwkRegionCompiler(scope, AwkRegionKind.Phase);
        var success = compiler.CompileBody(rule.ActionStatements, null, defaultPrint: false);
        region = success ? compiler.CreateRegion() : null;
        diagnostic = compiler._diagnostic ?? (success ? default : InvalidRule(rule));
        return success;
    }

    private bool AppendMainRuleBody(AwkRuleSyntax rule, int rangeIndex)
    {
        AwkExpressionSyntax? condition = null;
        AwkExpressionSyntax? rangeEnd = null;
        if (rule.PatternTokens.Count != 0)
        {
            if (rule.PatternSyntax is AwkSinglePatternSyntax { Expression: { } expression })
                condition = expression;
            else if (rangeIndex >= 0 && rule.PatternSyntax is AwkRangePatternSyntax
                { Start.Expression: { } start, End.Expression: { } end })
            {
                condition = start;
                rangeEnd = end;
            }
            else
            {
                _diagnostic = InvalidRule(rule);
                return false;
            }
        }
        var success = CompileBody(rule.ActionStatements, condition, defaultPrint: !rule.HasExplicitAction, rangeEnd, rangeIndex);
        if (!success && _diagnostic is null && rule.HasExplicitAction)
        {
            // Body diagnostics have always preceded pattern-lowering failures.
            // Preparing a body cannot execute effects or publish a partial program.
            var body = new AwkRegionCompiler(_scope, AwkRegionKind.Record);
            if (!body.Compile(rule.ActionStatements, null, defaultPrint: false, out _)) _diagnostic = body._diagnostic;
        }
        return success;
    }

    private static AwkDiagnostic InvalidRule(AwkRuleSyntax rule) => new(rule.Span,
        rule.HasExplicitAction ? "Invalid action block." : "Invalid pattern.", AwkDiagnosticId.BindInvalidProgram);

    private bool Compile(IReadOnlyList<AwkStatementSyntax> statements, AwkExpressionSyntax? condition,
        bool defaultPrint, [NotNullWhen(true)] out AwkRegion? region,
        AwkExpressionSyntax? rangeEnd = null, int rangeIndex = -1)
    {
        var success = CompileBody(statements, condition, defaultPrint, rangeEnd, rangeIndex);
        region = success ? CreateRegion() : null;
        return success;
    }

    private bool CompileBody(IReadOnlyList<AwkStatementSyntax> statements, AwkExpressionSyntax? condition,
        bool defaultPrint, AwkExpressionSyntax? rangeEnd = null, int rangeIndex = -1)
    {
        // Called functions cross the activation checkpoint before entering code.
        // Their initial field demand belongs to entry, without another dispatch.
        if (_kind != AwkRegionKind.Function) Label();
        List<int>? skipped = null;
        if (condition is not null)
        {
            skipped = [];
            var active = rangeEnd is null ? -1 : Emit(new(AwkRegionInstructionKind.JumpIfRangeActive, Left: rangeIndex));
            if (!Branch(condition, whenTrue: false, skipped))
            {
                return false;
            }
            if (rangeEnd is not null)
            {
                Emit(new(AwkRegionInstructionKind.SetRangeState, rangeIndex, Detail: 1));
                Patch(active, Label());
                var continuing = new List<int>();
                if (!Branch(rangeEnd, whenTrue: false, continuing)) return false;
                Emit(new(AwkRegionInstructionKind.SetRangeState, rangeIndex));
                PatchBranches(continuing, Label());
            }
        }
        if (!Statements(statements))
        {
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
        return true;
    }

    private AwkRegionStorageLayout StorageLayout() =>
        new(_numbers, _values, _texts, _updateSlots, Math.Max(_builderCount, _usesOutputBuilder ? 1 : 0), _incomingCalls, _printSegments, _getlines.Count, _capturedPaths, _iterators, _arrayTargets, _sortSlots);

    private AwkRegion CreateRegion()
    {
        if (_mainBodyStart < 0) PrepareArrayOwners(0);
        var entryFieldDemand = PrepareFieldDemands();
        var region = new AwkRegion(_code.ToArray(), _operands.ToArray(),
            _constants.ToArray(), _literals.ToArray(), _arrays.ToArray(), _updates.ToArray(), _scalarUpdates.ToArray(),
            StorageLayout(),
            _calls.ToArray(),
            _builtins.ToArray(), _prints.ToArray(), _printfs.ToArray(), _getlines.ToArray(), _strings.ToArray(), _paths.ToArray(), _switchCases.ToArray(), _regexMatches.ToArray(), _splits.ToArray(), _matches.ToArray(), _substitutions.ToArray(), _gensubs.ToArray(), _sorts.ToArray(), _iterations.ToArray(), _kind, _mainBodyStart, _ruleScopes.ToArray(), entryFieldDemand);
        if (_kind != AwkRegionKind.Function) AwkFunctionHostEffects.PrepareCalls(region, _scope.CheckCancellation);
        return region;
    }

    private bool Statements(IReadOnlyList<AwkStatementSyntax> statements) =>
        PrepareWork(new StatementsWork(statements));

    // Control bodies use preparation continuations. These leaf statements may
    // finish an operand work stack without recursively entering statement bodies.
    private bool PrepareStatement(AwkStatementSyntax statement)
    {
        _scope.CheckCancellation();
        if (AwkStatementKindFacts.IsOptionalValue(statement.Kind) &&
                (statement.Expressions.Count > 1 || statement.Expressions.Count == 0 &&
                    AwkTokenNavigation.FindNextSignificant(statement.Tokens, 1) is not null) ||
            AwkStatementValidationFacts.IsArgumentless(statement.Kind) && statement.Expressions.Count != 0 ||
            statement.Kind == AwkStatementKind.Expression && statement.Expressions.Count != 1 &&
                statement.Payload is not AwkInputPipeExpressionStatementSyntaxPayload ||
            statement.Kind is AwkStatementKind.If or AwkStatementKind.While or AwkStatementKind.DoWhile && statement.Expressions.Count != 1)
            return false;
        switch (statement.Kind)
        {
            case AwkStatementKind.Expression:
                if (statement.Payload is AwkInputPipeExpressionStatementSyntaxPayload pipe)
                {
                    if (pipe.InputPipeGetline.IsCoprocess)
                        Emit(new(AwkRegionInstructionKind.UnsupportedCoprocess));
                    else if (!InputPipe(pipe.InputPipeGetline)) return false;
                    break;
                }
                if (!statement.Expression.TryGetValue(out var expression)) return false;
                if (!CopyField(expression))
                {
                    if (!Expression(expression, out var value)) return false;
                    Discard(value);
                }
                break;
            case AwkStatementKind.Break:
            case AwkStatementKind.Continue:
                if (!TryGetControl(statement.Kind == AwkStatementKind.Continue, out var control)) return false;
                if (_values > control.FirstTemporary)
                    Emit(new(AwkRegionInstructionKind.ReleaseValues, control.FirstTemporary, Right: _values - control.FirstTemporary));
                Emit(new(AwkRegionInstructionKind.StatementBoundary));
                var jump = Emit(new(AwkRegionInstructionKind.Jump));
                (statement.Kind == AwkStatementKind.Break ? control.Breaks : control.Continues).Add(jump);
                break;
            case AwkStatementKind.Exit:
                var status = -1;
                if (statement.Expression.TryGetValue(out var statusExpression))
                {
                    if (!Expression(statusExpression, out var exitValue)) return false;
                    status = NumericRegister(exitValue);
                }
                Emit(new(AwkRegionInstructionKind.Exit, Left: status));
                Emit(new(AwkRegionInstructionKind.StatementBoundary));
                Emit(new(AwkRegionInstructionKind.Flow, Detail: (int)AwkStatementFlow.ExitProgram));
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
                if (returned >= 0 && _operands[returned] is { Kind: AwkRegionOperandKind.NumberTemporary } numericReturn)
                {
                    if (_lastNumberRead == _code.Count - 1 && _code.Count > 0 &&
                        _code[^1] is var read && read.IsNumberRead && read.Target == numericReturn.Index)
                        _code[^1] = read with { Kind = AwkRegionInstructionKind.ReturnNumber,
                            Target = 0, Left = _lastNumberOperand };
                    else Emit(new(AwkRegionInstructionKind.ReturnNumber, Left: ~numericReturn.Index));
                }
                else Emit(new(AwkRegionInstructionKind.Return, Left: returned));
                break;
            case AwkStatementKind.Print:
                if (!AwkStatementLoweringShapes.Output(statement).TryGetValue(out var output)) return false;
                if (output.Redirection.TryGetValue(out var printRedirection) && printRedirection.Kind == AwkOutputRedirectionKind.Coprocess)
                {
                    Emit(new(AwkRegionInstructionKind.UnsupportedCoprocess));
                    break;
                }
                if (!OutputTarget(output.Redirection, out var printTarget)) return false;
                if (output.Output.IsEmpty)
                {
                    if (printTarget.IsRedirected)
                    {
                        var capturedRecord = _values++;
                        Emit(new(AwkRegionInstructionKind.Value, capturedRecord, Operand(AwkRegionOperandKind.Field, 0), Detail: 1));
                        var recordPrint = AllocatePrint([new(AwkRegionPrintArgumentKind.Value, capturedRecord)], printTarget);
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
                if (_operands[printValue].Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField)
                    Emit(new(AwkRegionInstructionKind.PrintField, Left: printValue));
                else EmitPrint(AllocatePrint([PrintArgument(printValue, final: true)], printTarget));
                break;
            case AwkStatementKind.Printf:
                if (!AwkStatementLoweringShapes.Output(statement).TryGetValue(out var formatted)) return false;
                if (formatted.Redirection.TryGetValue(out var printfRedirection) && printfRedirection.Kind == AwkOutputRedirectionKind.Coprocess)
                {
                    Emit(new(AwkRegionInstructionKind.UnsupportedCoprocess));
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
                    !AwkGetlineSyntaxFacts.TryResolveStatementParts(getline, out var parts) ||
                    !Getline(parts, isExpression: false, out _)) return false;
                break;
            case AwkStatementKind.Close:
            case AwkStatementKind.Fflush:
                var builtinId = statement.Kind == AwkStatementKind.Close ? AwkBuiltinId.Close : AwkBuiltinId.Fflush;
                if (!AwkStatementLoweringShapes.TryGetWholeStatementCall(statement, builtinId, out var hostCall) ||
                    !hostCall.Definition.TryGetHostComputation(out var hostImplementation) ||
                    !HostBuiltin(hostCall.Expression, hostCall.Definition, hostImplementation, out var hostResult,
                        flushBeforeArguments: builtinId == AwkBuiltinId.Close && hostCall.ArgumentCount == 1)) return false;
                Discard(hostResult);
                break;
            case AwkStatementKind.Split:
            case AwkStatementKind.Patsplit:
            case AwkStatementKind.Asort:
            case AwkStatementKind.Asorti:
            case AwkStatementKind.Sub:
            case AwkStatementKind.Gsub:
                if (!AwkStatementLoweringShapes.SingleExpression(statement).TryGetValue(out var splitCall) ||
                    !Expression(splitCall, out var splitResult)) return false;
                Discard(splitResult);
                break;
            default: return false;
        }
        return true;
    }

    private void FinishStatement(int firstTemporary)
    {
        // Child statements retire their captures before their enclosing block does.
        // Calls and pending users complete before this boundary; nonlocal flow unwinds.
        if (_values > firstTemporary)
            Emit(new(AwkRegionInstructionKind.ReleaseValues, firstTemporary, Right: _values - firstTemporary));
        Emit(new(AwkRegionInstructionKind.StatementBoundary));
    }

    private bool TryGetControl(bool forContinue, [NotNullWhen(true)] out ControlScope? selected)
    {
        foreach (var control in _controls)
        {
            if (forContinue && !control.IsLoop) continue;
            selected = control;
            return true;
        }
        selected = null;
        return false;
    }

    private bool PrepareLeafOperand(AwkExpressionSyntax syntax, out int result)
    {
        result = 0;
        switch (syntax.Kind)
        {
            case AwkExpressionKind.Number:
                if (!AwkSourceLiterals.TryReadSwitchConstant(syntax, _scope, out var number)) return false;
                result = Constant(number.AsNumber());
                return true;
            case AwkExpressionKind.String:
                if (!AwkSourceLiterals.TryReadStringLiteral(syntax, _scope, out var literal)) return false;
                result = Operand(AwkRegionOperandKind.Literal, _literals.Count);
                _literals.Add(new(AwkText.FromSourceLiteralString(literal), _scope.Checkpoint));
                return true;
            case AwkExpressionKind.Regex:
                if (!AwkSourceLiterals.TryLowerRegexLiteralPattern(syntax, out var regex, _scope.CompatibilityMode, _scope.Checkpoint)) return false;
                result = EmitRegexMatch(new(Operand(AwkRegionOperandKind.Field, 0), regex), negated: false);
                return true;
            case AwkExpressionKind.Variable:
                if (!Variable(syntax, out var variable)) return false;
                result = variable;
                return true;
            default: return false;
        }
    }

    private bool InputPipe(AwkInputPipeGetlineSyntax syntax)
    {
        if (!syntax.Command.Expression.TryGetValue(out var command)) return false;
        var target = syntax.Target.Expression;
        if (!syntax.Target.IsEmpty && !target.HasValue) return false;
        return Getline(new(target, command, AwkGetlineSourceKind.InputPipe), isExpression: false, out _);
    }

    private bool Getline(AwkGetlineParts parts, bool isExpression, out int result) =>
        PrepareOperandWork(new GetlineWork(parts, isExpression), out result);

    private bool HostBuiltin(AwkExpressionSyntax syntax, AwkBuiltinDefinition definition,
        AwkBuiltinImplementation.AsyncBuiltinImplementation implementation, out int result,
        bool flushBeforeArguments = false)
    {
        return PrepareOperandWork(new HostBuiltinWork(syntax, definition, implementation, flushBeforeArguments), out result);
    }

    private void CaptureStringArguments(Span<int> arguments, int count, AwkBuiltinDefinition definition)
    {
        for (var index = 0; index < count; index++)
        {
            if (_operands[arguments[index]].Kind is AwkRegionOperandKind.NumberTemporary or AwkRegionOperandKind.ValueTemporary or AwkRegionOperandKind.TextTemporary) continue;
            var captured = _values++;
            Emit(new(AwkRegionInstructionKind.BuiltinArgument, captured, arguments[index],
                Detail: (int)definition.InputConsumption(index)));
            arguments[index] = Operand(AwkRegionOperandKind.ValueTemporary, captured);
        }
    }

    private bool ArrayAccess(AwkExpressionSyntax syntax, out int index) =>
        PrepareOperandWork(new ArrayAccessWork(syntax), out index);

    private bool NestedAccess(AwkExpressionSyntax syntax, out int index, bool deletion = false) =>
        PrepareOperandWork(new ArrayAccessWork(syntax, forcePath: true, deletion), out index);

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
            if (_paths[_arrays[access].Path].ImmediateKeys is null)
                Emit(new(AwkRegionInstructionKind.PathEnd, _arrays[access].Path));
            return;
        }
        if (_operands[_arrays[access].Key] is not { Kind: AwkRegionOperandKind.KeyBuilder } key) return;
        Emit(new(AwkRegionInstructionKind.KeyEnd, key.Index));
        _activeBuilders--;
    }

    private void Store(int target, int value)
    {
        var operand = _operands[target];
        if (operand.Kind == AwkRegionOperandKind.ArrayElement)
        {
            PrepareImmediatePath(operand.Index);
            Emit(new(AwkRegionInstructionKind.ArrayAssign, operand.Index, value));
            FinishKey(operand.Index);
        }
        else if (operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField)
            Emit(new(AwkRegionInstructionKind.AssignField, target, value));
        else Assign(target, value);
    }

    private bool Field(AwkExpressionSyntax syntax, out int operand) =>
        PrepareOperandWork(new FieldWork(syntax), out operand);

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
        if (index.Kind != AwkExpressionKind.Number || !AwkSourceLiterals.TryReadSwitchConstant(index, _scope, out var constant)) return false;
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

    private AwkRegionPrint AllocatePrint(AwkRegionPrintArgument[] arguments, AwkRegionOutput output)
    {
        if (arguments[^1] is { Kind: AwkRegionPrintArgumentKind.Text } last && _code.Count > 0 &&
            _code[^1] is { Kind: AwkRegionInstructionKind.Substring } producer && producer.Target == last.Index &&
            last.Index == _texts - 1 && _operands[^1] is { Kind: AwkRegionOperandKind.TextTemporary } result && result.Index == last.Index)
        {
            // One immediate consumer completes the print before the byte view
            // expires; no retained text result or fragment staging is needed.
            _code[^1] = producer with { Target = 0, Right = _prints.Count + 1 };
            arguments[^1] = new(AwkRegionPrintArgumentKind.PreparedText, 0);
            _texts--;
            _operands.RemoveAt(_operands.Count - 1);
        }
        var print = new AwkRegionPrint(_texts, _printSegments, arguments, output);
        if (output.IsRedirected || (!print.UsesFieldViews && arguments.Length > 1)) _usesOutputBuilder = true;
        _texts = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_texts, print.TextCount));
        if (arguments.Length > 1 || output.IsRedirected || print.UsesFieldViews)
            _printSegments = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedAdd(_printSegments, arguments.Length));
        return print;
    }

    private void EmitPrint(AwkRegionPrint print)
    {
        var site = _prints.Count;
        _prints.Add(print);
        if (!print.HasPreparedText) Emit(new(AwkRegionInstructionKind.PrintList, Left: site));
    }

    private bool PrintList(IReadOnlyList<AwkExpressionSyntax> arguments, AwkRegionOutput output)
    {
        if (arguments.Count == 0) return false;
        var prepared = new AwkRegionPrintArgument[arguments.Count];
        var fieldList = true;
        for (var index = 0; index < arguments.Count; index++)
        {
            // $0 can reconstruct and retire record slices. Such a list owns its
            // earlier values rather than carrying views across reconstruction.
            if (TryFixedField(arguments[index], out var field) && field != 0)
            {
                prepared[index] = new(AwkRegionPrintArgumentKind.Field, field);
                continue;
            }
            fieldList = false;
            break;
        }
        if (fieldList)
        {
            for (var index = 0; index < prepared.Length; index++)
                prepared[index] = prepared[index] with { FallbackValue = _values++ };
        }
        else
        {
            for (var index = 0; index < arguments.Count; index++)
            {
                if (!Expression(arguments[index], out var value, preserveOrigin: true)) return false;
                prepared[index] = PrintArgument(value, final: index == arguments.Count - 1);
            }
        }
        EmitPrint(AllocatePrint(prepared, output));
        return true;
    }

    private AwkRegionPrintArgument PrintArgument(int value, bool final)
    {
        var source = _operands[value];
        if (source.Kind == AwkRegionOperandKind.NumberTemporary)
            return new(AwkRegionPrintArgumentKind.Number, source.Index);
        if (source.Kind == AwkRegionOperandKind.TextTemporary)
            return new(AwkRegionPrintArgumentKind.Text, source.Index);
        // No argument work follows this read. Fields keep their existing capture
        // boundaries; only complete fixed-field lists defer those reads.
        if (final && (source.IsVariable || source.Kind is AwkRegionOperandKind.Literal or AwkRegionOperandKind.ArrayElement))
            return new(AwkRegionPrintArgumentKind.Current, value);
        var captured = CaptureValue(value, preserveOrigin: true);
        return new(AwkRegionPrintArgumentKind.Value, _operands[captured].Index);
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

    private void Assign(int variable, int value)
    {
        if (_operands[value].Kind != AwkRegionOperandKind.NumberTemporary)
        {
            var assignment = Emit(new(AwkRegionInstructionKind.Assign, variable, value));
            if (_operands[variable].Kind is AwkRegionOperandKind.LocalVariable or AwkRegionOperandKind.GlobalVariable &&
                _operands[value] is { Kind: AwkRegionOperandKind.ValueTemporary } source && assignment > 0 &&
                _code[assignment - 1] is { Kind: AwkRegionInstructionKind.Call, Detail: 0 } call && call.Target == source.Index)
                _code[assignment - 1] = call with { Detail = (int)AwkRegionReturnConsumer.AssignmentValue };
            return;
        }
        Emit(new(AwkRegionInstructionKind.AssignNumber, variable, NumericRegister(value)));
    }

    private bool Branch(AwkExpressionSyntax syntax, bool whenTrue, List<int> destinations) =>
        PrepareWork(new BranchWork(syntax, whenTrue, destinations));

    private int EmitBranch(int operand, bool whenTrue)
    {
        var source = _operands[operand];
        var branch = source.Kind == AwkRegionOperandKind.NumberTemporary
            ? Emit(new(whenTrue ? AwkRegionInstructionKind.JumpIfNonzero : AwkRegionInstructionKind.JumpIfZero, Left: source.Index))
            : Emit(new(whenTrue ? AwkRegionInstructionKind.JumpIfTrue : AwkRegionInstructionKind.JumpIfFalse, Left: operand));
        if (source.Kind == AwkRegionOperandKind.NumberTemporary &&
            operand == _operands.Count - 1 && branch > 0 &&
            _code[branch - 1] is { Kind: AwkRegionInstructionKind.Compare or AwkRegionInstructionKind.CompareText } comparison &&
            comparison.Target == source.Index)
        {
            // Only this fresh result has one immediate consumer. Keep its branch
            // descriptor, including conditional field demand. An inserted poll
            // breaks adjacency and retains the ordinary numeric continuation.
            _code[branch - 1] = comparison with { Target = -1 };
        }
        return branch;
    }

    private int CaptureByteText(int input)
    {
        if (_operands[input].Kind != AwkRegionOperandKind.ValueTemporary)
        {
            var slot = _values++;
            Emit(new(AwkRegionInstructionKind.Value, slot, input));
            input = Operand(AwkRegionOperandKind.ValueTemporary, slot);
        }
        Emit(new(AwkRegionInstructionKind.PrepareByteText, _operands[input].Index));
        return input;
    }

    private int EmitRegexMatch(AwkRegionRegexMatch match, bool negated)
    {
        var site = _regexMatches.Count;
        _regexMatches.Add(match);
        var number = _numbers++;
        Emit(new(AwkRegionInstructionKind.RegexMatch, number, site, Detail: negated ? 1 : 0));
        if (_operands[match.Input].Kind == AwkRegionOperandKind.ValueTemporary)
            Emit(new(AwkRegionInstructionKind.ReleaseValue, _operands[match.Input].Index));
        return Operand(AwkRegionOperandKind.NumberTemporary, number);
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
            index = _operands.Count;
            _variableIndices.Add(name, index);
            if (_scope.TryGetFunctionLocalSlot(name, out var slot))
                _operands.Add(new(AwkRegionOperandKind.LocalVariable, slot.Index, name));
            else if (AwkSpecialVariables.TryGetScalar(name, out var id) &&
                AwkCompatibilityNames.EnablesSpecial(id, _scope.CompatibilityMode))
                _operands.Add(new(AwkRegionOperandKind.SpecialVariable, (int)id, name));
            else if (_scope.TryGetGlobalReadIndex(name, out var global))
                _operands.Add(new(AwkRegionOperandKind.GlobalVariable, global, name));
            else _operands.Add(new(AwkRegionOperandKind.DynamicVariable, -1, name));
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
            _code[^1] = call with { Target = target, Detail = (int)AwkRegionReturnConsumer.Number };
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
            AwkRegionOperandKind.TextTemporary => AwkRegionInstructionKind.TextNumber,
            AwkRegionOperandKind.ArrayElement => AwkRegionInstructionKind.ArrayNumber,
            AwkRegionOperandKind.LocalVariable => AwkRegionInstructionKind.LocalNumber,
            AwkRegionOperandKind.GlobalVariable => AwkRegionInstructionKind.GlobalNumber,
            AwkRegionOperandKind.SpecialVariable or AwkRegionOperandKind.DynamicVariable => AwkRegionInstructionKind.VariableNumber,
            _ => throw new InvalidOperationException("A numeric operand requires a value location.")
        };
        _lastNumberRead = Emit(new(kind, target, source.IsVariable ? operand : source.Index));
        _lastNumberOperand = operand;
        return Operand(AwkRegionOperandKind.NumberTemporary, target);
    }

    private int CaptureValue(int operand, bool preserveOrigin = false)
    {
        if (_operands[operand].Kind is AwkRegionOperandKind.NumberTemporary or AwkRegionOperandKind.ValueTemporary or AwkRegionOperandKind.TextTemporary)
            return operand;
        var target = _values++;
        Emit(new(AwkRegionInstructionKind.Value, target, operand, Detail: preserveOrigin ? 1 : 0));
        return Operand(AwkRegionOperandKind.ValueTemporary, target);
    }

    private void Discard(int operand)
    {
        var source = _operands[operand];
        if (source.Kind == AwkRegionOperandKind.TextTemporary && _code.Count > 0 &&
            _code[^1] is { Kind: AwkRegionInstructionKind.Assign } textAssignment && textAssignment.Left == operand &&
            _operands[textAssignment.Target].Kind == AwkRegionOperandKind.LocalVariable)
            // No consumer follows this statement's assignment. Its local owner
            // can take the canonical text instead of retaining a second claim.
            _code[^1] = textAssignment with { Kind = AwkRegionInstructionKind.AssignText, Left = source.Index };
        if (source.Kind == AwkRegionOperandKind.ValueTemporary && _code.Count >= 2 &&
            _code[^1] is { Kind: AwkRegionInstructionKind.Assign } assignment && assignment.Left == operand &&
            _code[^2] is { Kind: AwkRegionInstructionKind.Call, Detail: (int)AwkRegionReturnConsumer.AssignmentValue } call &&
            call.Target == source.Index)
            _code[^2] = call with { Detail = (int)AwkRegionReturnConsumer.AssignmentDiscarded };
        if (source.IsVariable || source.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField or AwkRegionOperandKind.ArrayElement)
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

    private sealed class ControlScope(bool isLoop, int firstTemporary)
    {
        internal bool IsLoop { get; } = isLoop;
        internal int FirstTemporary { get; } = firstTemporary;
        internal List<int> Breaks { get; } = [];
        internal List<int> Continues { get; } = [];
    }

    private readonly record struct PreparedTarget(int Operand, int Capture);
}
