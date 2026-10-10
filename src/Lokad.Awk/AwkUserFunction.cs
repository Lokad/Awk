using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal interface IAwkUserFunction
{
    AwkFunctionName FunctionName { get; }

    string Name { get; }

    AwkFunctionLocalLayout LocalLayout { get; }

    IReadOnlyList<AwkVariableName> Parameters { get; }

    ValueTask<AwkValue> InvokeAsync(
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context);

    ValueTask<AwkValue> InvokeValuesAsync(
        IReadOnlyList<AwkValue> arguments,
        AwkEvaluationContext context);
}

internal sealed class AwkUserFunction : IAwkUserFunction
{
    private readonly AwkStatement[] _statements;

    public AwkUserFunction(
        AwkFunctionName name,
        AwkFunctionLocalLayout localLayout,
        IReadOnlyList<AwkStatement> statements)
    {
        FunctionName = name;
        LocalLayout = localLayout;
        _statements = AwkCollectionSnapshots.Copy(statements);
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
        var requiresStackBoundary = state.EnterFunctionInvocation();
        try
        {
            // Completed ValueTasks otherwise deepen the CLR stack even while evaluating arguments.
            if (requiresStackBoundary)
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            state.ThrowIfCancellationRequested();

            using (var buffer = RentedFunctionArguments.Rent(arguments.Count))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                {
                    var argument = arguments[index];
                    if (argument.TryGetVariableName(out var variableName) &&
                        state.TryResolveArrayArgument(variableName, out var arrayArgument))
                    {
                        values[index] = arrayArgument;
                        continue;
                    }

                    values[index] = AwkFunctionArgument.Scalar(
                        await argument.EvaluateAsync(context).ConfigureAwait(false));
                }

                PushArguments(values, arguments.Count, context);
            }

            return await InvokeFrameAsync(context).ConfigureAwait(false);
        }
        finally
        {
            // The unwind needs its own boundary: a chain of inline continuations can also overflow.
            if (requiresStackBoundary)
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            state.LeaveFunctionInvocation();
        }
    }

    public async ValueTask<AwkValue> InvokeValuesAsync(
        IReadOnlyList<AwkValue> arguments,
        AwkEvaluationContext context)
    {
        var state = context.State;
        var requiresStackBoundary = state.EnterFunctionInvocation();
        try
        {
            if (requiresStackBoundary)
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            state.ThrowIfCancellationRequested();

            using (var buffer = RentedFunctionArguments.Rent(arguments.Count))
            {
                var values = buffer.Values;
                for (var index = 0; index < arguments.Count; index++)
                    values[index] = AwkFunctionArgument.Scalar(arguments[index]);

                PushArguments(values, arguments.Count, context);
            }

            return await InvokeFrameAsync(context).ConfigureAwait(false);
        }
        finally
        {
            if (requiresStackBoundary)
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            state.LeaveFunctionInvocation();
        }
    }

    private void PushArguments(
        AwkFunctionArgument[] arguments,
        int argumentCount,
        AwkEvaluationContext context)
    {
        context.State.ThrowIfCancellationRequested();
        context.State.PushFunctionFrame(LocalLayout, arguments.AsSpan(0, argumentCount));
    }

    private async ValueTask<AwkValue> InvokeFrameAsync(AwkEvaluationContext context)
    {
        var state = context.State;
        try
        {
            if (_statements.Length == 0)
                return AwkValue.FromText(string.Empty);

            if (!context.TryGetExecutionContext(out _))
                return AwkValue.FromText(string.Empty);

            var flow = await AwkStatementBlockExecutor
                .ExecuteAsync(_statements, context)
                .ConfigureAwait(false);
            if (flow == AwkStatementFlow.ReturnFunction)
                return state.TryConsumeFunctionReturnValue(out var value) ? value : AwkValue.FromText(string.Empty);

            if (flow is AwkStatementFlow.BreakLoop or AwkStatementFlow.ContinueLoop)
                throw new AwkRuntimeException(flow == AwkStatementFlow.BreakLoop
                    ? AwkMessages.ErrorBreakOutsideLoop
                    : AwkMessages.ErrorContinueOutsideLoop);

            if (flow is AwkStatementFlow.NextRecord or AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
            {
                state.ValidateFunctionFlow(flow);
                throw new AwkFunctionFlowSignal(flow);
            }

            return AwkValue.FromText(string.Empty);
        }
        finally
        {
            state.PopFunctionFrame();
        }
    }

    private readonly struct RentedFunctionArguments : IDisposable
    {
        private readonly bool _isPooled;
        private readonly AwkFunctionArgument[]? _values;

        private RentedFunctionArguments(int count)
        {
            _isPooled = count > 0;
            _values = _isPooled
                ? ArrayPool<AwkFunctionArgument>.Shared.Rent(count)
                : [];
        }

        public AwkFunctionArgument[] Values => _values ?? [];

        public static RentedFunctionArguments Rent(int count) => new(count);

        public void Dispose()
        {
            if (_isPooled)
                ArrayPool<AwkFunctionArgument>.Shared.Return(Values, clearArray: true);
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

    public static AwkFunctionArgument Array(AwkArrayName arrayName) =>
        new(AwkFunctionArgumentKind.Array, default, arrayName);

    public static AwkFunctionArgument PotentialArray(AwkArrayName arrayName) =>
        new(AwkFunctionArgumentKind.PotentialArray, default, arrayName);
}

internal enum AwkFunctionArgumentKind
{
    Scalar,
    Array,
    PotentialArray
}
