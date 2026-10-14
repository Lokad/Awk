using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

/// <summary>A borrowed host budget sharing one allowance across cooperating consumers.</summary>
/// <remarks>Admission is thread-safe and immediate. The library never disposes a supplied budget.</remarks>
public interface IMemoryBudget
{
    /// <summary>Creates an empty owned scope sharing this allowance, without a separate cap.</summary>
    IMemoryScope CreateScope();

    /// <summary>Reserves nonnegative bytes; refusal leaves accounting unchanged and returns null.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The estimate is negative.</exception>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    bool TryReserve(long bytes, [NotNullWhen(true)] out IMemoryReservation? reservation);
}

/// <summary>An owned subtree of a shared budget.</summary>
/// <remarks>Disposal is idempotent and non-throwing, refunds the subtree and invalidates descendants.
/// Owners must finish storage users and serialize disposal with subtree operations.</remarks>
public interface IMemoryScope : IMemoryBudget, IDisposable { }

/// <summary>An owned charge against a shared allowance.</summary>
/// <remarks>Serialize resizing/disposal. Disposal is idempotent and non-throwing.</remarks>
public interface IMemoryReservation : IDisposable
{
    /// <summary>Gets the estimate, or zero after this reservation or an ancestor is disposed.</summary>
    long Bytes { get; }

    /// <summary>Replaces the nonnegative estimate; refusal leaves it unchanged.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The estimate is negative.</exception>
    /// <exception cref="ObjectDisposedException">The owner has been disposed.</exception>
    bool TryResize(long bytes);
}
