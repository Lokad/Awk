using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokad.Awk;

/// <summary> An exported environment variable for an awk command invocation. </summary>
public readonly record struct AwkEnvironmentVariable
{
    private readonly string? _name;
    private readonly string? _value;

    /// <summary>Initializes an exported environment variable.</summary>
    public AwkEnvironmentVariable(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _name = name;
        _value = value;
    }

    /// <summary>Gets the variable name.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>Gets the variable value.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>Deconstructs the variable into its name and value.</summary>
    public void Deconstruct(out string name, out string value)
    {
        name = Name;
        value = Value;
    }
}

/// <summary>
/// Represents a single awk command invocation with resolved arguments and descriptors.
/// </summary>
public sealed class AwkCommandInvocation
{
    /// <summary>
    /// Creates an invocation with standard descriptors and a current directory derived from <c>PWD</c>.
    /// </summary>
    public static AwkCommandInvocation CreateWithStandardDescriptors(
        string commandName,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return new AwkCommandInvocation(
            commandName,
            arguments,
            environment,
            AwkPathResolution.ResolveCurrentDirectory(environment),
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr);
    }

    /// <summary>Initializes an invocation with an explicit current directory and descriptors.</summary>
    public AwkCommandInvocation(
        string commandName,
        IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkFileDescriptor stdIn,
        AwkFileDescriptor stdOut,
        AwkFileDescriptor stdErr,
        IMemoryBudget? memoryBudget = null)
    {
        static ReadOnlyCollection<string> CopyArguments(IReadOnlyList<string> arguments)
        {
            var copy = new string[arguments.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                var argument = arguments[index];
                ArgumentNullException.ThrowIfNull(argument, nameof(arguments));
                copy[index] = argument;
            }

            return Array.AsReadOnly(copy);
        }

        static ReadOnlyCollection<AwkEnvironmentVariable> CopyEnvironment(
            IReadOnlyList<AwkEnvironmentVariable> environment)
        {
            var copy = new AwkEnvironmentVariable[environment.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                var variable = environment[index];
                ArgumentException.ThrowIfNullOrEmpty(variable.Name, nameof(environment));
                copy[index] = variable;
            }

            return Array.AsReadOnly(copy);
        }

        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(currentDirectory);
        CommandName = commandName;
        Arguments = CopyArguments(arguments);
        Environment = CopyEnvironment(environment);
        CurrentDirectory = currentDirectory;
        StdIn = stdIn;
        StdOut = stdOut;
        StdErr = stdErr;
        MemoryBudget = memoryBudget;
    }

    /// <summary>Gets the executable name supplied by the caller.</summary>
    public string CommandName { get; }

    /// <summary>Gets the borrowed budget associated with this command by its caller.</summary>
    public IMemoryBudget? MemoryBudget { get; }

    /// <summary>Gets an immutable snapshot of command arguments, excluding the executable name.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>Gets an immutable snapshot of exported environment variables.</summary>
    public IReadOnlyList<AwkEnvironmentVariable> Environment { get; }

    /// <summary>Gets the canonical absolute current directory for the command.</summary>
    public AwkPath CurrentDirectory { get; }

    /// <summary>Gets the descriptor used for standard input.</summary>
    public AwkFileDescriptor StdIn { get; }

    /// <summary>Gets the descriptor used for standard output.</summary>
    public AwkFileDescriptor StdOut { get; }

    /// <summary>Gets the descriptor used for standard error.</summary>
    public AwkFileDescriptor StdErr { get; }

}
