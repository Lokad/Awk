namespace Lokad.Awk;

// Owns the current descriptor borrow. Record/source storage remains in its existing owners.
internal sealed class AwkByteReader(
    IAwkHost host, AwkFileDescriptor descriptor, CancellationToken cancellationToken,
    AwkRuntimeResourceMonitor resources, bool exposeHostErrorDetails, int maxHostErrorBytes) : IDisposable
{
    internal const int ReadSize = 64 << 10;
    private AwkReadBuffer? _borrow;
    private int _offset;
    private AwkReadCompletion _completion;
    private AwkOperationError _error;

    public ReadOnlyMemory<byte> Content => _borrow is { } borrow ? borrow.Content[_offset..] : default;
    public bool ReachedEof => _completion == AwkReadCompletion.EndOfFile;

    public async ValueTask<bool> TryBufferAsync()
    {
        if (!Content.IsEmpty) return true;
        if (_completion == AwkReadCompletion.Failure) ThrowReadFailure();
        if (ReachedEof) return false;
        ReleaseBorrow();
        resources.RecordHostCall();
        var borrow = await host.ReadAsync(descriptor, ReadSize, cancellationToken).ConfigureAwait(false);
        _borrow = borrow;
        _offset = 0;
        _completion = borrow.Completion;
        _error = borrow.Error;
        resources.RecordBytesRead(borrow.Content.Length);
        cancellationToken.ThrowIfCancellationRequested();
        if (borrow.Content.Length > ReadSize)
            throw new AwkHostReadException(descriptor,
                new(AwkOperationErrorKind.IoFailure, "Read exceeded the requested byte count."));
        if (!borrow.Content.IsEmpty) return true;
        if (borrow.Failed) ThrowReadFailure();
        return false;
    }

    public void Advance(int count)
    {
        var borrow = _borrow ?? throw new InvalidOperationException("No read borrow is active.");
        if (count < 0 || count > borrow.Content.Length - _offset)
            throw new ArgumentOutOfRangeException(nameof(count));
        _offset += count;
        borrow.SetConsumedBytes(_offset);
    }

    public void ReleaseBorrow()
    {
        var borrow = _borrow;
        _borrow = null;
        _offset = 0;
        borrow?.Dispose();
    }

    public void Dispose() => ReleaseBorrow();

    private void ThrowReadFailure()
    {
        var error = _error;
        _completion = AwkReadCompletion.MoreData;
        _error = default;
        ReleaseBorrow();
        // Report once. A later explicit getline may make a new host read; there is no automatic retry.
        throw new AwkHostReadException(descriptor,
            new(error.Kind, AwkHostBoundaryPolicy.ResolveError(error, exposeHostErrorDetails, maxHostErrorBytes)));
    }
}
