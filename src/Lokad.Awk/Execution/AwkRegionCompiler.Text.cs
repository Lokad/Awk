using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private readonly record struct PreparedPattern(int Slot, AwkRegexPatternOrigin Origin, bool Literal);

    private sealed class PatternWork(AwkExpressionSyntax syntax) : PreparationWork<PreparedPattern>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var origin = AwkRegexPatternOrigin.Runtime;
            var literal = false;
            int value;
            if (!_waiting)
            {
                if (AwkSourceLiterals.TryReadRegexLiteralPattern(syntax, out var pattern, compiler._scope.Checkpoint))
                {
                    literal = true;
                    origin = AwkRegexPatternOrigin.SourceLiteral;
                    value = compiler.Operand(AwkRegionOperandKind.Literal, compiler._literals.Count);
                    compiler._literals.Add(new(AwkText.FromSourceLiteralString(pattern), compiler._scope.Checkpoint));
                }
                else
                {
                    _waiting = true;
                    next = compiler.CreateOperandWork(syntax);
                    return true;
                }
            }
            else value = CompletedResult<int>(completed);
            var captured = compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.Value, captured, value));
            compiler.Emit(new(AwkRegionInstructionKind.PrepareByteText, captured));
            return Complete(new(captured, origin, literal));
        }
    }

    private sealed class RegexMatchWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private byte _stage;
        private int _input;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var negated = syntax.OperatorTokens[0].Kind == AwkTokenKind.BangTilde;
            if (_stage == 0)
            {
                _stage = 1;
                next = compiler.CreateOperandWork(syntax.Children[0]);
                return true;
            }
            if (_stage == 1)
            {
                _input = CompletedResult<int>(completed);
                if (AwkSourceLiterals.TryLowerRegexLiteralPattern(syntax.Children[1], out var literal,
                    compiler._scope.CompatibilityMode, compiler._scope.Checkpoint))
                    return Complete(compiler.EmitRegexMatch(new(_input, literal), negated));
                // Preserve the input value now, but format it after RHS effects.
                _input = compiler.CaptureValue(_input);
                _stage = 2;
                next = compiler.CreateOperandWork(syntax.Children[1]);
                return true;
            }
            var pattern = compiler.CaptureByteText(CompletedResult<int>(completed));
            _input = compiler.CaptureByteText(_input);
            var result = compiler.EmitRegexMatch(new(_input, null, compiler._operands[pattern].Index), negated);
            compiler.Emit(new(AwkRegionInstructionKind.ReleaseValue, compiler._operands[pattern].Index));
            return Complete(result);
        }
    }

    private sealed class StringCallWork(AwkExpressionSyntax syntax, AwkBuiltinDefinition definition) : PreparationWork<int>
    {
        private readonly int[] _arguments = [-1, -1, -1];
        private int _position;
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_waiting)
            {
                var value = CompletedResult<int>(completed);
                _arguments[_position] = value;
                if (compiler._operands[value].Kind == AwkRegionOperandKind.ArrayElement)
                    compiler.CaptureStringArguments(_arguments, _position + 1, definition);
                _position++;
            }
            if (_position < syntax.Children.Count - 1)
            {
                if (!compiler.IsImmediateRead(syntax.Children[_position + 1])) compiler.CaptureStringArguments(_arguments, _position, definition);
                _waiting = true;
                next = compiler.CreateOperandWork(syntax.Children[_position + 1]);
                return true;
            }
            var site = compiler._strings.Count;
            compiler._strings.Add(new(definition.Id, _arguments[0], _arguments[1], _arguments[2],
                definition.InputConsumption(1), definition.InputConsumption(2)));
            var target = definition.Id == AwkBuiltinId.Index ? compiler._numbers++ : compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.String, target, site));
            return Complete(compiler.Operand(definition.Id == AwkBuiltinId.Index ? AwkRegionOperandKind.NumberTemporary : AwkRegionOperandKind.ValueTemporary, target));
        }
    }

    private sealed class ConcatenationWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private readonly List<AwkExpressionSyntax> _parts = [];
        private int _start;
        private int _position = -1;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_position < 0)
            {
                var pending = new Stack<AwkExpressionSyntax>();
                pending.Push(syntax);
                while (pending.TryPop(out var part))
                {
                    compiler._scope.CheckCancellation();
                    if (part.Kind == AwkExpressionKind.Concatenation && part.Children.Count == 2)
                    {
                        pending.Push(part.Children[1]);
                        pending.Push(part.Children[0]);
                    }
                    else _parts.Add(part);
                }
                _start = compiler._texts;
                compiler._texts += _parts.Count;
                _position = 0;
            }
            else
            {
                // Format this operand before later work can change its source or CONVFMT.
                compiler.Emit(new(AwkRegionInstructionKind.ConcatArgument, _start + _position, CompletedResult<int>(completed)));
                _position++;
                if (_position == _parts.Count)
                {
                    var target = compiler._values++;
                    compiler.Emit(new(AwkRegionInstructionKind.ConcatValue, target, _start, _parts.Count));
                    return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, target));
                }
            }
            next = compiler.CreateOperandWork(_parts[_position]);
            return true;
        }
    }
}
