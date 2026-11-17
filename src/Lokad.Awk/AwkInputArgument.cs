
namespace Lokad.Awk;

internal abstract record AwkInputArgument
{
    public abstract AwkArgvIndex ArgvIndex { get; init; }

    public static AwkInputArgument Empty(int argvIndex) =>
        new EmptyArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument File(string path, int argvIndex) =>
        new FileArgument(path, AwkUtf8Codec.Encode(path), new AwkArgvIndex(argvIndex));

    public static AwkInputArgument StandardInput(int argvIndex) =>
        new StandardInputArgument(new AwkArgvIndex(argvIndex));

    public static AwkInputArgument Assignment(int argvIndex) =>
        new VariableAssignmentArgument(new AwkArgvIndex(argvIndex));

    internal sealed record EmptyArgument(AwkArgvIndex ArgvIndex) : AwkInputArgument;

    internal sealed record FileArgument(
        string RawPath,
        ReadOnlyMemory<byte> Display,
        AwkArgvIndex ArgvIndex) : AwkInputArgument;

    internal sealed record StandardInputArgument(
        AwkArgvIndex ArgvIndex) : AwkInputArgument;

    internal sealed record VariableAssignmentArgument(
        AwkArgvIndex ArgvIndex) : AwkInputArgument;
}
