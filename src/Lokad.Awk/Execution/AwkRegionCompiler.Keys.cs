using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private sealed class ArrayKeyWork(IReadOnlyList<AwkExpressionSyntax> parts, int first) : PreparationWork<int>
    {
        private int _position = -1;
        private int _builder;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_position < 0)
            {
                if (parts.Count <= first) return false;
                _position = first;
                if (parts.Count - first > 1)
                {
                    _builder = compiler._activeBuilders++;
                    compiler._builderCount = Math.Max(compiler._builderCount, compiler._activeBuilders);
                    compiler.Emit(new(AwkRegionInstructionKind.KeyBegin, _builder));
                }
            }
            else
            {
                var value = CompletedResult<int>(completed);
                if (parts.Count - first == 1) return Complete(value);
                compiler.Emit(new(AwkRegionInstructionKind.KeyAppend, _builder, value));
                _position++;
                if (_position == parts.Count)
                {
                    compiler.Emit(new(AwkRegionInstructionKind.KeyReady, _builder));
                    return Complete(compiler.Operand(AwkRegionOperandKind.KeyBuilder, _builder));
                }
                compiler.Emit(new(AwkRegionInstructionKind.KeySeparator, _builder));
            }
            next = compiler.CreateOperandWork(parts[_position]);
            return true;
        }
    }

    private sealed class ArrayAccessWork(AwkExpressionSyntax syntax, bool forcePath = false, bool deletion = false) : PreparationWork<int>
    {
        private readonly List<AwkExpressionSyntax> _levels = [];
        private AwkArrayName _name;
        private AwkArrayBinding _binding;
        private int _path = -1;
        private int _depth;
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                if (syntax.Kind != AwkExpressionKind.ArrayElement || syntax.Children.Count < 2) return false;
                var root = syntax;
                if (forcePath || syntax.Children[0].Kind == AwkExpressionKind.ArrayElement)
                {
                    while (root.Kind == AwkExpressionKind.ArrayElement && root.Children.Count > 1)
                    {
                        compiler._scope.CheckCancellation();
                        _levels.Add(root);
                        root = root.Children[0];
                    }
                }
                else root = syntax.Children[0];
                if (!AwkExpressionSyntaxFacts.TryGetIdentifier(root, out var name) || compiler._scope.IsReservedVariable(name.Name)) return false;
                _name = new(name.Identifier);
                _binding = compiler._scope.GetArrayBinding(name.Identifier);
                if (_levels.Count > 0)
                {
                    _path = compiler._paths.Count;
                    compiler._paths.Add(new(_name, _binding, _levels.Count));
                    compiler.Emit(new(AwkRegionInstructionKind.PathBegin, _path));
                }
                _waiting = true;
            }
            else
            {
                var key = CompletedResult<int>(completed);
                if (_path < 0)
                {
                    if (compiler._operands[key].Kind == AwkRegionOperandKind.ArrayElement) key = compiler.CaptureValue(key);
                    var access = compiler._arrays.Count;
                    compiler._arrays.Add(new(_name, _binding, key));
                    return Complete(access);
                }
                compiler.Emit(new(AwkRegionInstructionKind.PathKey, _path, key, _depth, deletion ? 1 : 0));
                if (compiler._operands[key] is { Kind: AwkRegionOperandKind.KeyBuilder } builder)
                {
                    compiler.Emit(new(AwkRegionInstructionKind.KeyEnd, builder.Index));
                    compiler._activeBuilders--;
                }
                _depth++;
                if (_depth == _levels.Count)
                {
                    var access = compiler._arrays.Count;
                    compiler._arrays.Add(new(_name, _binding, -1, _path));
                    return Complete(access);
                }
            }
            next = new ArrayKeyWork(_path < 0 ? syntax.Children : _levels[^(_depth + 1)].Children, 1);
            return true;
        }
    }

    private sealed class ArrayReadWork(AwkExpressionSyntax syntax, bool preserveOrigin) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_waiting)
            {
                _waiting = true;
                next = new ArrayAccessWork(syntax);
                return true;
            }
            var access = CompletedResult<int>(completed);
            if (compiler._arrays[access].Path < 0 && compiler._operands[compiler._arrays[access].Key].Kind != AwkRegionOperandKind.KeyBuilder)
                return Complete(compiler.Operand(AwkRegionOperandKind.ArrayElement, access));
            var captured = compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.ArrayRead, captured, access, Detail: preserveOrigin ? 1 : 0));
            compiler.FinishKey(access);
            return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, captured));
        }
    }

    private sealed class MembershipWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private byte _stage;
        private bool _named;
        private int _key;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (_stage == 0)
            {
                _named = AwkExpressionSyntaxFacts.TryGetIdentifier(syntax.Children[1], out var array);
                if (_named ? compiler._scope.IsReservedVariable(array.Name) : syntax.Children[1].Kind != AwkExpressionKind.ArrayElement) return false;
                var index = syntax.Children[0];
                if (index.Kind == AwkExpressionKind.Group && index.FirstChild is { Kind: AwkExpressionKind.ExpressionList } tuple) index = tuple;
                _stage = 1;
                next = index.Kind == AwkExpressionKind.ExpressionList ? new ArrayKeyWork(index.Children, 0) : compiler.CreateOperandWork(index);
                return true;
            }
            if (_stage == 1)
            {
                _key = CompletedResult<int>(completed);
                if (!_named)
                {
                    if (compiler._operands[_key].Kind != AwkRegionOperandKind.KeyBuilder)
                    {
                        // Finish conversion before the RHS can change its owner or CONVFMT.
                        var builder = compiler._activeBuilders++;
                        compiler._builderCount = Math.Max(compiler._builderCount, compiler._activeBuilders);
                        compiler.Emit(new(AwkRegionInstructionKind.KeyEnd, builder));
                        compiler.Emit(new(AwkRegionInstructionKind.KeyAppend, builder, _key));
                        _key = compiler.Operand(AwkRegionOperandKind.KeyBuilder, builder);
                    }
                    _stage = 2;
                    next = new ArrayAccessWork(syntax.Children[1], forcePath: true);
                    return true;
                }
                if (!AwkExpressionSyntaxFacts.TryGetIdentifier(syntax.Children[1], out var array)) return false;
                var access = compiler._arrays.Count;
                compiler._arrays.Add(new(new(array.Identifier), compiler._scope.GetArrayBinding(array.Identifier), _key));
                var number = compiler._numbers++;
                compiler.Emit(new(AwkRegionInstructionKind.ArrayContains, number, access));
                compiler.FinishKey(access);
                return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, number));
            }
            var nested = CompletedResult<int>(completed);
            var found = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.NestedContains, found, compiler._arrays[nested].Path, compiler._operands[_key].Index));
            compiler.FinishKey(nested);
            compiler.Emit(new(AwkRegionInstructionKind.KeyEnd, compiler._operands[_key].Index));
            compiler._activeBuilders--;
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, found));
        }
    }
}
