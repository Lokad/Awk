namespace Lokad.Awk.Tests;

// Direct-runtime tests own the same execution scope used at the public boundary.
internal sealed class CooperativeTestMemory : IDisposable
{
    private AwkRuntimeState? _state;
    private AwkMemoryAccounting? _standaloneAccounting;

    public CooperativeTestMemory(long bytes, bool enabled = true)
    {
        Budget = new CooperativeTestBudget(bytes);
        Owner = AwkExecutionMemoryOwner.Create(enabled ? Budget : null, CancellationToken.None);
    }

    public CooperativeTestBudget Budget { get; }
    public AwkExecutionMemoryOwner Owner { get; }
    public AwkMemoryAccounting Accounting => _state?.MemoryAccounting ??
        (_standaloneAccounting ??= AwkMemoryAccounting.Create(Owner));
    public AwkRuntimeState State => _state ?? throw new InvalidOperationException("No direct runtime was created.");

    public CooperativeTestMemory InitializeRuntime(AwkRuntimeCounters counters, CancellationToken token = default) =>
        Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token,
            Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, null,
                counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance)
            {
                InternalFailureObserver = AwkInternalFailuresAttribute.CreateObserver()
            })));

    public CooperativeTestMemory Initialize(AwkRuntimeState state)
    {
        RequireUninitialized();
        _state = state;
        return this;
    }

    public CooperativeTestMemory Initialize(Func<AwkRuntimeState> create)
    {
        RequireUninitialized();
        try { return Initialize(create()); }
        catch { Dispose(); throw; }
    }

    private void RequireUninitialized()
    {
        if (_state is not null || _standaloneAccounting is not null)
            throw new InvalidOperationException("Initialize a fixture before using its accounting.");
    }

    public AwkRuntimeOptions Configure(AwkRuntimeOptions options) => options with
    {
        MemoryOwner = Owner,
        MemoryBudget = Owner.IsEnabled ? Budget : null
    };

    public void Dispose()
    {
        _state?.Dispose();
        Owner.DisposeAsync().GetAwaiter().GetResult();
    }
}
