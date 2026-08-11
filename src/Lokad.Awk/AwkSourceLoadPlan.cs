using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkSourceLoadPlan
{
    private readonly IReadOnlyList<AwkSourceReference> _sources;
    private readonly IReadOnlyList<string> _awkPathEntries;
    private readonly AwkPath _currentDirectory;
    private readonly AwkPath? _accessiblePathRoot;
    private readonly HashSet<AwkSourceIdentity> _includedIdentities = [];
    private readonly Dictionary<AwkPath, AwkScriptTextReadResult> _scriptTextByPath = [];

    private AwkSourceLoadPlan(
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkPath? accessiblePathRoot)
    {
        static IReadOnlyList<string> ParseAwkPathEntries(
            IReadOnlyList<AwkEnvironmentVariable> environment)
        {
            for (var index = 0; index < environment.Count; index++)
            {
                if (!string.Equals(environment[index].Name, "AWKPATH", StringComparison.Ordinal))
                    continue;

                var awkPath = environment[index].Value;
                if (awkPath.Length == 0)
                    return [];

                var entries = awkPath.Split(':', StringSplitOptions.None);
                for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                {
                    entries[entryIndex] = entries[entryIndex].Length == 0
                        ? "."
                        : entries[entryIndex].TrimEnd('/');
                }

                return entries;
            }

            return [];
        }

        _sources = sources;
        _awkPathEntries = ParseAwkPathEntries(environment);
        _currentDirectory = currentDirectory;
        _accessiblePathRoot = accessiblePathRoot;
    }

    public int SourceCount => _sources.Count;

    public IReadOnlyList<string> AwkPathEntries => _awkPathEntries;

    public static AwkSourceLoadPlan FromInvocation(AwkInvocation invocation, AwkPath? accessiblePathRoot = null) =>
        new(
            invocation.SourcePlan.Sources,
            invocation.RuntimeArgumentsPlan.Environment,
            invocation.RuntimeArgumentsPlan.CurrentDirectory,
            accessiblePathRoot);

    public AwkSourceReference GetSource(int index) => _sources[index];

    public AwkResolvedPath ResolvePath(string path)
    {
        var resolved = AwkPathResolution.ResolveRuntimeArgument(path, _currentDirectory);
        ThrowIfDenied(resolved.Absolute);
        return resolved;
    }

    public void ThrowIfDenied(AwkPath path) =>
        AwkHostPathPolicy.ThrowIfDenied(path, _accessiblePathRoot);

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
}
