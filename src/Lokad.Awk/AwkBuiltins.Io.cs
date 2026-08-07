using System.Threading.Tasks;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static async ValueTask<AwkValue> EvaluateCloseAsync(AwkBuiltinContext context)
    {
        if (context.ArgumentCount is < 1 or > 2)
            return AwkValue.FromNumber(-1);

        if (!context.TryGetExecutionContext(out var execution))
            return AwkValue.FromNumber(-1);

        var path = await context.GetStringAsync(0).ConfigureAwait(false);
        var direction = AwkOptionalCloseDirection.None;
        if (context.ArgumentCount == 2)
        {
            var directionText = await context.GetStringAsync(1).ConfigureAwait(false);
            if (!AwkCloseDirectionParser.TryParse(directionText, out var parsedDirection))
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidCloseMode);

            direction = AwkOptionalCloseDirection.Some(parsedDirection);
        }

        await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        var result = !direction.HasValue
            ? await execution.Io.CloseAsync(path).ConfigureAwait(false)
            : await execution.Io.CloseAsync(path, direction.Value).ConfigureAwait(false);
        if (result.Failed)
            context.State.SetErrno(result.Error.Errno);

        return AwkValue.FromNumber(result.StatusCode);
    }

    private static async ValueTask<AwkValue> EvaluateFflushAsync(AwkBuiltinContext context)
    {
        if (context.ArgumentCount > 1)
            return AwkValue.FromNumber(-1);

        if (!context.TryGetExecutionContext(out var execution))
            return AwkValue.FromNumber(-1);

        await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
        AwkIoOperationResult result;
        if (context.ArgumentCount == 0)
        {
            result = await execution.Io.FlushAllAsync().ConfigureAwait(false);
        }
        else
        {
            var path = await context.GetStringAsync(0).ConfigureAwait(false);
            result = path.Length == 0
                ? await execution.Io.FlushAllAsync().ConfigureAwait(false)
                : await execution.Io.FlushPathAsync(path).ConfigureAwait(false);
        }
        if (result.Failed)
            context.State.SetErrno(result.Error.Errno);

        return AwkValue.FromNumber(result.StatusCode);
    }
}
