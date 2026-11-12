using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private sealed class LengthWork(AwkExpressionSyntax? argument) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (argument is not null && !_waiting)
            {
                _waiting = true;
                next = compiler.CreateOperandWork(argument, preserveOrigin: true);
                return true;
            }
            var value = argument is null ? compiler.Operand(AwkRegionOperandKind.Field, 0) : CompletedResult<int>(completed);
            var number = compiler._numbers++;
            if (compiler.TryTakeFreshTextProducer(value, AwkRegionInstructionKind.Case, out var producer))
                // Case's immediate length consumer uses a numeric destination.
                compiler._code[^1] = producer with { Target = number, Right = 1 };
            else
                compiler.Emit(new(AwkRegionInstructionKind.Length, number, value,
                    Detail: argument is not null && AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out _) ? 1 : 0));
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, number));
        }
    }

    private sealed class IntrospectionWork(AwkExpressionSyntax argument, bool typeName) : PreparationWork<int>
    {
        private bool _waiting;
        private AwkRegionInstructionKind _operation;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            int operand;
            if (!_waiting)
            {
                if (AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out _) && compiler.Variable(argument, out operand))
                    _operation = AwkRegionInstructionKind.InspectVariable;
                else
                {
                    var unwrapped = argument;
                    while (unwrapped.Kind == AwkExpressionKind.Group && unwrapped.Children.Count == 1)
                    {
                        compiler._scope.CheckCancellation();
                        unwrapped = unwrapped.Children[0];
                    }
                    _waiting = true;
                    _operation = unwrapped.Kind == AwkExpressionKind.ArrayElement
                        ? AwkRegionInstructionKind.InspectArray : AwkRegionInstructionKind.InspectValue;
                    next = _operation == AwkRegionInstructionKind.InspectArray ? new ArrayAccessWork(unwrapped)
                        : compiler.CreateOperandWork(argument, preserveOrigin: true);
                    return true;
                }
            }
            else operand = CompletedResult<int>(completed);
            var target = typeName ? compiler._values++ : compiler._numbers++;
            compiler.Emit(new(_operation, target, operand, Detail: typeName ? 1 : 0));
            if (_operation == AwkRegionInstructionKind.InspectArray) compiler.FinishKey(operand);
            return Complete(compiler.Operand(typeName ? AwkRegionOperandKind.ValueTemporary : AwkRegionOperandKind.NumberTemporary, target));
        }
    }

    private sealed class ArrayTargetWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private bool _waiting;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            int access;
            if (!_waiting)
            {
                if (AwkExpressionSyntaxFacts.TryGetIdentifier(syntax, out var name))
                {
                    if (compiler._scope.IsReservedVariable(name.Name)) return false;
                    access = compiler._arrays.Count;
                    compiler._arrays.Add(new(new(name.Identifier), compiler._scope.GetArrayBinding(name.Identifier), -1));
                }
                else
                {
                    if (compiler._scope.CompatibilityMode != AwkCompatibilityMode.GnuDefault || syntax.Kind != AwkExpressionKind.ArrayElement)
                        return false;
                    _waiting = true;
                    next = new ArrayAccessWork(syntax);
                    return true;
                }
            }
            else access = CompletedResult<int>(completed);
            var slot = compiler._arrayTargets++;
            compiler.Emit(new(AwkRegionInstructionKind.ArrayTarget, slot, access));
            if (compiler._arrays[access].Key >= 0 || compiler._arrays[access].Path >= 0) compiler.FinishKey(access);
            return Complete(slot);
        }
    }

    private sealed class SplitWork(AwkExpressionSyntax syntax, bool patsplit) : PreparationWork<int>
    {
        private byte _stage;
        private int _input;
        private int _array;
        private int _separators = -1;
        private PreparedPattern _pattern;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var usesDefault = syntax.Children.Count == 3;
            switch (_stage)
            {
                case 0:
                    _stage = 1;
                    next = compiler.CreateOperandWork(syntax.Children[1]);
                    return true;
                case 1:
                    _input = compiler._values++;
                    compiler.Emit(new(AwkRegionInstructionKind.Value, _input, CompletedResult<int>(completed)));
                    _stage = 2;
                    next = new ArrayTargetWork(syntax.Children[2]);
                    return true;
                case 2:
                    _array = CompletedResult<int>(completed);
                    if (usesDefault) _pattern = new(compiler._values++, AwkRegexPatternOrigin.Runtime, false);
                    else
                    {
                        _stage = 3;
                        next = new PatternWork(syntax.Children[3]);
                        return true;
                    }
                    break;
                case 3:
                    _pattern = CompletedResult<PreparedPattern>(completed);
                    break;
                case 4:
                    _separators = CompletedResult<int>(completed);
                    break;
            }
            if (_stage != 4 && syntax.Children.Count == 5)
            {
                _stage = 4;
                next = new ArrayTargetWork(syntax.Children[4]);
                return true;
            }
            var site = compiler._splits.Count;
            compiler._splits.Add(new(_input, _array, _separators, _pattern.Slot, _pattern.Origin, _pattern.Literal, usesDefault, patsplit));
            var target = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.Split, target, site));
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, target));
        }
    }

    private sealed class MatchWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private byte _stage;
        private int _input;
        private PreparedPattern _pattern;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            switch (_stage++)
            {
                case 0:
                    next = compiler.CreateOperandWork(syntax.Children[1]);
                    return true;
                case 1:
                    _input = compiler._values++;
                    compiler.Emit(new(AwkRegionInstructionKind.Value, _input, CompletedResult<int>(completed)));
                    next = new PatternWork(syntax.Children[2]);
                    return true;
                case 2:
                    _pattern = CompletedResult<PreparedPattern>(completed);
                    if (syntax.Children.Count == 4)
                    {
                        next = new ArrayTargetWork(syntax.Children[3]);
                        return true;
                    }
                    break;
            }
            var array = syntax.Children.Count == 4 ? CompletedResult<int>(completed) : -1;
            var site = compiler._matches.Count;
            compiler._matches.Add(new(_input, _pattern.Slot, _pattern.Origin, array));
            var target = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.Match, target, site));
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, target));
        }
    }

    private sealed class SubstituteWork(AwkExpressionSyntax syntax, bool global) : PreparationWork<int>
    {
        private byte _stage;
        private PreparedPattern _pattern;
        private int _replacement;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            switch (_stage++)
            {
                case 0:
                    next = new PatternWork(syntax.Children[1]);
                    return true;
                case 1:
                    _pattern = CompletedResult<PreparedPattern>(completed);
                    next = compiler.CreateOperandWork(syntax.Children[2]);
                    return true;
                case 2:
                    _replacement = compiler._values++;
                    compiler.Emit(new(AwkRegionInstructionKind.Value, _replacement, CompletedResult<int>(completed)));
                    if (syntax.Children.Count == 4)
                    {
                        next = new LocationWork(syntax.Children[3], strict: true);
                        return true;
                    }
                    break;
            }
            var location = syntax.Children.Count == 4 ? CompletedResult<PreparedLocation>(completed) : new(-1, -1);
            var site = compiler._substitutions.Count;
            compiler._substitutions.Add(new(_pattern.Slot, _pattern.Origin, _replacement, location.Operand, global));
            var number = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.Substitute, number, site));
            if (location.Access >= 0) compiler.FinishKey(location.Access);
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, number));
        }
    }

    private sealed class GensubWork(AwkExpressionSyntax syntax) : PreparationWork<int>
    {
        private bool _started;
        private PreparedPattern _pattern;
        private int _start;
        private int _position = -1;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_started)
            {
                _started = true;
                next = new PatternWork(syntax.Children[1]);
                return true;
            }
            var count = syntax.Children.Count - 2;
            if (_position < 0)
            {
                _pattern = CompletedResult<PreparedPattern>(completed);
                _start = compiler._values;
                compiler._values = checked(compiler._values + count);
                _position = 0;
            }
            else
            {
                compiler.Emit(new(AwkRegionInstructionKind.Value, _start + _position, CompletedResult<int>(completed), Detail: 1));
                _position++;
            }
            if (_position < count)
            {
                next = compiler.CreateOperandWork(syntax.Children[_position + 2], preserveOrigin: true);
                return true;
            }
            var site = compiler._gensubs.Count;
            compiler._gensubs.Add(new(_pattern.Slot, _pattern.Origin, _start, count));
            var target = compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.Gensub, target, site));
            return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, target));
        }
    }

    private sealed class SortWork(AwkExpressionSyntax syntax, bool keys) : PreparationWork<int>
    {
        private byte _stage;
        private int _source;
        private int _destination;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            switch (_stage)
            {
                case 0:
                    _stage = 1;
                    next = new ArrayTargetWork(syntax.Children[1]);
                    return true;
                case 1:
                    _source = _destination = CompletedResult<int>(completed);
                    _stage = 3;
                    if (syntax.Children.Count >= 3)
                    {
                        _stage = 2;
                        next = new ArrayTargetWork(syntax.Children[2]);
                        return true;
                    }
                    break;
                case 2:
                    _destination = CompletedResult<int>(completed);
                    _stage = 3;
                    break;
            }
            var order = -1;
            if (_stage == 3 && syntax.Children.Count == 4)
            {
                _stage = 4;
                next = compiler.CreateOperandWork(syntax.Children[3]);
                return true;
            }
            if (_stage == 4)
            {
                order = compiler._values++;
                compiler.Emit(new(AwkRegionInstructionKind.Value, order, CompletedResult<int>(completed)));
                compiler.Emit(new(AwkRegionInstructionKind.PrepareString, order));
            }
            var site = compiler._sorts.Count;
            var comparison = compiler._numbers++;
            compiler._sorts.Add(new(_source, _destination, order, compiler._sortSlots++, compiler._activeCalls, comparison, keys));
            compiler._incomingCalls = Math.Max(compiler._incomingCalls, compiler._activeCalls + 1);
            compiler.Emit(new(AwkRegionInstructionKind.SortBegin, Left: site));
            var target = compiler._numbers++;
            compiler.Emit(new(AwkRegionInstructionKind.SortAdvance, target, site));
            return Complete(compiler.Operand(AwkRegionOperandKind.NumberTemporary, target));
        }
    }
}
