namespace Lokad.Awk;

// One coarse charge for source copies and frontend/program storage. Conservative source
// retention ends with the execution/artifact owner; there is no reservation for each node.
internal sealed class AwkFrontendMemory(IMemoryBudget? budget) : IDisposable
{
    private IMemoryReservation? _reservation;
    private long _sourceBytes;
    private long _parserBytes;

    public static AwkFrontendMemory Disabled { get; } = new(null);

    public void AdmitSourceUnits(int count) => AddSourceEstimate(count, 64);

    // UTF-16 builders, their growth overlap/final strings and source mapping buffers.
    public void AdmitTextCopy(int characters) => AddSourceEstimate(characters, 8);

    private void AddSourceEstimate(int count, int bytesPerItem)
    {
        if (budget is null || count == 0) return;
        var bytes = AwkRuntimeMemory.CheckedAdd(_sourceBytes,
            AwkRuntimeMemory.CheckedMultiply(count, bytesPerItem));
        Resize(bytes, _parserBytes);
        _sourceBytes = bytes;
    }

    // The dependency eagerly allocates its tokens. Admit a character-derived buffer
    // bound before entering it, then use the observed token count for syntax/lowering.
    public void AdmitLexing(int characters)
    {
        if (budget is null) return;
        var bytes = AwkRuntimeMemory.CheckedMultiply((long)characters + 1, 64);
        Resize(_sourceBytes, Math.Max(_parserBytes, bytes));
    }

    public void AdmitLexemes(int characters, int count) => AdmitTokens(characters, count, 192);

    public void AdmitTokens(int characters, int count) => AdmitTokens(characters, count, 512);

    private void AdmitTokens(int characters, int count, int bytesPerToken)
    {
        if (budget is null) return;
        var bytes = AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedMultiply((long)characters + 1, 16),
            AwkRuntimeMemory.CheckedMultiply((long)count + 1, bytesPerToken));
        Resize(_sourceBytes, bytes);
        _parserBytes = bytes;
    }

    private void Resize(long sourceBytes, long parserBytes)
    {
        if (budget is null) return;
        var bytes = AwkRuntimeMemory.CheckedAdd(sourceBytes, parserBytes);
        if (_reservation is { } reservation)
        {
            if (!reservation.TryResize(bytes))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        }
        else
        {
            if (!budget.TryReserve(bytes, out var created))
                throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
            _reservation = created;
        }
    }

    public void Dispose() => _reservation?.Dispose();
}
