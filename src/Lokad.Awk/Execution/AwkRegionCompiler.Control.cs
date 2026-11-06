using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private sealed class StatementsWork(IReadOnlyList<AwkStatementSyntax> statements) : PreparationWork
    {
        private int _position;
        private int _firstTemporary;
        private bool _waiting;
        internal AwkStatementSyntax? CurrentStatement => _position < statements.Count ? statements[_position] : null;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_waiting)
            {
                compiler.FinishStatement(_firstTemporary);
                _position++;
                _waiting = false;
            }
            while (_position < statements.Count)
            {
                compiler._scope.CheckCancellation();
                var statement = statements[_position];
                _firstTemporary = compiler._values;
                if (statement.Kind == AwkStatementKind.Block) next = new StatementsWork(statement.BodyStatements);
                else if (statement.Kind == AwkStatementKind.If) next = new IfWork(statement);
                else if (statement.Kind is AwkStatementKind.While or AwkStatementKind.DoWhile) next = new LoopWork(statement);
                else if (statement.Kind == AwkStatementKind.For)
                    next = statement.Payload is AwkForStatementSyntaxPayload { Header.IsForIn: true } loop
                        ? new IterationWork(loop.Header, statement.BodyStatements) : new LoopWork(statement);
                else if (statement.Kind == AwkStatementKind.Switch) next = new SwitchWork(statement);
                else
                {
                    if (!compiler.PrepareStatement(statement)) return false;
                    // Return owns output completion and callee retirement. Its
                    // fallthrough cleanup would be unreachable, but later source
                    // still needs preparation/validation.
                    if (statement.Kind != AwkStatementKind.Return)
                        compiler.FinishStatement(_firstTemporary);
                    _position++;
                    continue;
                }
                _waiting = true;
                return true;
            }
            return Complete();
        }
    }

    private sealed class IfWork(AwkStatementSyntax statement) : PreparationWork
    {
        private readonly List<int> _otherwise = [];
        private byte _stage;
        private int _done;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            switch (_stage++)
            {
                case 0:
                    if (statement.Expressions.Count != 1 || !statement.Expression.TryGetValue(out var condition)) return false;
                    next = new BranchWork(condition, false, _otherwise);
                    return true;
                case 1:
                    next = new StatementsWork(statement.BodyStatements);
                    return true;
                case 2:
                    _done = compiler.Emit(new(AwkRegionInstructionKind.Jump));
                    compiler.PatchBranches(_otherwise, compiler.Label());
                    next = new StatementsWork(statement.ElseStatements);
                    return true;
                default:
                    compiler.Patch(_done, compiler.Label());
                    return Complete();
            }
        }
    }

    private sealed class ConditionalWork(AwkExpressionSyntax syntax, bool preserveOrigin) : PreparationWork<int>
    {
        private readonly List<int> _otherwise = [];
        private byte _stage;
        private int _target;
        private int _done;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            switch (_stage++)
            {
                case 0:
                    next = new BranchWork(syntax.Children[0], false, _otherwise);
                    return true;
                case 1:
                    _target = compiler._values++;
                    next = compiler.CreateOperandWork(syntax.Children[1], preserveOrigin);
                    return true;
                case 2:
                    compiler.Emit(new(AwkRegionInstructionKind.Value, _target, CompletedResult<int>(completed), Detail: preserveOrigin ? 1 : 0));
                    _done = compiler.Emit(new(AwkRegionInstructionKind.Jump));
                    compiler.PatchBranches(_otherwise, compiler.Label());
                    next = compiler.CreateOperandWork(syntax.Children[2], preserveOrigin);
                    return true;
                default:
                    compiler.Emit(new(AwkRegionInstructionKind.Value, _target, CompletedResult<int>(completed), Detail: preserveOrigin ? 1 : 0));
                    compiler.Patch(_done, compiler.Label());
                    return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, _target));
            }
        }
    }

    private sealed class BranchWork(AwkExpressionSyntax syntax, bool whenTrue, List<int> destinations) : PreparationWork
    {
        private byte _stage;
        private List<int>? _bypass;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_stage == 0)
            {
                while (true)
                {
                    compiler._scope.CheckCancellation();
                    if (syntax.Kind == AwkExpressionKind.Group && syntax.Children.Count == 1) syntax = syntax.Children[0];
                    else if (syntax.Kind == AwkExpressionKind.Unary && syntax.Children.Count == 1 &&
                        syntax.OperatorTokens.Count == 1 && syntax.OperatorTokens[0].Kind == AwkTokenKind.Bang)
                    {
                        syntax = syntax.Children[0];
                        whenTrue = !whenTrue;
                    }
                    else break;
                }
                if (syntax.Kind == AwkExpressionKind.Binary && syntax.Children.Count == 2 && syntax.OperatorTokens.Count == 1 &&
                    syntax.OperatorTokens[0].Kind is AwkTokenKind.AmpersandAmpersand or AwkTokenKind.PipePipe)
                {
                    var conjunction = syntax.OperatorTokens[0].Kind == AwkTokenKind.AmpersandAmpersand;
                    _stage = 1;
                    if (conjunction != whenTrue) next = new BranchWork(syntax.Children[0], whenTrue, destinations);
                    else
                    {
                        _bypass = [];
                        next = new BranchWork(syntax.Children[0], !whenTrue, _bypass);
                    }
                }
                else
                {
                    _stage = 3;
                    next = compiler.CreateOperandWork(syntax);
                }
                return true;
            }
            if (_stage == 1)
            {
                _stage = 2;
                next = new BranchWork(syntax.Children[1], whenTrue, destinations);
                return true;
            }
            if (_stage == 3) destinations.Add(compiler.EmitBranch(CompletedResult<int>(completed), whenTrue));
            else if (_bypass is { } bypass) compiler.PatchBranches(bypass, compiler.Label());
            return Complete();
        }
    }
}
