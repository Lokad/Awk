using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
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
