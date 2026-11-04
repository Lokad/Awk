using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkUserFunction
{
    // Preparation finishes before the program is published to any execution.
    private AwkStatement[] _statements = [];
    internal AwkRegion? Region { get; private set; }

    internal void PrepareRegion(AwkRegion region)
    {
        Region = region;
        _statements = [];
    }

    internal AwkUserFunction(AwkFunctionName name, AwkFunctionLocalLayout localLayout)
    {
        FunctionName = name;
        LocalLayout = localLayout;
    }

    public AwkUserFunction(
        AwkFunctionName name,
        AwkFunctionLocalLayout localLayout,
        IReadOnlyList<AwkStatement> statements)
        : this(name, localLayout)
    {
        PrepareBody(statements);
    }

    internal void PrepareBody(IReadOnlyList<AwkStatement> statements) =>
        _statements = AwkCollectionSnapshots.Copy(statements);

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
                var argument = expression.TryResolveArrayArgument(state, out var arrayArgument)
                    ? arrayArgument
                    : AwkFunctionArgument.FromValue(await EvaluateArgumentAsync(expression, 0, context).ConfigureAwait(false));
                PushArguments([argument], context);
            }
            else using (var buffer = state.RentFunctionArguments(arguments.Count))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                {
                    if ((index & 63) == 0) state.CancellationCheckpoint();
                    var argument = arguments[index];
                    if (argument.TryResolveArrayArgument(state, out var arrayArgument))
                    {
                        values[index] = arrayArgument;
                        continue;
                    }

                    var value = await EvaluateArgumentAsync(argument, index, context).ConfigureAwait(false);
                    buffer.Retain(value);
                    values[index] = AwkFunctionArgument.FromValue(value);
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

    private ValueTask<AwkValue> EvaluateArgumentAsync(AwkExpression expression, int index, AwkEvaluationContext context) =>
        LocalLayout.ConsumesArithmeticInput(index)
            ? expression.EvaluateArithmeticArgumentAsync(context) : expression.EvaluateAsync(context);

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
            else using (var buffer = state.RentFunctionArguments(arguments.Count))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                {
                    if ((index & 63) == 0) state.CancellationCheckpoint();
                    buffer.Retain(arguments[index]);
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
        try { return await ExecuteBodyAsync(context, context.State.CurrentFunctionFrame).ConfigureAwait(false); }
        finally { context.State.PopFunctionFrame(); }
    }

    // The caller owns activation/unwinding. During migration this also serves the
    // remaining host/statement bodies; there is one argument binder and frame owner.
    internal async ValueTask<AwkValue> ExecuteBodyAsync(AwkEvaluationContext context, AwkFunctionFrame frame)
    {
        var state = context.State;
        AwkStatementFlow flow;
        if (Region is { } region)
        {
            flow = context.TryGetExecutionContext(out var execution)
                ? await region.ExecuteAsync(state, frame.GetRegionStorage(state), execution.Output, execution.Io).ConfigureAwait(false)
                : region.Execute(state, frame.GetRegionStorage(state));
        }
        else
        {
            if (_statements.Length == 0 || !context.TryGetExecutionContext(out var execution)) return AwkValue.Unassigned();
            flow = await AwkStatementBlockExecutor.ExecuteAsync(_statements, context).ConfigureAwait(false);
        }
        if (flow == AwkStatementFlow.ReturnFunction)
            return state.TryConsumeFunctionReturnValue(out var value) ? value : AwkValue.Unassigned();
        if (flow is AwkStatementFlow.BreakLoop or AwkStatementFlow.ContinueLoop)
            throw new AwkRuntimeException(flow == AwkStatementFlow.BreakLoop ? AwkMessages.ErrorBreakOutsideLoop : AwkMessages.ErrorContinueOutsideLoop);
        if (flow is AwkStatementFlow.NextRecord or AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
        {
            state.ValidateFunctionFlow(flow);
            throw new AwkFunctionFlowSignal(flow);
        }
        return AwkValue.Unassigned();
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
        value.TryGetArrayBinding(out var array)
            ? Array(array)
            : value.TryGetArrayName(out var name)
            ? Array(name)
            : value.IsUntypedValue
            ? new(AwkFunctionArgumentKind.UntypedScalar, value, default)
            : Scalar(value.AsScalarCopy());

    public static AwkFunctionArgument Array(AwkArrayName arrayName) =>
        new(AwkFunctionArgumentKind.Array, default, arrayName);

    public static AwkFunctionArgument Array(AwkArrayIdentity array) =>
        new(AwkFunctionArgumentKind.Array, AwkValue.FromArrayReference(array), default);

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
