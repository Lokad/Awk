using System.Collections.Generic;

namespace Lokad.Awk;

/// <summary> An exported environment variable for an awk command invocation. </summary>
public readonly record struct AwkEnvironmentVariable(string Name, string Value);

/// <summary>
/// Represents a single awk command invocation with resolved arguments and descriptors.
/// </summary>
public sealed class AwkCommandInvocation
{
    public AwkCommandInvocation(
        string commandName,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment)
        : this(
            commandName,
            arguments,
            environment,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr)
    {
    }

    public AwkCommandInvocation(
        string commandName,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr)
        : this(
            commandName,
            arguments,
            environment,
            AwkPathResolution.ResolveCurrentDirectory(environment),
            stdIn,
            stdOut,
            stdErr)
    {
    }

    public AwkCommandInvocation(
        string commandName,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr)
    {
        CommandName = commandName;
        Arguments = arguments;
        Environment = environment;
        CurrentDirectory = currentDirectory;
        StdIn = stdIn;
        StdOut = stdOut;
        StdErr = stdErr;
    }

    public string CommandName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public IReadOnlyList<AwkEnvironmentVariable> Environment { get; }

    public AwkPath CurrentDirectory { get; }

    public AwkFileDescriptor StdIn { get; }

    public AwkFileDescriptor StdOut { get; }

    public AwkFileDescriptor StdErr { get; }
}
