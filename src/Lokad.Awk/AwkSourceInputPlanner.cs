using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkSourceInputPlanner
{
    private readonly AwkPath _currentDirectory;
    private readonly List<AwkResolvedPath> _paths = [];
    private readonly List<AwkInputArgument> _inputArguments = [];
    private readonly List<AwkSourceReference> _sources = [];
    private readonly List<string> _runtimeArguments;
    private readonly List<AwkVariableAssignment> _variables = [];

    public AwkSourceInputPlanner(string commandName, AwkPath currentDirectory)
    {
        _currentDirectory = currentDirectory;
        _runtimeArguments = [commandName];
    }

    public bool HasScript { get; private set; }

    public AwkInvocationPlan BuildPlan(IReadOnlyList<AwkEnvironmentVariable> environment) =>
        new(
            new AwkSourcePlan(AwkCollectionSnapshots.Copy(_sources)),
            new AwkInputPlan(
                AwkCollectionSnapshots.Copy(_paths),
                AwkCollectionSnapshots.Copy(_inputArguments)),
            new AwkRuntimeArgumentPlan(
                AwkCollectionSnapshots.Copy(_runtimeArguments),
                AwkCollectionSnapshots.Copy(_variables),
                AwkCollectionSnapshots.Copy(environment),
                _currentDirectory));

    public void AddInlineScript(string source)
    {
        _sources.Add(AwkSourceReference.Inline(source));
        HasScript = true;
    }

    public void AddSourceFile(string path)
    {
        var scriptPath = AwkPathResolution.ResolveArgument(path, _currentDirectory);
        _sources.Add(AwkSourceReference.File(scriptPath));
        HasScript = true;
    }

    public void AddIncludeFile(string path)
    {
        var includePath = AwkPathResolution.ResolveArgument(path, _currentDirectory);
        _sources.Add(AwkSourceReference.Include(includePath));
    }

    public bool TryAddOptionVariableAssignment(string assignment, out ReadOnlyMemory<byte> error)
    {
        if (!AwkVariableAssignment.TryParse(
                assignment,
                AwkVariableAssignmentSource.Option,
                out var parsedAssignment))
        {
            error = AwkMessages.ErrorInvalidVariableAssignment;
            return false;
        }

        _variables.Add(parsedAssignment);
        error = ReadOnlyMemory<byte>.Empty;
        return true;
    }

    public void AddInputArgument(string value)
    {
        var argvIndex = AddRuntimeArgument(value);
        if (AwkVariableAssignment.TryParse(
                value,
                AwkVariableAssignmentSource.InputArgument,
                out var assignment))
        {
            _inputArguments.Add(AwkInputArgument.Assignment(assignment, argvIndex));
            return;
        }

        AddInputPath(value, argvIndex);
    }

    private void AddInputPath(string value, int argvIndex)
    {
        if (value is "-" or "/dev/stdin")
        {
            _inputArguments.Add(AwkInputArgument.StandardInput(argvIndex));
            return;
        }

        var path = AwkPathResolution.ResolveArgument(value, _currentDirectory);
        _paths.Add(path);
        _inputArguments.Add(AwkInputArgument.File(path, argvIndex));
    }

    private int AddRuntimeArgument(string value)
    {
        _runtimeArguments.Add(value);
        return _runtimeArguments.Count - 1;
    }
}
