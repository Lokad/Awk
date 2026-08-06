using System;
using System.Collections.Generic;

namespace Lokad.Awk;

/// <summary> An exported environment variable for an awk command invocation. </summary>
public readonly record struct AwkEnvironmentVariable
{
    private readonly string? _name;
    private readonly string? _value;

    public AwkEnvironmentVariable(string Name, string Value)
    {
        ArgumentException.ThrowIfNullOrEmpty(Name);
        ArgumentNullException.ThrowIfNull(Value);
        _name = Name;
        _value = Value;
    }

    public string Name => _name ?? string.Empty;

    public string Value => _value ?? string.Empty;

    public void Deconstruct(out string Name, out string Value)
    {
        Name = this.Name;
        Value = this.Value;
    }
}

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
            AwkPathResolution.ResolveCurrentDirectory(RequireEnvironment(environment)),
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
    }

    public string CommandName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public IReadOnlyList<AwkEnvironmentVariable> Environment { get; }

    public AwkPath CurrentDirectory { get; }

    public AwkFileDescriptor StdIn { get; }

    public AwkFileDescriptor StdOut { get; }

    public AwkFileDescriptor StdErr { get; }

    private static IReadOnlyList<AwkEnvironmentVariable> RequireEnvironment(
        IReadOnlyList<AwkEnvironmentVariable>? environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment;
    }

    private static IReadOnlyList<string> CopyArguments(IReadOnlyList<string> arguments)
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

    private static IReadOnlyList<AwkEnvironmentVariable> CopyEnvironment(
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
}
