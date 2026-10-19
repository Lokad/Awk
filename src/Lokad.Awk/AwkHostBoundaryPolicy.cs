using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal static class AwkHostBoundaryPolicy
{
    // Await the actual user before observing cancellation; borrowed storage stays alive.
    public static async Task<T> AwaitAsync<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        var result = await operation.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public static string ResolveError(
        AwkOperationError error,
        bool exposeDetails,
        int maxErrorBytes)
    {
        if (!exposeDetails ||
            error.Message.Length > maxErrorBytes ||
            Encoding.UTF8.GetByteCount(error.Message) > maxErrorBytes)
            return default(AwkOperationError).Message;

        return error.Message;
    }
}
