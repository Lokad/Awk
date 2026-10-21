using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkUserFunction
{
    private readonly AwkStatement[] _statements;
    private readonly AwkSynchronousFunction? _synchronous;

    public AwkUserFunction(
        AwkFunctionName name,
        AwkFunctionLocalLayout localLayout,
        IReadOnlyList<AwkStatement> statements)
        : this(name, localLayout, AwkCollectionSnapshots.Copy(statements), null)
    {
    }

    private AwkUserFunction(AwkFunctionName name, AwkFunctionLocalLayout localLayout,
        AwkStatement[] statements, AwkSynchronousFunction? synchronous)
    {
        FunctionName = name;
        LocalLayout = localLayout;
        _statements = statements;
        _synchronous = synchronous;
    }

    internal AwkUserFunction WithSynchronousFunction(AwkSynchronousFunction function) =>
        new(FunctionName, LocalLayout, _statements, function);

    internal bool TryGetSynchronousReturnBody(out AwkExpression expression,
        out AwkSynchronousExpression condition, out AwkExpression alternative,
        out (AwkVariableName Name, AwkFunctionLocalSlotId Slot) local, out AwkOptionalExpression prefix)
    {
        expression = AwkSynchronousExpression.Unavailable;
        condition = AwkSynchronousExpression.Unavailable;
        alternative = AwkSynchronousExpression.Unavailable;
        local = default;
        prefix = AwkOptionalExpression.None;
        var start = 0;
        if (_statements.Length >= 2 && _statements[0].TryGetLocalAssignment(out local, out var value))
        {
            prefix = AwkOptionalExpression.Some(value);
            start = 1;
        }
        if (_statements.Length - start == 1)
            return _statements[start].TryGetReturnExpression(out expression);
        return _statements.Length - start == 2 &&
            _statements[start].TryGetGuardedReturn(out condition, out expression) &&
            _statements[start + 1].TryGetReturnExpression(out alternative);
    }

    public AwkFunctionName FunctionName { get; }

    public string Name => FunctionName.Text;

    public AwkFunctionLocalLayout LocalLayout { get; }

    public IReadOnlyList<AwkVariableName> Parameters => LocalLayout.Parameters;

    public async ValueTask<AwkValue> InvokeAsync(
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context)
    {
        var state = context.State;
        state.Checkpoint();
        try
        {
            // Completed ValueTasks otherwise deepen the CLR stack even while evaluating arguments.
            if (RequiresStackBoundary(state))
            {
                state.Counters.RecordFunctionStackSuspension();
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }
            state.ThrowIfCancellationRequested();

            if (arguments.Count == 1)
            {
                var expression = arguments[0];
                var argument = expression.TryGetVariableName(out var variableName) &&
                    state.TryResolveArrayArgument(variableName, out var arrayArgument)
                    ? arrayArgument
                    : AwkFunctionArgument.FromValue(await expression.EvaluateAsync(context).ConfigureAwait(false));
                PushArguments([argument], context);
            }
            else using (var buffer = RentedFunctionArguments.Rent(arguments.Count, state))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                {
                    if ((index & 63) == 0) state.CancellationCheckpoint();
                    var argument = arguments[index];
                    if (argument.TryGetVariableName(out var variableName) &&
                        state.TryResolveArrayArgument(variableName, out var arrayArgument))
                    {
                        values[index] = arrayArgument;
                        continue;
                    }

                    values[index] = AwkFunctionArgument.FromValue(
                        await argument.EvaluateAsync(context).ConfigureAwait(false));
                }

                PushArguments(values.AsSpan(0, arguments.Count), context);
            }

            return await InvokeFrameAsync(context).ConfigureAwait(false);
        }
        finally
        {
            // Check unwind independently: asynchronous host calls can create a long
            // continuation chain without consuming stack during function entry.
            if (RequiresStackBoundary(state))
            {
                state.Counters.RecordFunctionStackSuspension();
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }
        }
    }

    public async ValueTask<AwkValue> InvokeValuesAsync(
        IReadOnlyList<AwkValue> arguments,
        AwkEvaluationContext context)
    {
        var state = context.State;
        state.Checkpoint();
        try
        {
            if (RequiresStackBoundary(state))
            {
                state.Counters.RecordFunctionStackSuspension();
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }
            state.ThrowIfCancellationRequested();

            if (arguments.Count == 1)
            {
                PushArguments([AwkFunctionArgument.FromValue(arguments[0])], context);
            }
            else using (var buffer = RentedFunctionArguments.Rent(arguments.Count, state))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                {
                    if ((index & 63) == 0) state.CancellationCheckpoint();
                    values[index] = AwkFunctionArgument.FromValue(arguments[index]);
                }

                PushArguments(values.AsSpan(0, arguments.Count), context);
            }

            return await InvokeFrameAsync(context).ConfigureAwait(false);
        }
        finally
        {
            if (RequiresStackBoundary(state))
            {
                state.Counters.RecordFunctionStackSuspension();
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }
        }
    }

    private static bool RequiresStackBoundary(AwkRuntimeState state) =>
        // The CLR probe reserves space for ordinary work. A host allowing larger
        // expression trees retains an unconditional boundary for each invocation.
        state.Limits.MaxSyntaxNestingDepth > AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth ||
        !RuntimeHelpers.TryEnsureSufficientExecutionStack();

    private void PushArguments(
        ReadOnlySpan<AwkFunctionArgument> arguments,
        AwkEvaluationContext context)
    {
        context.State.ThrowIfCancellationRequested();
        context.State.PushFunctionFrame(LocalLayout, arguments);
    }

    private async ValueTask<AwkValue> InvokeFrameAsync(AwkEvaluationContext context)
    {
        var state = context.State;
        try
        {
            if (_statements.Length == 0)
                return AwkValue.Unassigned();

            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.Unassigned();

            if (_synchronous is { } prepared && state.LintMode == AwkLintMode.Off &&
                state.Limits.MaxSyntaxNestingDepth <= AwkRuntimeLimits.DefaultMaxSyntaxNestingDepth &&
                (!prepared.HasLocalAssignment || state.FieldsCannotReportRegexDiagnostics) &&
                !execution.Output.HasPendingOutput && RuntimeHelpers.TryEnsureSufficientExecutionStack())
                return prepared.EvaluateBody(context);

            var flow = await AwkStatementBlockExecutor
                .ExecuteAsync(_statements, context)
                .ConfigureAwait(false);
            if (flow == AwkStatementFlow.ReturnFunction)
                return state.TryConsumeFunctionReturnValue(out var value) ? value : AwkValue.Unassigned();

            if (flow is AwkStatementFlow.BreakLoop or AwkStatementFlow.ContinueLoop)
                throw new AwkRuntimeException(flow == AwkStatementFlow.BreakLoop
                    ? AwkMessages.ErrorBreakOutsideLoop
                    : AwkMessages.ErrorContinueOutsideLoop);

            if (flow is AwkStatementFlow.NextRecord or AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
            {
                state.ValidateFunctionFlow(flow);
                throw new AwkFunctionFlowSignal(flow);
            }

            return AwkValue.Unassigned();
        }
        finally
        {
            state.PopFunctionFrame();
        }
    }

    private readonly struct RentedFunctionArguments : IDisposable
    {
        private readonly AwkFunctionArgument[]? _values;
        private readonly AwkMemoryLease? _memory;
        private readonly bool _pooled;

        private RentedFunctionArguments(AwkFunctionArgument[] values, AwkMemoryLease memory, bool pooled)
        {
            _values = values;
            _memory = memory;
            _pooled = pooled;
        }

        public AwkFunctionArgument[] Values => _values ?? [];

        public static RentedFunctionArguments Rent(int count, AwkRuntimeState state)
        {
            if (count == 0) return new([], AwkMemoryLease.Disabled, false);
            var memory = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateFunctionArgumentArrayBytes(count));
            var pooled = !state.MemoryAccounting.IsEnabled;
            try
            {
                var values = pooled ? ArrayPool<AwkFunctionArgument>.Shared.Rent(count) : new AwkFunctionArgument[count];
                state.Counters.RecordFunctionArgumentBufferRent();
                return new(values, memory, pooled);
            }
            catch
            {
                memory.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_pooled) ArrayPool<AwkFunctionArgument>.Shared.Return(Values, clearArray: true);
            else Array.Clear(Values);
            _memory?.Dispose();
        }
    }

}

internal readonly struct AwkFunctionArgument
{
    private AwkFunctionArgument(
        AwkFunctionArgumentKind kind,
        AwkValue value,
        AwkArrayName arrayName)
    {
        Kind = kind;
        Value = value;
        ArrayName = arrayName;
    }

    public AwkFunctionArgumentKind Kind { get; }

    public AwkValue Value { get; }

    public AwkArrayName ArrayName { get; }

    public static AwkFunctionArgument Scalar(AwkValue value) =>
        new(AwkFunctionArgumentKind.Scalar, value, default);

    public static AwkFunctionArgument FromValue(AwkValue value) =>
        value.TryGetArrayName(out var name)
            ? Array(name)
            : value.IsUntypedValue
            ? new(AwkFunctionArgumentKind.UntypedScalar, value, default)
            : Scalar(value.AsScalarCopy());

    public static AwkFunctionArgument Array(AwkArrayName arrayName) =>
        new(AwkFunctionArgumentKind.Array, default, arrayName);

    public static AwkFunctionArgument PotentialArray(AwkArrayName arrayName) =>
        PotentialArray(arrayName, AwkValue.FromUntypedScalar(new AwkUntypedScalar()));

    public static AwkFunctionArgument PotentialArray(AwkArrayName arrayName, AwkValue value) =>
        new(AwkFunctionArgumentKind.PotentialArray, value, arrayName);
}

internal enum AwkFunctionArgumentKind
{
    Scalar,
    Array,
    PotentialArray,
    UntypedScalar
}
