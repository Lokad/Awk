
namespace Lokad.Awk;

internal abstract record AwkInputArgument
{
    private AwkInputArgument(AwkArgvIndex argvIndex)
    {
        ArgvIndex = argvIndex;
    }

    public AwkArgvIndex ArgvIndex { get; }

    public static AwkInputArgument Empty(int argvIndex) =>
        new EmptyArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument File(string path, int argvIndex) =>
        new FileArgument(path, AwkUtf8Codec.Encode(path), new AwkArgvIndex(argvIndex));

    public static AwkInputArgument StandardInput(int argvIndex) =>
        new StandardInputArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument Assignment(int argvIndex) =>
        new VariableAssignmentArgument(new AwkArgvIndex(argvIndex));

    internal sealed record EmptyArgument(AwkArgvIndex EmptyArgvIndex) : AwkInputArgument(EmptyArgvIndex);

    internal sealed record FileArgument(
        string RawPath,
        ReadOnlyMemory<byte> Display,
        AwkArgvIndex FileArgvIndex) : AwkInputArgument(FileArgvIndex);

    internal sealed record StandardInputArgument(
        AwkArgvIndex StandardInputArgvIndex) : AwkInputArgument(StandardInputArgvIndex);

    internal sealed record VariableAssignmentArgument(
        AwkArgvIndex VariableAssignmentArgvIndex) : AwkInputArgument(VariableAssignmentArgvIndex);
}
