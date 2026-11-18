using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

// Tag only application callbacks, with no replacement owners or extra accounting.
internal static class AwkMemoryCallbacks
{
    public static bool TryReserve(IMemoryBudget budget, long bytes,
        [NotNullWhen(true)] out IMemoryReservation? reservation)
    {
        try { return budget.TryReserve(bytes, out reservation); }
        catch (Exception error) { throw new AwkHostException(error); }
    }

    public static bool TryResize(IMemoryReservation reservation, long bytes)
    {
        try { return reservation.TryResize(bytes); }
        catch (Exception error) { throw new AwkHostException(error); }
    }

    public static long Bytes(IMemoryReservation reservation)
    {
        try { return reservation.Bytes; }
        catch (Exception error) { throw new AwkHostException(error); }
    }

    public static void Dispose(IDisposable? owner)
    {
        try { owner?.Dispose(); }
        catch (Exception error) { throw new AwkHostException(error); }
    }
}
