using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkSourceInputPlanner
{
    private readonly AwkPath _currentDirectory;
    private readonly List<AwkInputArgument> _inputArguments = [];
    private readonly List<AwkSourceReference> _sources = [];
    private readonly List<string> _runtimeArguments;
    private readonly List<AwkVariableAssignment> _variables = [];
    private readonly CancellationToken _cancellationToken;

    public AwkSourceInputPlanner(string commandName, AwkPath currentDirectory, CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _currentDirectory = currentDirectory;
        _runtimeArguments = [commandName];
    }

    public bool HasScript { get; private set; }

    public void AddFieldSeparatorOption(string rawValue) =>
        _variables.Add(AwkVariableAssignment.FieldSeparatorOption(rawValue));

    public void PrepareCompatibility(AwkCompatibilityMode mode)
    {
        Action? checkpoint = _cancellationToken.CanBeCanceled ? _cancellationToken.ThrowIfCancellationRequested : null;
        for (var index = 0; index < _variables.Count; index++)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var assignment = _variables[index].WithCompatibilityMode(mode, checkpoint);
            if (assignment.Source == AwkVariableAssignmentSource.FieldSeparatorOption)
                // Preserve strict text validation before source IO without retaining a UTF-8 copy.
                _ = AwkUtf8Codec.GetEncodedByteCount(assignment.Value);
            _cancellationToken.ThrowIfCancellationRequested();
            _variables[index] = assignment;
        }
    }

    public AwkInvocationPlan BuildPlan(IReadOnlyList<AwkEnvironmentVariable> environment) =>
        new(
            new AwkSourcePlan(AwkCollectionSnapshots.Copy(_sources, _cancellationToken)),
            new AwkInputPlan(AwkCollectionSnapshots.Copy(_inputArguments, _cancellationToken)),
            new AwkRuntimeArgumentPlan(
                AwkCollectionSnapshots.Copy(_runtimeArguments, _cancellationToken),
                AwkCollectionSnapshots.Copy(_variables, _cancellationToken),
                AwkCollectionSnapshots.Copy(environment, _cancellationToken),
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
        if (value.Length == 0)
        {
            _inputArguments.Add(AwkInputArgument.Empty(argvIndex));
            return;
        }
        var equalsIndex = value.IndexOf('=', StringComparison.Ordinal);
        if (equalsIndex > 0 && AwkNameFacts.IsValidVariableName(value.AsSpan(0, equalsIndex)))
        {
            _inputArguments.Add(AwkInputArgument.Assignment(argvIndex));
            return;
        }

        AddInputPath(value, argvIndex);

        void AddInputPath(string value, int argvIndex)
        {
            if (AwkSpecialPathPolicy.IsStandardInput(value))
            {
                _inputArguments.Add(AwkInputArgument.StandardInput(argvIndex));
                return;
            }

            _inputArguments.Add(AwkInputArgument.File(value, argvIndex));
        }

        int AddRuntimeArgument(string value)
        {
            _runtimeArguments.Add(value);
            return _runtimeArguments.Count - 1;
        }
    }
}
