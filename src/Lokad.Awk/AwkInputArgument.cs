
namespace Lokad.Awk;

internal abstract record AwkInputArgument
{
    private AwkInputArgument(AwkArgvIndex argvIndex)
    {
        ArgvIndex = argvIndex;
    }

    /// <summary>Gets the input-scheduler action represented by this argument.</summary>
    public abstract AwkInputArgumentKind Kind { get; }

    public AwkArgvIndex ArgvIndex { get; }

    public static AwkInputArgument Empty(int argvIndex) =>
        new EmptyArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument File(AwkResolvedPath path, int argvIndex) =>
        new FileArgument(path, new AwkArgvIndex(argvIndex));

    public static AwkInputArgument StandardInput(int argvIndex) =>
        new StandardInputArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument Assignment(AwkVariableAssignment assignment, int argvIndex) =>
        new VariableAssignmentArgument(assignment, new AwkArgvIndex(argvIndex));

    internal sealed record EmptyArgument(AwkArgvIndex EmptyArgvIndex) : AwkInputArgument(EmptyArgvIndex)
    {
        public override AwkInputArgumentKind Kind => AwkInputArgumentKind.Empty;
    }

    internal sealed record FileArgument(
        AwkResolvedPath Path,
        AwkArgvIndex FileArgvIndex) : AwkInputArgument(FileArgvIndex)
    {
        public override AwkInputArgumentKind Kind => AwkInputArgumentKind.File;
    }

    internal sealed record StandardInputArgument(
        AwkArgvIndex StandardInputArgvIndex) : AwkInputArgument(StandardInputArgvIndex)
    {
        public override AwkInputArgumentKind Kind => AwkInputArgumentKind.StandardInput;
    }

    internal sealed record VariableAssignmentArgument(
        AwkVariableAssignment VariableAssignment,
        AwkArgvIndex VariableAssignmentArgvIndex) : AwkInputArgument(VariableAssignmentArgvIndex)
    {
        public override AwkInputArgumentKind Kind => AwkInputArgumentKind.VariableAssignment;
    }
}

internal enum AwkInputArgumentKind
{
    Empty,
    File,
    StandardInput,
    VariableAssignment
}
