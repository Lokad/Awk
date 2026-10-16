namespace Lokad.Awk;

// Immutable compilation metadata under the existing program owner. Recursive and
// host-dependent functions retain their ordinary executor.
internal sealed class AwkSynchronousFunction(AwkFunctionLocalLayout layout, AwkSynchronousExpression body)
{
    public AwkFunctionLocalLayout Layout { get; } = layout;
    public AwkSynchronousExpression Body { get; } = body;

    public AwkValue EvaluateBody(AwkEvaluationContext context)
    {
        var state = context.State;
        state.Counters.RecordSynchronousFunction();
        state.RecordExecutionStep(); // Return statement checkpoint in its enclosing block.
        return Body.EvaluateSynchronous(context).AsScalarCopy();
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
                if (prepared.ContainsKey(name) || !function.TryGetReturnExpression(out var expression) ||
                    !expression.TryPrepareFunctionCalls(prepared, 16, out var body) ||
                    body.SynchronousCallDepth >= 4) continue;
                prepared.Add(name, new(function.LocalLayout, body));
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
            var depth = function.Body.SynchronousDepth;
            var calls = function.Body.SynchronousCallDepth;
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
