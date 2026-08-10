using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkSourceLoadPlan
{
    private readonly IReadOnlyList<AwkSourceReference> _sources;
    private readonly IReadOnlyList<AwkEnvironmentVariable> _environment;
    private readonly AwkPath _currentDirectory;
    private readonly HashSet<AwkSourceIdentity> _includedIdentities = [];
    private readonly Dictionary<AwkPath, AwkScriptTextReadResult> _scriptTextByPath = [];

    private AwkSourceLoadPlan(
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory)
    {
        _sources = sources;
        _environment = environment;
        _currentDirectory = currentDirectory;
    }

    public int SourceCount => _sources.Count;

    public static AwkSourceLoadPlan FromInvocation(AwkInvocation invocation) =>
        new(
            invocation.SourcePlan.Sources,
            invocation.RuntimeArgumentsPlan.Environment,
            invocation.RuntimeArgumentsPlan.CurrentDirectory);

    public AwkSourceReference GetSource(int index) => _sources[index];

    public AwkResolvedPath ResolvePath(string path) =>
        AwkPathResolution.ResolveArgument(path, _currentDirectory);

    public bool TryMarkIncluded(AwkScriptFile script) =>
        _includedIdentities.Add(script.SourceIdentity);

    public bool TryGetScriptText(
        AwkPath path,
        [NotNullWhen(true)] out AwkScriptTextReadResult? result) =>
        _scriptTextByPath.TryGetValue(path, out result);

    public void CacheScriptText(AwkPath path, AwkScriptTextReadResult result)
    {
        if (result is not AwkScriptTextReadResult.Failed)
            _scriptTextByPath[path] = result;
    }

    public bool TryGetEnvironmentValue(string name, out string value)
    {
        for (var index = 0; index < _environment.Count; index++)
        {
            if (string.Equals(_environment[index].Name, name, StringComparison.Ordinal))
            {
                value = _environment[index].Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
