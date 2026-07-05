namespace Lokad.Awk;

/// <summary>
/// Represents a resolved file descriptor handle.
/// </summary>
public readonly record struct AwkFileDescriptor(int Id)
{
    public static AwkFileDescriptor StdIn => new(0);

    public static AwkFileDescriptor StdOut => new(1);

    public static AwkFileDescriptor StdErr => new(2);

    public static AwkFileDescriptor DevNull => new(int.MaxValue);

    public static AwkPath DevNullPath { get; } = new("/dev/null");

    public bool IsDevNull => this == DevNull;
}
