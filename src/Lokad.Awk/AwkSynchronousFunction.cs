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
        state.SetFunctionReturnValue(Body.EvaluateSynchronous(context));
        return state.TryConsumeFunctionReturnValue(out var value) ? value : AwkValue.Unassigned();
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

    private sealed class SynchronousFunctionCall(AwkSynchronousFunction function, AwkSynchronousExpression operand)
        : AwkSynchronousExpression
    {
        internal override int SynchronousDepth { get; } = 1 + Math.Max(function.Body.SynchronousDepth, operand.SynchronousDepth);
        internal override int SynchronousCallDepth { get; } = 1 + Math.Max(function.Body.SynchronousCallDepth, operand.SynchronousCallDepth);

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var state = context.State;
            state.RecordExecutionStep(); // Invocation, after its call-expression checkpoint.
            state.ThrowIfCancellationRequested();
            var argument = operand.TryGetVariableName(out var name) && state.TryResolveArrayArgument(name, out var alias)
                ? alias : AwkFunctionArgument.FromValue(operand.EvaluateSynchronous(context));
            state.ThrowIfCancellationRequested();
            state.PushFunctionFrame(function.Layout, [argument]);
            try { return function.EvaluateBody(context); }
            finally { state.PopFunctionFrame(); }
        }
    }
}
