namespace Lokad.Awk;

/// <summary>
/// Represents a resolved file descriptor handle.
/// </summary>
public readonly record struct AwkFileDescriptor
{
    /// <summary>Initializes a descriptor from a nonnegative host-defined identifier.</summary>
    public AwkFileDescriptor(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        Id = id;
    }

    /// <summary>Gets the host-defined descriptor identifier.</summary>
    public int Id { get; }

    /// <summary>Gets the conventional standard-input descriptor.</summary>
    public static AwkFileDescriptor StdIn => new(0);

    /// <summary>Gets the conventional standard-output descriptor.</summary>
    public static AwkFileDescriptor StdOut => new(1);

    /// <summary>Gets the conventional standard-error descriptor.</summary>
    public static AwkFileDescriptor StdErr => new(2);

}
