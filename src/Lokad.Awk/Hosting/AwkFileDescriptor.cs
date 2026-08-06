namespace Lokad.Awk;

/// <summary>
/// Represents a resolved file descriptor handle.
/// </summary>
/// <param name="Id">The nonnegative host-defined descriptor identifier.</param>
public readonly record struct AwkFileDescriptor(int Id)
{
    /// <summary>Gets the conventional standard-input descriptor.</summary>
    public static AwkFileDescriptor StdIn => new(0);

    /// <summary>Gets the conventional standard-output descriptor.</summary>
    public static AwkFileDescriptor StdOut => new(1);

    /// <summary>Gets the conventional standard-error descriptor.</summary>
    public static AwkFileDescriptor StdErr => new(2);

    /// <summary>Gets the distinguished descriptor that discards writes.</summary>
    public static AwkFileDescriptor DevNull => new(int.MaxValue);

    /// <summary>Gets the canonical path corresponding to the null device.</summary>
    public static AwkPath DevNullPath { get; } = new("/dev/null");

    /// <summary>Gets whether this is the distinguished null-device descriptor.</summary>
    public bool IsDevNull => this == DevNull;
}
