namespace Lokad.Awk;

// Own-storage estimates; admission belongs exclusively to the host's shared allowance.
internal sealed class AwkMemoryAccounting
{
    private readonly AwkExecutionMemoryOwner _owner;

    private AwkMemoryAccounting(AwkExecutionMemoryOwner owner)
    {
        _owner = owner;
    }

    public static AwkMemoryAccounting Disabled { get; } =
        new(AwkExecutionMemoryOwner.Disabled);

    public bool IsEnabled => _owner.IsEnabled;

    public long LiveBytes => _owner.TrackedBytes;

    public static AwkMemoryAccounting Create(AwkExecutionMemoryOwner owner)
    {
        return !owner.IsEnabled
            ? Disabled
            : new AwkMemoryAccounting(owner);
    }

    public AwkMemoryMutationAdmission AdmitMutation(long byteCount) =>
        _owner.AdmitMutation(byteCount);

    public void Charge(long bytes)
    {
        if (IsEnabled && bytes > 0) ResizeCharge(0, bytes);
    }

    public void Refund(long bytes)
    {
        if (IsEnabled && bytes > 0) ResizeCharge(bytes, 0);
    }

    public AwkMemoryLease ReserveTransient(long byteCount)
    {
        if (!IsEnabled || byteCount <= 0)
            return AwkMemoryLease.Disabled;

        var lease = new AwkMemoryLease(this);
        lease.Resize(byteCount);
        return lease;
    }

    // Evaluation can keep a scalar after a later operand replaces its original slot.
    // A scoped duplicate estimate avoids tracking aliases between storage owners.
    public AwkMemoryLease RetainValue(AwkValue value) =>
        ReserveTransient(IsEnabled ? value.RetainedTextMemoryBytes : 0);

    internal void ResizeCharge(long previousBytes, long bytes)
    {
        var live = LiveBytes;
        if (previousBytes > live)
            throw new InvalidOperationException("Cannot release more bytes than are currently charged.");

        if (!_owner.TryResizeTracked(AwkRuntimeMemory.CheckedAdd(live - previousBytes, bytes)))
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
    }

    public AwkMemoryLease CreateTransientLease() => IsEnabled ? new AwkMemoryLease(this) : AwkMemoryLease.Disabled;
}

internal sealed class AwkMemoryLease : IDisposable
{
    private AwkMemoryAccounting? _accounting;
    private long _byteCount;

    private AwkMemoryLease()
    {
    }

    public AwkMemoryLease(AwkMemoryAccounting accounting) => _accounting = accounting;

    public static AwkMemoryLease Disabled { get; } = new();

    internal long Bytes => _byteCount;

    internal void Resize(long bytes)
    {
        if (ReferenceEquals(this, Disabled)) return;
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        var accounting = _accounting ?? throw new ObjectDisposedException(nameof(AwkMemoryLease));
        accounting.ResizeCharge(_byteCount, bytes);
        _byteCount = bytes;
    }

    public void ReserveAdditional(long byteCount)
    {
        var accounting = _accounting;
        if (accounting is null || byteCount <= 0)
            return;

        var nextByteCount = AwkRuntimeMemory.CheckedAdd(_byteCount, byteCount);
        accounting.Charge(byteCount);
        _byteCount = nextByteCount;
    }

    public void Release(long byteCount)
    {
        var accounting = _accounting;
        if (accounting is null)
            return;
        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot release more bytes than remain in the memory lease.");
        accounting.Refund(byteCount);
        _byteCount -= byteCount;
    }

    public void Transfer(long byteCount)
    {
        if (_accounting is null)
            return;

        if (byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Cannot transfer more bytes than remain in the memory lease.");

        _byteCount -= byteCount;
    }

    // The former storage owner has already admitted these bytes. Its caller
    // relinquishes its claim before any callback can observe the transfer.
    internal void ReceiveTransferred(long byteCount)
    {
        if (_accounting is null) return;
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        _byteCount = AwkRuntimeMemory.CheckedAdd(_byteCount, byteCount);
    }

    public void MoveTo(AwkMemoryLease destination, long byteCount)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_accounting is null || ReferenceEquals(this, destination))
            return;
        if (!ReferenceEquals(_accounting, destination._accounting) || byteCount < 0 || byteCount > _byteCount)
            throw new InvalidOperationException("Memory can only move between live leases of the same accounting owner.");
        var nextDestinationBytes = AwkRuntimeMemory.CheckedAdd(destination._byteCount, byteCount);
        _byteCount -= byteCount;
        destination._byteCount = nextDestinationBytes;
    }

    public void Dispose()
    {
        var accounting = _accounting;
        if (accounting is null)
            return;

        _accounting = null;
        if (_byteCount > 0)
            accounting.Refund(_byteCount);
        _byteCount = 0;
    }
}
