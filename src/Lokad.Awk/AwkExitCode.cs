namespace Lokad.Awk;

/// <summary>Represents the exit status returned by an awk command or host operation.</summary>
public readonly record struct AwkExitCode
{
    /// <summary>Initializes an exit status from its integer representation.</summary>
    public AwkExitCode(int value)
    {
        Value = value;
    }

    /// <summary>Gets the successful exit status.</summary>
    public static AwkExitCode Success { get; } = new(0);

    /// <summary>Gets the conventional exit status for a fatal awk error.</summary>
    public static AwkExitCode FatalError { get; } = new(2);

    /// <summary>Gets the integer exit status.</summary>
    public int Value { get; }

    /// <summary>Gets whether the status represents success.</summary>
    public bool Succeeded => Value == 0;

    /// <summary>Gets whether the status represents failure.</summary>
    public bool Failed => Value != 0;
}

internal readonly record struct AwkOptionalExitCode
{
    private readonly AwkExitCode _exitCode;

    private AwkOptionalExitCode(bool hasValue, AwkExitCode exitCode)
    {
        HasValue = hasValue;
        _exitCode = exitCode;
    }

    public static AwkOptionalExitCode None { get; } = new(false, default);

    public static AwkOptionalExitCode Specified(AwkExitCode exitCode) => new(true, exitCode);

    public bool HasValue { get; }

    public AwkExitCode Value => HasValue ? _exitCode : AwkExitCode.Success;
}
