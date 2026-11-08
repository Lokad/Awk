using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private PreparationWork<int> CreateCallWork(AwkExpressionSyntax syntax)
    {
        if (AwkExpressionSyntaxFacts.TryGetCallableTarget(syntax, out var target) &&
            !_scope.TryGetFunctionLocalSlot(new(target.Identifier), out _))
        {
            if (AwkBuiltins.IsName(target.Name, AwkBuiltinId.Getline) &&
                AwkGetlineSyntaxFacts.TryResolveExpressionCallParts(syntax, out var parts)) return new GetlineWork(parts, isExpression: true);
            if (AwkBuiltins.IsName(target.Name, AwkBuiltinId.Length) && target.HasDelimitedCall && syntax.Children.Count is 1 or 2)
                return new LengthWork(syntax.Children.Count == 1 ? null : syntax.Children[1]);
            if (AwkBuiltins.TryGetCallable(target.Name, out var builtin) &&
                AwkCompatibilityNames.EnablesBuiltin(builtin, _scope.CompatibilityMode) &&
                target.HasDelimitedCall && AwkBuiltinCallShape.AcceptsArgumentCount(builtin, syntax.Children.Count - 1))
            {
                if (builtin.Id is AwkBuiltinId.IsArray or AwkBuiltinId.TypeOf)
                    return new IntrospectionWork(syntax.Children[1], builtin.Id == AwkBuiltinId.TypeOf);
                if (builtin.Id is AwkBuiltinId.Split or AwkBuiltinId.Patsplit) return new SplitWork(syntax, builtin.Id == AwkBuiltinId.Patsplit);
                if (builtin.Id == AwkBuiltinId.Match) return new MatchWork(syntax);
                if (builtin.Id is AwkBuiltinId.Sub or AwkBuiltinId.Gsub) return new SubstituteWork(syntax, builtin.Id == AwkBuiltinId.Gsub);
                if (builtin.Id is AwkBuiltinId.Asort or AwkBuiltinId.Asorti) return new SortWork(syntax, builtin.Id == AwkBuiltinId.Asorti);
                if (builtin.Id == AwkBuiltinId.Gensub) return new GensubWork(syntax);
                if (builtin.Id is AwkBuiltinId.Index or AwkBuiltinId.Substr or AwkBuiltinId.ToLower or AwkBuiltinId.ToUpper)
                    return new StringCallWork(syntax, builtin.Id);
                if (builtin.TryGetScalarComputation(out var computation))
                    return new ScalarBuiltinWork(syntax, builtin, computation);
                if (builtin.TryGetHostComputation(out var host)) return new HostBuiltinWork(syntax, builtin, host, false);
            }
            else if (_scope.Functions is { } functions && functions.TryGetValue(new(target.Identifier), out var function))
                return new UserCallWork(syntax, function);
        }
        return new LeafOperandWork(syntax);
    }

    private sealed class UserCallWork(AwkExpressionSyntax syntax, AwkUserFunction function) : PreparationWork<int>
    {
        private bool _started;
        private int _call;
        private int _argument = 1;
        private readonly List<AwkRegionArgument> _arguments = [];
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            if (!_started)
            {
                _started = true;
                var slot = compiler._activeCalls++;
                compiler._incomingCalls = Math.Max(compiler._incomingCalls, compiler._activeCalls);
                _call = compiler._calls.Count;
                compiler._calls.Add(new(function, slot));
                compiler.Emit(new(AwkRegionInstructionKind.BeginCall, Left: _call));
            }
            else
            {
                var argument = CompletedResult<int>(completed);
                // Callee consumption is finalized before any region is prepared.
                var kind = compiler._operands[argument].Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField &&
                    function.LocalLayout.ConsumesArithmeticInput(_argument - 1)
                    ? AwkRegionArgumentKind.ArithmeticField
                    : syntax.Children[_argument].Kind == AwkExpressionKind.Group
                        ? AwkRegionArgumentKind.Grouped : AwkRegionArgumentKind.Value;
                _arguments.Add(new(argument, kind));
                // Only adjacent staging can share a segment. Intervening work
                // keeps the earlier capture at its original language point.
                if (compiler._code.Count > 0 && compiler._code[^1] is
                    { Kind: AwkRegionInstructionKind.Arguments } segment && segment.Target == _call)
                    compiler._code[^1] = segment with { Right = segment.Right + 1 };
                else compiler.Emit(new(AwkRegionInstructionKind.Arguments, _call, _argument - 1, 1));
                _argument++;
            }
            if (_argument < syntax.Children.Count)
            {
                next = compiler.CreateOperandWork(syntax.Children[_argument], preserveOrigin: true);
                return true;
            }
            var firstPending = _arguments.Count;
            if (compiler._code.Count > 0 && compiler._code[^1] is
                { Kind: AwkRegionInstructionKind.Arguments } pending && pending.Target == _call)
            {
                firstPending = pending.Left;
                compiler._code.RemoveAt(compiler._code.Count - 1);
                compiler._straightLine--;
            }
            compiler._calls[_call] = new(function, compiler._calls[_call].Slot, _arguments.ToArray());
            var returned = compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.Call, returned, _call, firstPending));
            compiler._activeCalls--;
            return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, returned));
        }
    }

    private sealed class ScalarBuiltinWork(AwkExpressionSyntax syntax, AwkBuiltinDefinition definition,
        AwkBuiltinImplementation.SyncBuiltinImplementation implementation) : PreparationWork<int>
    {
        private bool _started;
        private int _start;
        private int _argument;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var count = syntax.Children.Count - 1;
            if (!_started)
            {
                _started = true;
                _start = compiler._values;
                compiler._values = checked(compiler._values + count);
            }
            else
            {
                compiler.Emit(new(AwkRegionInstructionKind.BuiltinArgument, _start + _argument, CompletedResult<int>(completed),
                    Detail: definition.ConsumesArithmeticInputs ? 1 : 0));
                _argument++;
            }
            if (_argument < count)
            {
                next = compiler.CreateOperandWork(syntax.Children[_argument + 1]);
                return true;
            }
            // Retain captures before effects; fold only their final uninterrupted suffix.
            var firstPending = compiler._code.Count;
            while (firstPending > 0 && compiler._code[firstPending - 1] is { Kind: AwkRegionInstructionKind.BuiltinArgument } capture &&
                capture.Target >= _start && capture.Target < _start + count) firstPending--;
            var pendingArguments = firstPending == compiler._code.Count ? [] : new int[compiler._code.Count - firstPending];
            for (var index = 0; index < pendingArguments.Length; index++)
                pendingArguments[index] = compiler._code[firstPending + index].Left;
            compiler._code.RemoveRange(firstPending, pendingArguments.Length);
            var site = compiler._builtins.Count;
            compiler._builtins.Add(new(implementation, _start, count, pendingArguments));
            var returned = compiler._values++;
            compiler.Emit(new(AwkRegionInstructionKind.Builtin, returned, site));
            return Complete(compiler.Operand(AwkRegionOperandKind.ValueTemporary, returned));
        }
    }

    private sealed class HostBuiltinWork(AwkExpressionSyntax syntax, AwkBuiltinDefinition definition,
        AwkBuiltinImplementation.AsyncBuiltinImplementation implementation, bool flushBeforeArguments) : PreparationWork<int>
    {
        private bool _started;
        private int _start;
        private int _returned;
        private int _result;
        private int _boundary;
        private int _argument;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            var count = syntax.Children.Count - 1;
            if (!_started)
            {
                _started = true;
                _start = compiler._values;
                compiler._values = checked(compiler._values + count);
                _returned = compiler._values++;
                _result = compiler.Operand(AwkRegionOperandKind.ValueTemporary, _returned);
                _boundary = compiler.Emit(new(AwkRegionInstructionKind.BuiltinBoundary, Left: _returned,
                    Detail: flushBeforeArguments ? 1 : 0));
            }
            else
            {
                compiler.Emit(new(AwkRegionInstructionKind.BuiltinArgument, _start + _argument, CompletedResult<int>(completed)));
                // close formats its captured name before evaluating the direction.
                if (definition.Id == AwkBuiltinId.Close && _argument == 0)
                    compiler.Emit(new(AwkRegionInstructionKind.PrepareString, _start));
                _argument++;
            }
            if (_argument < count)
            {
                next = compiler.CreateOperandWork(syntax.Children[_argument + 1]);
                return true;
            }
            var site = compiler._builtins.Count;
            compiler._builtins.Add(new(null, _start, count, [], implementation));
            compiler.Emit(new(AwkRegionInstructionKind.Builtin, _returned, site));
            compiler.Patch(_boundary, compiler.Label());
            return Complete(_result);
        }
    }
}
