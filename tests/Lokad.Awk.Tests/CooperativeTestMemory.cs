namespace Lokad.Awk.Tests;

// Direct-runtime tests own the same execution scope used at the public boundary.
internal sealed class CooperativeTestMemory : IDisposable
{
    private AwkRuntimeState? _state;

    public CooperativeTestMemory(long bytes, AwkRuntimeCounters? counters = null, bool enabled = true)
    {
        Budget = new CooperativeTestBudget(bytes);
        Owner = AwkExecutionMemoryOwner.Create(enabled ? Budget : null, CancellationToken.None);
        Accounting = AwkMemoryAccounting.Create(counters ?? AwkRuntimeCounters.Disabled, Owner);
    }

    public CooperativeTestBudget Budget { get; }
    public AwkExecutionMemoryOwner Owner { get; }
    public AwkMemoryAccounting Accounting { get; }
    public AwkRuntimeState State => _state ?? throw new InvalidOperationException("No direct runtime was created.");

    public CooperativeTestMemory Initialize(AwkRuntimeState state)
    {
        if (_state is not null) throw new InvalidOperationException("A fixture owns one runtime.");
        _state = state;
        return this;
    }

    public CooperativeTestMemory Initialize(Func<AwkRuntimeState> create)
    {
        try { return Initialize(create()); }
        catch { Dispose(); throw; }
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
