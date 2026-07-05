using System;

namespace Lokad.Awk;

internal readonly struct AwkProgramResult
{
    private readonly AwkProgram _program;

    private AwkProgramResult(AwkProgram program)
    {
        HasProgram = true;
        _program = program;
    }

    public bool HasProgram { get; }

    public AwkProgram Program =>
        HasProgram ? _program : throw new InvalidOperationException("No awk program is available.");

    public static AwkProgramResult None => default;

    public static AwkProgramResult Available(AwkProgram program) => new(program);
}
