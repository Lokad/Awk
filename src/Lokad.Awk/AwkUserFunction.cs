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
        context.State.ThrowIfCancellationRequested();

        using (var buffer = RentedFunctionArguments.Rent(arguments.Count))
        {
            var values = buffer.Values;
            for (var index = 0; index < arguments.Count; index++)
            {
                var argument = arguments[index];
                if (argument.TryGetVariableName(out var variableName) &&
                    context.State.TryResolveArrayArgument(variableName, out var arrayArgument))
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

    public ValueTask<AwkValue> InvokeValuesAsync(
        IReadOnlyList<AwkValue> arguments,
        AwkEvaluationContext context)
    {
        context.State.ThrowIfCancellationRequested();

        using (var buffer = RentedFunctionArguments.Rent(arguments.Count))
        {
            var values = buffer.Values;
            for (var index = 0; index < arguments.Count; index++)
                values[index] = AwkFunctionArgument.Scalar(arguments[index]);

            PushArguments(values, arguments.Count, context);
        }

        return InvokeFrameAsync(context);
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

            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.FromText(string.Empty);

            for (var index = 0; index < _statements.Length; index++)
            {
                state.RecordExecutionStep();
                state.ThrowIfCancellationRequested();
                var flow = await _statements[index]
                    .ExecuteAsync(context)
                    .ConfigureAwait(false);
                await execution.Output.FlushAsync(execution.Io).ConfigureAwait(false);
                if (flow == AwkStatementFlow.ReturnFunction)
                    return state.TryConsumeFunctionReturnValue(out var value) ? value : AwkValue.FromText(string.Empty);

                if (flow is AwkStatementFlow.BreakLoop or AwkStatementFlow.ContinueLoop)
                    throw new AwkRuntimeException(flow == AwkStatementFlow.BreakLoop
                        ? AwkMessages.ErrorBreakOutsideLoop
                        : AwkMessages.ErrorContinueOutsideLoop);

                if (flow != AwkStatementFlow.Continue)
                    return AwkValue.FromText(string.Empty);
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
