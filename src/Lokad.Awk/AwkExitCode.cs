namespace Lokad.Awk;

public readonly record struct AwkExitCode
{
    public AwkExitCode(int value)
    {
        Value = value;
    }

    public static AwkExitCode Success { get; } = new(0);

    public static AwkExitCode FatalError { get; } = new(2);

    public int Value { get; }

    public bool Succeeded => Value == 0;

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
