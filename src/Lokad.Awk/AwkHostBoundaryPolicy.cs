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

    public static AwkRuntimeException CloseFailure(AwkFileDescriptor descriptor,
        AwkDescriptorCloseResult result, bool exposeDetails, int maxErrorBytes,
        ReadOnlyMemory<byte> identity = default)
    {
        var detail = result.Error.HasMessage ? ResolveError(result.Error, exposeDetails, maxErrorBytes) : null;
        var message = identity.IsEmpty
            ? AwkMessages.FormatDescriptorCloseError(descriptor, detail)
            : AwkMessages.FormatPathError(identity, "close failed" + (detail is null ? "" : ": " + detail));
        return new AwkRuntimeException(message, AwkDiagnosticId.HostIoFailure);
    }
}
