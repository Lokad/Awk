using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private sealed class LoopWork(AwkStatementSyntax statement) : PreparationWork
    {
        private readonly List<int> _conditionJumps = [];
        private AwkForHeaderSyntax? _header;
        private ControlScope? _loop;
        private ControlScope Loop => _loop ?? throw new InvalidOperationException("The loop body has not started.");
        private byte _stage;
        private int _start;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            while (true)
            {
                switch (_stage)
                {
                    case 0:
                        if (statement.Kind == AwkStatementKind.For)
                        {
                            if (statement.Payload is not AwkForStatementSyntaxPayload { Header.Parts.Count: 3 } payload) return false;
                            _header = payload.Header;
                            if (_header.Parts[0].Expression.TryGetValue(out var initial))
                            {
                                _stage = 1;
                                next = compiler.CreateOperandWork(initial);
                                return true;
                            }
                        }
                        else if (statement.Expressions.Count != 1) return false;
                        _stage = 2;
                        break;
                    case 1:
                        compiler.Discard(CompletedResult<int>(completed));
                        _stage = 2;
                        break;
                    case 2:
                        _start = compiler.Label();
                        _stage = 3;
                        if (statement.Kind != AwkStatementKind.DoWhile)
                        {
                            var condition = _header is null ? statement.Expression : _header.Parts[1].Expression;
                            if (condition.TryGetValue(out var expression))
                            {
                                next = new BranchWork(expression, false, _conditionJumps);
                                return true;
                            }
                        }
                        break;
                    case 3:
                        _loop = new(isLoop: true, compiler._values);
                        compiler._controls.Push(Loop);
                        _stage = 4;
                        next = new StatementsWork(statement.BodyStatements);
                        return true;
                    case 4:
                        compiler._controls.Pop();
                        AwkExpressionSyntax? step = null;
                        if (_header is not null) _header.Parts[2].Expression.TryGetValue(out step);
                        // Without tail computation, the head already polls after
                        // body/continue cleanup and before the next condition.
                        var continuation = step is not null || statement.Kind == AwkStatementKind.DoWhile
                            ? compiler.Label() : _start;
                        foreach (var position in Loop.Continues) compiler.Patch(position, continuation);
                        if (step is not null)
                        {
                            _stage = 5;
                            next = compiler.CreateOperandWork(step);
                            return true;
                        }
                        _stage = 6;
                        break;
                    case 5:
                        compiler.Discard(CompletedResult<int>(completed));
                        _stage = 6;
                        break;
                    case 6:
                        _stage = 7;
                        if (statement.Kind == AwkStatementKind.DoWhile)
                        {
                            if (!statement.Expression.TryGetValue(out var condition)) return false;
                            next = new BranchWork(condition, false, _conditionJumps);
                            return true;
                        }
                        break;
                    default:
                        compiler.Emit(new(AwkRegionInstructionKind.Jump, _start));
                        var end = compiler.Label();
                        compiler.PatchBranches(_conditionJumps, end);
                        foreach (var position in Loop.Breaks) compiler.Patch(position, end);
                        return Complete();
                }
            }
        }
    }

    private sealed class IterationWork(AwkForHeaderSyntax header, IReadOnlyList<AwkStatementSyntax> body) : PreparationWork
    {
        private ControlScope? _loop;
        private int _slot;
        private int _start;
        private int _next;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_loop is { } loop)
            {
                compiler._controls.Pop();
                foreach (var position in loop.Continues) compiler.Patch(position, _start);
                compiler.Emit(new(AwkRegionInstructionKind.Jump, _start));
                var end = compiler.Label();
                compiler.Patch(_next, end);
                foreach (var position in loop.Breaks) compiler.Patch(position, end);
                compiler.Emit(new(AwkRegionInstructionKind.IteratorEnd, _slot));
                return Complete();
            }
            if (header.Parts.Count != 2 || !header.Parts[0].Expression.TryGetValue(out var variableSyntax) ||
                !compiler.Variable(variableSyntax, out var variable) || !header.Parts[1].Expression.TryGetValue(out var arraySyntax)) return false;
            int access;
            if (AwkExpressionSyntaxFacts.TryGetIdentifier(arraySyntax, out var name))
            {
                if (compiler._scope.IsReservedVariable(name.Name)) return false;
                access = compiler._arrays.Count;
                compiler._arrays.Add(new(new(name.Identifier), compiler._scope.GetArrayBinding(name.Identifier), -1));
            }
            else if (arraySyntax.Kind != AwkExpressionKind.ArrayElement || !compiler.ArrayAccess(arraySyntax, out access)) return false;
            _slot = compiler._iterators++;
            var site = compiler._iterations.Count;
            compiler._iterations.Add(new(access, _slot, compiler._sortSlots++, compiler._activeCalls, compiler._numbers++));
            compiler._incomingCalls = Math.Max(compiler._incomingCalls, compiler._activeCalls + 1);
            compiler.Emit(new(AwkRegionInstructionKind.IteratorBegin, Left: site));
            if (compiler._arrays[access].Key >= 0) compiler.FinishKey(access);
            compiler.Emit(new(AwkRegionInstructionKind.IteratorSortAdvance, Left: site));
            _start = compiler.Label();
            _next = compiler.Emit(new(AwkRegionInstructionKind.IteratorNext, Left: _slot, Right: variable));
            _loop = new(isLoop: true, compiler._values);
            compiler._controls.Push(_loop);
            next = new StatementsWork(body);
            return true;
        }
    }

    private sealed class SwitchWork(AwkStatementSyntax statement) : PreparationWork
    {
        private IReadOnlyList<AwkSwitchCaseSyntax> _arms = [];
        private ControlScope? _control;
        private ControlScope Control => _control ?? throw new InvalidOperationException("The switch has not started.");
        private int[] _branches = [];
        private int _defaultArm = -1;
        private int _unmatched;
        private int _arm;
        private byte _stage;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_stage == 0)
            {
                if (compiler._scope.CompatibilityMode != AwkCompatibilityMode.GnuDefault ||
                    !AwkStatementLoweringShapes.Switch(statement).TryGetValue(out var shape) ||
                    !shape.Header.Expression.TryGetValue(out var selectorSyntax)) return false;
                _arms = shape.SwitchCases;
                _control = new(isLoop: false, compiler._values);
                _stage = 1;
                next = compiler.CreateOperandWork(selectorSyntax);
                return true;
            }
            if (_stage == 1)
            {
                var selector = compiler.CaptureValue(CompletedResult<int>(completed));
                _branches = new int[_arms.Count];
                for (var index = 0; index < _branches.Length; index++)
                {
                    compiler._scope.CheckCancellation();
                    var arm = _arms[index];
                    if (arm.IsDefault) { _branches[index] = -1; _defaultArm = index; continue; }
                    if (!arm.Expression.TryGetValue(out var pattern)) return false;
                    AwkSwitchCasePattern prepared;
                    if (AwkSourceLiterals.TryLowerRegexLiteralPattern(pattern, out var regex, compiler._scope.CompatibilityMode, compiler._scope.Checkpoint))
                        prepared = AwkSwitchCasePattern.FromRegex(regex);
                    else if (AwkSourceLiterals.TryReadSwitchConstant(pattern, compiler._scope, out var value))
                        prepared = AwkSwitchCasePattern.FromValue(value);
                    else return false;
                    var site = compiler._switchCases.Count;
                    compiler._switchCases.Add(prepared);
                    _branches[index] = compiler.Emit(new(AwkRegionInstructionKind.JumpIfCase, Left: selector, Right: site));
                }
                _unmatched = compiler.Emit(new(AwkRegionInstructionKind.Jump));
                compiler._controls.Push(Control);
                _stage = 2;
            }
            else if (_stage == 3) { _arm++; _stage = 2; }
            if (_arm < _arms.Count)
            {
                var start = compiler.Label();
                if (_branches[_arm] >= 0) compiler.Patch(_branches[_arm], start);
                if (_arm == _defaultArm) compiler.Patch(_unmatched, start);
                _stage = 3;
                next = new StatementsWork(_arms[_arm].BodyStatements);
                return true;
            }
            compiler._controls.Pop();
            var end = compiler.Label();
            if (_defaultArm < 0) compiler.Patch(_unmatched, end);
            foreach (var position in Control.Breaks) compiler.Patch(position, end);
            return Complete();
        }
    }
}
