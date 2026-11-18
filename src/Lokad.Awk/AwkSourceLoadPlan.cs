using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed class AwkSourceLoadPlan
{
    private readonly IReadOnlyList<AwkSourceReference> _sources;
    private readonly AwkPath _currentDirectory;
    private readonly AwkCompatibilityMode _compatibilityMode;
    private readonly Dictionary<AwkSourceIdentity, AwkSourceSegmentKind> _sourceRoles = [];
    private readonly Dictionary<AwkPath, AwkScriptTextReadResult> _scriptTextByIdentity = [];
    private readonly Dictionary<AwkPathRequest, AwkScriptTextReadResult> _scriptTextByRequest = [];

    private AwkSourceLoadPlan(
        IReadOnlyList<AwkSourceReference> sources,
        IReadOnlyList<AwkEnvironmentVariable> environment,
        AwkPath currentDirectory,
        AwkRuntimeOptions options,
        AwkCompatibilityMode compatibilityMode,
        CancellationToken cancellationToken)
    {
        static string FindAwkPath(
            IReadOnlyList<AwkEnvironmentVariable> environment, CancellationToken cancellationToken)
        {
            for (var index = 0; index < environment.Count; index++)
            {
                if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
                if (!string.Equals(environment[index].Name, "AWKPATH", StringComparison.Ordinal))
                    continue;

                return environment[index].Value;
            }

            return string.Empty;
        }

        _sources = sources;
        Options = options;
        AwkPath = options.ExposeEnvironment ? FindAwkPath(environment, cancellationToken) : string.Empty;
        _currentDirectory = currentDirectory;
        _compatibilityMode = compatibilityMode;
    }

    public int SourceCount => _sources.Count;
    public AwkRuntimeOptions Options { get; }
    public AwkFrontendMemory Memory => Options.MemoryOwner.FrontendMemory;

    public string AwkPath { get; }

    public bool AllowSourceIncludes => Options.AllowSourceIncludes;
    public AwkCompatibilityMode CompatibilityMode => _compatibilityMode;



    public string ResolveHostError(AwkOperationError error) =>
        AwkHostBoundaryPolicy.ResolveError(error, Options.ExposeHostErrorDetails, Options.MaxHostErrorBytes);

    public static AwkSourceLoadPlan FromInvocation(
        AwkInvocation invocation,
        AwkRuntimeOptions options,
        CancellationToken cancellationToken = default) =>
        new(
            invocation.SourcePlan.Sources,
            invocation.RuntimeArgumentsPlan.Environment,
            invocation.RuntimeArgumentsPlan.CurrentDirectory,
            options,
            invocation.ExecutionOptions.CompatibilityProfile.Mode,
            cancellationToken);

    public AwkSourceReference GetSource(int index) => _sources[index];

    // A bare name can resolve through AWKPATH instead. Authorize the actual candidate
    // in the reader before consulting its cache or opening a host descriptor.
    public AwkFileOperand PreparePath(string path) =>
        AwkPathResolution.PrepareRuntimeArgument(path, _currentDirectory, accessiblePathRoot: Options.AccessiblePathRoot);

    public AwkFileOperand BindHostPolicy(AwkFileOperand path) =>
        Equals(path.Request.AccessiblePathRoot, Options.AccessiblePathRoot) ? path :
            new(path.Display, new(path.Request.Path, path.Request.CurrentDirectory, Options.AccessiblePathRoot));

    public bool TryGetRequestText(AwkPathRequest request,
        [NotNullWhen(true)] out AwkScriptTextReadResult? result) =>
        _scriptTextByRequest.TryGetValue(request, out result);

    public void CacheRequestText(AwkPathRequest request, AwkScriptTextReadResult result)
    {
        if (result is AwkScriptTextReadResult.Failed) return;
        if (!_scriptTextByRequest.ContainsKey(request))
        {
            Memory.AdmitSourceUnits(4);
            Memory.AdmitTextCopy(request.Path.Length);
        }
        _scriptTextByRequest[request] = result;
        if (result is AwkScriptTextReadResult.Found found)
            CacheResolvedText(found.ResolvedPath, result);
    }

    public AwkSourceRegistration RegisterSource(AwkScriptFile script, AwkSourceSegmentKind kind)
    {
        if (_sourceRoles.TryGetValue(script.SourceIdentity, out var previous))
            return previous == kind ? AwkSourceRegistration.Repeated : AwkSourceRegistration.Conflict;
        Memory.AdmitSourceUnits(1);
        _sourceRoles.Add(script.SourceIdentity, kind);
        return AwkSourceRegistration.Added;
    }

    public bool TryGetResolvedText(
        AwkPath path,
        [NotNullWhen(true)] out AwkScriptTextReadResult? result) =>
        _scriptTextByIdentity.TryGetValue(path, out result);

    public void CacheResolvedText(AwkPath path, AwkScriptTextReadResult result)
    {
        if (result is not AwkScriptTextReadResult.Failed)
        {
            if (!_scriptTextByIdentity.ContainsKey(path))
            {
                // Cached misses and empty files retain path/index storage too. Admit a
                // coarse entry/growth estimate through the existing frontend owner.
                Memory.AdmitSourceUnits(4);
                Memory.AdmitTextCopy(path.Path.Length);
            }
            _scriptTextByIdentity[path] = result;
        }
    }
}

internal enum AwkSourceRegistration { Added, Repeated, Conflict }
