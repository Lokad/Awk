namespace Lokad.Awk;

// Immutable compilation metadata under the existing program owner. Recursive and
// host-dependent functions retain their ordinary executor.
internal sealed class AwkSynchronousFunction(AwkFunctionLocalLayout layout, AwkSynchronousExpression body)
{
    private readonly AwkSynchronousExpression _condition = AwkSynchronousExpression.Unavailable;
    private readonly AwkSynchronousExpression _alternative = body;
    private readonly AwkSynchronousExpression _prefix = AwkSynchronousExpression.Unavailable;
    private readonly (AwkVariableName Name, AwkFunctionLocalSlotId Slot) _local;

    private AwkSynchronousFunction(AwkFunctionLocalLayout layout, AwkSynchronousExpression body,
        AwkSynchronousExpression condition, AwkSynchronousExpression alternative,
        (AwkVariableName Name, AwkFunctionLocalSlotId Slot) local, AwkSynchronousExpression prefix) : this(layout, body)
    {
        _condition = condition;
        _alternative = alternative;
        _local = local;
        _prefix = prefix;
    }

    public AwkFunctionLocalLayout Layout { get; } = layout;
    public AwkSynchronousExpression Body { get; } = body;
    internal bool HasLocalAssignment => _prefix.SynchronousDepth > 0;
    internal int SynchronousDepth => Math.Max(_prefix.SynchronousDepth, Math.Max(Body.SynchronousDepth,
        Math.Max(_condition.SynchronousDepth, _alternative.SynchronousDepth)));
    internal int SynchronousCallDepth => Math.Max(_prefix.SynchronousCallDepth, Math.Max(Body.SynchronousCallDepth,
        Math.Max(_condition.SynchronousCallDepth, _alternative.SynchronousCallDepth)));

    public AwkValue EvaluateBody(AwkEvaluationContext context)
    {
        var state = context.State;
        state.Counters.RecordSynchronousFunction();
        if (HasLocalAssignment)
        {
            state.RecordExecutionStep(); // Assignment statement in the function block.
            state.RecordExecutionStep(); // Assignment expression, before its value.
            state.AssignFunctionLocalValue(_local.Slot, _local.Name, _prefix.EvaluateSynchronous(context));
        }
        var selected = Body;
        if (_condition.SynchronousDepth > 0)
        {
            state.RecordExecutionStep(); // If statement, before its condition.
            if (!_condition.EvaluateSynchronous(context).IsTruthy()) selected = _alternative;
        }
        state.RecordExecutionStep(); // Return statement checkpoint in its enclosing block.
        return selected.EvaluateSynchronous(context).AsScalarCopy();
    }

    public static IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? Prepare(
        Dictionary<AwkFunctionName, AwkUserFunction> functions, Action? checkpoint)
    {
        if (functions.Count == 0) return null;
        var prepared = new Dictionary<AwkFunctionName, AwkSynchronousFunction>();
        for (var pass = 0; pass < 4; pass++)
        {
            var count = prepared.Count;
            foreach (var (name, function) in functions)
            {
                checkpoint?.Invoke();
                if (prepared.ContainsKey(name) ||
                    !function.TryGetSynchronousReturnBody(out var expression, out var condition, out var alternative,
                        out var local, out var prefix) ||
                    !expression.TryPrepareFunctionCalls(prepared, 16, out var body)) continue;
                var initial = AwkSynchronousExpression.Unavailable;
                if (prefix.HasValue && !prefix.Value.TryPrepareFunctionCalls(prepared, 16, out initial)) continue;
                var otherwise = body;
                if (condition.SynchronousDepth > 0 && (condition.SynchronousDepth > 16 ||
                    !alternative.TryPrepareFunctionCalls(prepared, 16, out otherwise))) continue;
                var candidate = new AwkSynchronousFunction(function.LocalLayout, body, condition, otherwise, local, initial);
                if (candidate.SynchronousDepth > 16 || candidate.SynchronousCallDepth >= 4) continue;
                prepared.Add(name, candidate);
            }
            if (prepared.Count == count) break;
        }
        foreach (var (name, function) in prepared)
        {
            checkpoint?.Invoke();
            functions[name] = functions[name].WithSynchronousFunction(function);
        }
        return prepared;
    }
}

internal abstract partial class AwkExpression
{
    internal virtual bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
        int remainingDepth, out AwkSynchronousExpression expression)
    {
        expression = this as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        return remainingDepth > 0 && expression.SynchronousDepth is > 0 and <= 16;
    }

    private sealed class SynchronousFunctionCall : AwkSynchronousExpression
    {
        private readonly AwkSynchronousFunction _function;
        private readonly AwkSynchronousExpression[] _operands;

        public SynchronousFunctionCall(AwkSynchronousFunction function, AwkSynchronousExpression[] operands)
        {
            _function = function;
            _operands = operands;
            var depth = function.SynchronousDepth;
            var calls = function.SynchronousCallDepth;
            foreach (var operand in operands)
            {
                depth = Math.Max(depth, operand.SynchronousDepth);
                calls = Math.Max(calls, operand.SynchronousCallDepth);
            }
            SynchronousDepth = depth + 1;
            SynchronousCallDepth = calls + 1;
        }

        internal override int SynchronousDepth { get; }
        internal override int SynchronousCallDepth { get; }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var state = context.State;
            state.RecordExecutionStep(); // Invocation, after its call-expression checkpoint.
            state.ThrowIfCancellationRequested();
            if (_operands.Length == 0)
            {
                state.ThrowIfCancellationRequested();
                state.PushFunctionFrame(_function.Layout, []);
            }
            else if (_operands.Length == 1)
            {
                var operand = _operands[0];
                var argument = operand.TryResolveArrayArgument(state, out var alias)
                    ? alias : AwkFunctionArgument.FromValue(operand.EvaluateSynchronous(context));
                state.ThrowIfCancellationRequested();
                state.PushFunctionFrame(_function.Layout, [argument]);
            }
            else
            {
                // Preparation bounds arity to four; each nested call has its own temporary.
                Span<AwkFunctionArgument> arguments = [default, default, default, default];
                for (var index = 0; index < _operands.Length; index++)
                {
                    var operand = _operands[index];
                    arguments[index] = operand.TryResolveArrayArgument(state, out var alias)
                        ? alias : AwkFunctionArgument.FromValue(operand.EvaluateSynchronous(context));
                }
                state.ThrowIfCancellationRequested();
                state.PushFunctionFrame(_function.Layout, arguments[.._operands.Length]);
            }
            try { return _function.EvaluateBody(context); }
            finally { state.PopFunctionFrame(); }
        }
    }
}
