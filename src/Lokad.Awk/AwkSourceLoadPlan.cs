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
    private readonly bool _allowSourceIncludes;
    private readonly bool _exposeHostErrorDetails;
    private readonly int _maxHostErrorBytes;
    private readonly AwkCompatibilityMode _compatibilityMode;
    private readonly HashSet<AwkSourceIdentity> _includedIdentities = [];
    private readonly Dictionary<AwkPath, AwkScriptTextReadResult> _scriptTextByPath = [];

    private AwkSourceLoadPlan(
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkPath? accessiblePathRoot,
        bool allowSourceIncludes,
        bool exposeEnvironment,
        bool exposeHostErrorDetails,
        int maxHostErrorBytes,
        AwkCompatibilityMode compatibilityMode,
        AwkFrontendMemory? memory,
        CancellationToken cancellationToken)
    {
        static IReadOnlyList<string> ParseAwkPathEntries(
            IReadOnlyList<AwkEnvironmentVariable> environment, AwkFrontendMemory memory, CancellationToken cancellationToken)
        {
            for (var index = 0; index < environment.Count; index++)
            {
                if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
                if (!string.Equals(environment[index].Name, "AWKPATH", StringComparison.Ordinal))
                    continue;

                var awkPath = environment[index].Value;
                if (awkPath.Length == 0)
                    return [];

                memory.AdmitTextCopy(awkPath.Length);
                return awkPath.Split(':', StringSplitOptions.None);
            }

            return [];
        }

        _sources = sources;
        Memory = memory ?? AwkFrontendMemory.Disabled;
        _awkPathEntries = exposeEnvironment ? ParseAwkPathEntries(environment, Memory, cancellationToken) : [];
        _currentDirectory = currentDirectory;
        _accessiblePathRoot = accessiblePathRoot;

        _allowSourceIncludes = allowSourceIncludes;

        _exposeHostErrorDetails = exposeHostErrorDetails;
        _maxHostErrorBytes = maxHostErrorBytes;

        _compatibilityMode = compatibilityMode;
    }

    public int SourceCount => _sources.Count;
    public AwkFrontendMemory Memory { get; }

    public IReadOnlyList<string> AwkPathEntries => _awkPathEntries;

    public bool AllowSourceIncludes => _allowSourceIncludes;
    public AwkCompatibilityMode CompatibilityMode => _compatibilityMode;

    public bool ExposeHostErrorDetails => _exposeHostErrorDetails;

    public int MaxHostErrorBytes => _maxHostErrorBytes;

    public string ResolveHostError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, _exposeHostErrorDetails, _maxHostErrorBytes);

    public static AwkSourceLoadPlan FromInvocation(
        AwkInvocation invocation,
        AwkPath? accessiblePathRoot = null,
        bool allowSourceIncludes = true,
        bool exposeEnvironment = true,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10,
        AwkFrontendMemory? memory = null,
        CancellationToken cancellationToken = default) =>
        new(
            invocation.SourcePlan.Sources,
            invocation.RuntimeArgumentsPlan.Environment,
            invocation.RuntimeArgumentsPlan.CurrentDirectory,
            accessiblePathRoot,
            allowSourceIncludes,
            exposeEnvironment,
            exposeHostErrorDetails,
            maxHostErrorBytes,
            invocation.ExecutionOptions.CompatibilityProfile.Mode,
            memory,
            cancellationToken);

    public AwkSourceReference GetSource(int index) => _sources[index];

    // A bare name can resolve through AWKPATH instead. Authorize the actual candidate
    // in the reader before consulting its cache or opening a host descriptor.
    public AwkResolvedPath ResolvePath(string path) =>
        AwkPathResolution.ResolveRuntimeArgument(path, _currentDirectory);

    public void ThrowIfDenied(AwkPath path) =>
        AwkHostPathPolicy.ThrowIfDenied(path, _accessiblePathRoot);

    public bool TryMarkIncluded(AwkScriptFile script)
    {
        if (_includedIdentities.Contains(script.SourceIdentity)) return false;
        Memory.AdmitSourceUnits(1);
        return _includedIdentities.Add(script.SourceIdentity);
    }

    public bool TryGetScriptText(
        AwkPath path,
        [NotNullWhen(true)] out AwkScriptTextReadResult? result) =>
        _scriptTextByPath.TryGetValue(path, out result);

    public void CacheScriptText(AwkPath path, AwkScriptTextReadResult result)
    {
        if (result is not AwkScriptTextReadResult.Failed)
        {
            if (!_scriptTextByPath.ContainsKey(path))
            {
                // Cached misses and empty files retain path/index storage too. Admit a
                // coarse entry/growth estimate through the existing frontend owner.
                Memory.AdmitSourceUnits(4);
                Memory.AdmitTextCopy(path.Path.Length);
            }
            _scriptTextByPath[path] = result;
        }
    }
}
