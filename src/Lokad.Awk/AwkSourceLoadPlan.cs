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
    private readonly TimeSpan _cleanupTimeout;
    private readonly bool _allowSourceIncludes;
    private readonly TimeSpan _hostOperationTimeout;
    private readonly bool _exposeHostErrorDetails;
    private readonly int _maxHostErrorBytes;
    private readonly AwkCleanupDeadline _cleanupDeadline;
    private readonly AwkCompatibilityMode _compatibilityMode;
    private readonly HashSet<AwkSourceIdentity> _includedIdentities = [];
    private readonly Dictionary<AwkPath, AwkScriptTextReadResult> _scriptTextByPath = [];

    private AwkSourceLoadPlan(
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkPath? accessiblePathRoot,
        TimeSpan? cleanupTimeout,
        bool allowSourceIncludes,
        bool exposeEnvironment,
        TimeSpan? hostOperationTimeout,
        bool exposeHostErrorDetails,
        int maxHostErrorBytes,
        AwkCompatibilityMode compatibilityMode,
        AwkFrontendMemory? memory)
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
        Memory = memory ?? AwkFrontendMemory.Disabled;
        _awkPathEntries = exposeEnvironment ? ParseAwkPathEntries(environment) : [];
        _currentDirectory = currentDirectory;
        _accessiblePathRoot = accessiblePathRoot;
        _cleanupTimeout = cleanupTimeout ?? TimeSpan.FromSeconds(30);
        _allowSourceIncludes = allowSourceIncludes;
        _hostOperationTimeout = hostOperationTimeout ?? TimeSpan.FromMinutes(5);
        _exposeHostErrorDetails = exposeHostErrorDetails;
        _maxHostErrorBytes = maxHostErrorBytes;
        _cleanupDeadline = new AwkCleanupDeadline(_cleanupTimeout);
        _compatibilityMode = compatibilityMode;
    }

    public int SourceCount => _sources.Count;
    public AwkFrontendMemory Memory { get; }

    public IReadOnlyList<string> AwkPathEntries => _awkPathEntries;

    public TimeSpan CleanupTimeout => _cleanupTimeout;

    public bool AllowSourceIncludes => _allowSourceIncludes;
    public AwkCompatibilityMode CompatibilityMode => _compatibilityMode;

    public TimeSpan HostOperationTimeout => _hostOperationTimeout;

    public TimeSpan CloseOperationTimeout =>
        _hostOperationTimeout <= _cleanupTimeout ? _hostOperationTimeout : _cleanupTimeout;

    public AwkCleanupDeadline CleanupDeadline => _cleanupDeadline;

    public bool ExposeHostErrorDetails => _exposeHostErrorDetails;

    public int MaxHostErrorBytes => _maxHostErrorBytes;

    public string ResolveHostError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, _exposeHostErrorDetails, _maxHostErrorBytes);

    public static AwkSourceLoadPlan FromInvocation(
        AwkInvocation invocation,
        AwkPath? accessiblePathRoot = null,
        TimeSpan? cleanupTimeout = null,
        bool allowSourceIncludes = true,
        bool exposeEnvironment = true,
        TimeSpan? hostOperationTimeout = null,
        bool exposeHostErrorDetails = true,
        int maxHostErrorBytes = 4 << 10,
        AwkFrontendMemory? memory = null) =>
        new(
            invocation.SourcePlan.Sources,
            invocation.RuntimeArgumentsPlan.Environment,
            invocation.RuntimeArgumentsPlan.CurrentDirectory,
            accessiblePathRoot,
            cleanupTimeout,
            allowSourceIncludes,
            exposeEnvironment,
            hostOperationTimeout,
            exposeHostErrorDetails,
            maxHostErrorBytes,
            invocation.ExecutionOptions.CompatibilityProfile.Mode,
            memory);

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
