namespace Lokad.Awk;

// Immutable compilation metadata under the existing program owner. Recursive and
// host-dependent functions retain their ordinary executor.
internal sealed class AwkSynchronousFunction(
    AwkFunctionLocalLayout layout, AwkStatement[] body, int depth, int calls)
{
    public AwkFunctionLocalLayout Layout { get; } = layout;
    internal int SynchronousDepth { get; } = depth;
    internal int SynchronousCallDepth { get; } = calls;

    public AwkValue EvaluateBody(AwkEvaluationContext context)
    {
        context.State.Counters.RecordSynchronousFunction();
        var value = AwkValue.Unassigned();
        var flow = AwkStatementBlockExecutor.ExecuteComputation(body, context, ref value);
        if (flow == AwkStatementFlow.ReturnFunction) return value.AsScalarCopy();
        if (flow is AwkStatementFlow.BreakLoop or AwkStatementFlow.ContinueLoop)
            throw new AwkRuntimeException(flow == AwkStatementFlow.BreakLoop
                ? AwkMessages.ErrorBreakOutsideLoop : AwkMessages.ErrorContinueOutsideLoop);
        return AwkValue.Unassigned();
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
                    !function.TryPrepareSynchronousBody(prepared, out var body, out var depth,
                        out var calls, checkpoint)) continue;
                var candidate = new AwkSynchronousFunction(function.LocalLayout, body, depth, calls);
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
        int remainingDepth, out AwkSynchronousExpression expression, Action? preparationCheckpoint = null)
    {
        expression = this as AwkSynchronousExpression ?? AwkSynchronousExpression.Unavailable;
        return remainingDepth > 0 && expression.SynchronousDepth is > 0 and <= 16;
    }

    private sealed class SynchronousFunctionCall : AwkSynchronousExpression
    {
        private readonly AwkSynchronousFunction _function;
        private readonly AwkSynchronousExpression[] _operands;

        public SynchronousFunctionCall(AwkSynchronousFunction function, AwkSynchronousExpression[] operands, int depth, int calls)
        {
            _function = function;
            _operands = operands;
            SynchronousDepth = depth;
            SynchronousCallDepth = calls;
        }

        internal override int SynchronousDepth { get; }
        internal override int SynchronousCallDepth { get; }

        protected override AwkValue EvaluateSynchronousCore(AwkEvaluationContext context)
        {
            var state = context.State;
            state.Checkpoint(); // Invocation, after its call-expression checkpoint.
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
            else if (_operands.Length <= 4) PushInlineArguments(context);
            else PushRentedArguments(context);
            try { return _function.EvaluateBody(context); }
            finally { state.PopFunctionFrame(); }
        }

        // Keep the staging cells out of the common frame used by unary and general calls.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void PushInlineArguments(AwkEvaluationContext context)
        {
            var state = context.State;
            // Keep small synchronous calls on their stack-local argument storage.
            Span<AwkFunctionArgument> arguments = [default, default, default, default];
            AwkMemoryLease? staged = null;
            try
            {
                for (var index = 0; index < _operands.Length; index++)
                {
                    var operand = _operands[index];
                    arguments[index] = operand.TryResolveArrayArgument(state, out var alias)
                        ? alias : AwkFunctionArgument.FromValue(operand.EvaluateSynchronous(context));
                    var bytes = arguments[index].Value.RetainedTextMemoryBytes;
                    if (state.MemoryAccounting.IsEnabled && bytes > 0)
                    {
                        staged ??= state.MemoryAccounting.CreateTransientLease();
                        staged.ReserveAdditional(bytes);
                    }
                }
                state.ThrowIfCancellationRequested();
                state.PushFunctionFrame(_function.Layout, arguments[.._operands.Length]);
            }
            finally { staged?.Dispose(); }
        }

        private void PushRentedArguments(AwkEvaluationContext context)
        {
            var state = context.State;
            using var buffer = AwkUserFunction.RentedFunctionArguments.Rent(_operands.Length, state);
            var arguments = buffer.Values;
            for (var index = 0; index < _operands.Length; index++)
            {
                if ((index & 63) == 0) state.CancellationCheckpoint();
                var operand = _operands[index];
                if (operand.TryResolveArrayArgument(state, out var alias))
                {
                    arguments[index] = alias;
                    continue;
                }
                var value = operand.EvaluateSynchronous(context);
                buffer.Retain(value);
                arguments[index] = AwkFunctionArgument.FromValue(value);
            }
            state.ThrowIfCancellationRequested();
            state.PushFunctionFrame(_function.Layout, arguments.AsSpan(0, _operands.Length));
        }
    }
}
